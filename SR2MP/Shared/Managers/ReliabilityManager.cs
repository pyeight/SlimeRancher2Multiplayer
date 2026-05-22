using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

internal sealed class ReliabilityManager
{
    private sealed class PendingPacket : IRecyclable
    {
        public SplitResult SplitData;
        public IPEndPoint Destination = null!;
        public ushort PacketId;
        public byte PacketType;
        public DateTime FirstSendTime;
        public DateTime LastSendTime;
        public int SendCount;

        public bool IsRecycled { get; set; }

        private void Initialize(SplitResult splitData, IPEndPoint destination, ushort packetId,
            byte packetType)
        {
            SplitData = splitData;
            Destination = destination;
            PacketId = packetId;
            PacketType = packetType;

            FirstSendTime = DateTime.UtcNow;
            LastSendTime = DateTime.UtcNow;
            SendCount = 1;
        }

        public void Recycle()
        {
            SplitData.Dispose();
            Destination = null!;
        }

        public void Dispose() => Return(this);

        public static PendingPacket Borrow(SplitResult splitData, IPEndPoint destination,
            ushort packetId, byte packetType)
        {
            var packet = RecyclePool<PendingPacket>.Borrow();
            packet.Initialize(splitData, destination, packetId, packetType);
            return packet;
        }

        public static void Return(PendingPacket packet) => RecyclePool<PendingPacket>.Return(packet);
    }

    private readonly ConcurrentDictionary<PacketKey, PendingPacket> pendingPackets = new();
    private readonly ConcurrentDictionary<ChannelKey, ushort> lastProcessedSequence = new();
    private readonly ConcurrentDictionary<ChannelKey, int> sequenceNumbersByChannel = new();
    private readonly ConcurrentDictionary<ChannelKey, SortedDictionary<ushort, Action>> reorderBuffers = new();
    private readonly ConcurrentDictionary<ChannelKey, object> reorderLocks = new();
    private readonly ConcurrentQueue<PendingPacket> pendingDisposal = new();

    private readonly Action<ArraySegment<byte>, IPEndPoint> sendRawCallback;

    private Thread? resendThread;
    private volatile bool isRunning;

    private static readonly TimeSpan ResendInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaxRetryTime = TimeSpan.FromSeconds(10);
    private const int MaxResendAttempts = 64;
    private const int MaxReorderBufferSize = 64;

    public ReliabilityManager(Action<ArraySegment<byte>, IPEndPoint> sendRawCallback)
    {
        this.sendRawCallback = sendRawCallback;
    }

    public void Start()
    {
        if (isRunning)
            return;

        isRunning = true;
        resendThread = new Thread(new Action(ResendLoop))
        {
            IsBackground = true,
            Name = "ReliabilityResendThread"
        };
        resendThread.Start();

        SrLogger.LogMessage("ReliabilityManager started");
    }

    public void Stop()
    {
        if (!isRunning)
            return;

        isRunning = false;

        foreach (var packet in pendingPackets.Values)
            PendingPacket.Return(packet);

        pendingPackets.Clear();
        
        while (pendingDisposal.TryDequeue(out var leftover))
            PendingPacket.Return(leftover);

        lastProcessedSequence.Clear();
        sequenceNumbersByChannel.Clear();
        reorderBuffers.Clear();
        reorderLocks.Clear();

        SrLogger.LogMessage("ReliabilityManager stopped");
    }

    public void TrackPacket(SplitResult splitData, IPEndPoint destination, ushort packetId,
        byte packetType, PacketReliability reliability)
    {
        if (!reliability.HasFlag(PacketReliability.Reliable))
            return;

        var key = new PacketKey(packetType, packetId, destination);
        pendingPackets[key] = PendingPacket.Borrow(splitData, destination, packetId, packetType);
    }

    public void HandleAck(IPEndPoint sender, ushort packetId, byte packetType)
    {
        var key = new PacketKey(packetType, packetId, sender);

        if (!pendingPackets.TryRemove(key, out var packet))
            return;

        SrLogger.LogPacketSize(
            $"ACK received for packet {packetId} (type={packetType}) " +
            $"after {packet.SendCount} sends, " +
            $"latency={(DateTime.UtcNow - packet.FirstSendTime).TotalMilliseconds:F1}ms");
        
        pendingDisposal.Enqueue(packet);
    }

    /// <summary>
    /// Returns the next sequence number for a given packet type / channel / destination.
    /// Sequence numbers wrap from <see cref="ushort.MaxValue"/> back to 1.
    /// </summary>
    public ushort GetNextSequenceNumber(NetworkChannel channel, byte packetType, IPEndPoint destination)
    {
        var key = new ChannelKey(destination, channel, packetType);
        var seq = sequenceNumbersByChannel.AddOrUpdate(
            key,
            1,
            (_, current) => current >= ushort.MaxValue ? 1 : current + 1);

        return (ushort)seq;
    }

    public bool ShouldProcessOrderedPacket(IPEndPoint sender, ushort sequenceNumber, byte packetType,
        NetworkChannel channel, PacketReliability reliability, Action? processAction = null)
    {
        var key     = new ChannelKey(sender, channel, packetType);
        var lockObj = reorderLocks.GetOrAdd(key, _ => new object());

        lock (lockObj)
        {
            if (!lastProcessedSequence.TryGetValue(key, out var lastSequence))
            {
                // First packet from this sender on this channel/type
                lastProcessedSequence[key] = sequenceNumber;
                return true;
            }

            if (IsSequenceNewer(sequenceNumber, lastSequence) || sequenceNumber == (ushort)(lastSequence + 1))
            {
                if (reliability == PacketReliability.Ordered)
                {
                    lastProcessedSequence[key] = sequenceNumber;
                    return true;
                }

                var expectedSequence = (ushort)(lastSequence + 1);

                if (sequenceNumber == expectedSequence)
                {
                    lastProcessedSequence[key] = sequenceNumber;
                    DrainReorderBuffer(key, lockObj);
                    return true;
                }

                if (processAction != null)
                {
                    var buffer = reorderBuffers.GetOrAdd(key, _ => new SortedDictionary<ushort, Action>());

                    if (buffer.Count < MaxReorderBufferSize)
                    {
                        buffer[sequenceNumber] = processAction;
                        SrLogger.LogPacketSize(
                            $"Buffered out-of-order packet: " +
                            $"expected seq={expectedSequence}, got seq={sequenceNumber}, " +
                            $"type={packetType}, channel={channel}, buffer size={buffer.Count}");
                    }
                    else
                    {
                        SrLogger.LogPacketSize(
                            $"Reorder buffer full, dropping packet: " +
                            $"seq={sequenceNumber}, type={packetType}, channel={channel}");
                    }
                }
                else
                {
                    SrLogger.LogPacketAcknowledge(
                        $"Out-of-order packet dropped: " +
                        $"expected seq={expectedSequence}, got seq={sequenceNumber}, " +
                        $"type={packetType}, channel={channel}");
                }
            }

            return false;
        }
    }

    // todo: review
    private void DrainReorderBuffer(ChannelKey key, object lockObj)
    {
        if (!reorderBuffers.TryGetValue(key, out var buffer) || buffer.Count == 0)
            return;

        while (buffer.Count > 0)
        {
            var next = (ushort)(lastProcessedSequence[key] + 1);
            if (!buffer.TryGetValue(next, out var bufferedAction))
                break;

            buffer.Remove(next);
            lastProcessedSequence[key] = next;

            Monitor.Exit(lockObj);
            try
            {
                bufferedAction();
            }
            finally
            {
                Monitor.Enter(lockObj);
            }
        }
    }

    private void ResendLoop()
    {
        while (isRunning)
        {
            try
            {
                var count = pendingPackets.Count;
                if (count == 0)
                {
                    DrainDisposalQueue();
                    Thread.Sleep(10);
                    continue;
                }

                var now           = DateTime.UtcNow;
                var keysToRemove  = ArrayPool<PacketKey>.Shared.Rent(count);
                var removeCount   = 0;

                foreach (var (key, packet) in pendingPackets)
                {
                    if (packet.IsRecycled)
                        continue;
                    
                    if (now - packet.FirstSendTime > MaxRetryTime || packet.SendCount >= MaxResendAttempts)
                    {
                        SrLogger.LogPacketAcknowledge(
                            $"Packet {packet.PacketId} (type={packet.PacketType}) " +
                            $"failed after {packet.SendCount} attempts");
                        keysToRemove[removeCount++] = key;
                        continue;
                    }

                    // Checks if packet should be resent
                    if (now - packet.LastSendTime > ResendInterval)
                    {
                        for (var i = 0; i < packet.SplitData.Count; i++)
                            sendRawCallback(packet.SplitData.Chunks[i], packet.Destination);

                        packet.LastSendTime = now;
                        packet.SendCount++;

                        if (packet.SendCount % 10 == 0)
                        {
                            SrLogger.LogPacketAcknowledge(
                                $"Resending packet {packet.PacketId} " +
                                $"(type={packet.PacketType}) attempt #{packet.SendCount}");
                        }
                    }
                }

                // Remove timed out packets
                for (var i = 0; i < removeCount; i++)
                {
                    if (pendingPackets.TryRemove(keysToRemove[i], out var timedOutPacket))
                        PendingPacket.Return(timedOutPacket);
                }

                ArrayPool<PacketKey>.Shared.Return(keysToRemove);

                DrainDisposalQueue();

                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"ResendLoop error: {ex}");
            }
        }
    }
    
    private void DrainDisposalQueue()
    {
        while (pendingDisposal.TryDequeue(out var packet))
            PendingPacket.Return(packet);
    }

    private static bool IsSequenceNewer(ushort s1, ushort s2)
    {
        return ((s1 > s2) && (s1 - s2 <= 32768)) ||
               ((s1 < s2) && (s2 - s1 > 32768));
    }

    public int GetPendingPacketCount() => pendingPackets.Count;
}