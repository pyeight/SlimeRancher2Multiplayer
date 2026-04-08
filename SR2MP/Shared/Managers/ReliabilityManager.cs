using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Managers;

public sealed class ReliabilityManager
{
    private sealed class PendingPacket : IRecyclable
    {
        internal SplitResult SplitData;
        internal IPEndPoint Destination = null!;
        internal ushort PacketId;
        internal byte PacketType;
        internal PacketReliability Reliability;
        internal DateTime FirstSendTime;
        internal DateTime LastSendTime;
        internal int SendCount;
        internal ushort SequenceNumber;
        
        public bool IsRecycled { get; set; }

        private void Initialize(SplitResult splitData, IPEndPoint destination, ushort packetId,
            byte packetType, PacketReliability reliability, ushort sequenceNumber)
        {
            SplitData = splitData;
            Destination = destination;
            PacketId = packetId;
            PacketType = packetType;
            Reliability = reliability;
            SequenceNumber = sequenceNumber;

            FirstSendTime = DateTime.UtcNow;
            LastSendTime = DateTime.UtcNow;
            SendCount = 1;
        }

        public void Recycle()
        {
            SplitData.Dispose();
            Destination = null!;
        }

        public static PendingPacket Borrow(SplitResult splitData, IPEndPoint destination, ushort packetId,
            byte packetType, PacketReliability reliability, ushort sequenceNumber)
        {
            var packet = RecyclePool<PendingPacket>.Borrow();
            packet.Initialize(splitData, destination, packetId, packetType, reliability, sequenceNumber);
            return packet;
        }
        
        public static void Return(PendingPacket packet) => RecyclePool<PendingPacket>.Return(packet);
    }

    private readonly ConcurrentDictionary<PacketKey, PendingPacket> pendingPackets = new();
    private readonly ConcurrentDictionary<SequenceKey, ushort> lastProcessedSequence = new();
    
    private readonly ConcurrentDictionary<SequenceKey, int> sequenceNumbersByType = new();
    
    private readonly ConcurrentDictionary<SequenceKey, SortedDictionary<ushort, Action>> reorderBuffers = new();
    private readonly ConcurrentDictionary<SequenceKey, object> reorderLocks = new();

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

        SrLogger.LogMessage("ReliabilityManager started", SrLogTarget.Both);
    }

    public void Stop()
    {
        if (!isRunning)
            return;

        isRunning = false;
        
        foreach (var packet in pendingPackets.Values)
            PendingPacket.Return(packet);

        pendingPackets.Clear();
        lastProcessedSequence.Clear();
        sequenceNumbersByType.Clear();
        reorderBuffers.Clear();
        reorderLocks.Clear();

        SrLogger.LogMessage("ReliabilityManager stopped", SrLogTarget.Both);
    }
    
    public void TrackPacket(SplitResult splitData, IPEndPoint destination, ushort packetId,
        byte packetType, PacketReliability reliability, ushort sequenceNumber)
    {
        if (reliability is PacketReliability.Unreliable or PacketReliability.UnreliableOrdered)
            return;

        var key = new PacketKey(packetType, packetId, destination);
        pendingPackets[key] = PendingPacket.Borrow(splitData, destination, packetId, packetType, reliability, sequenceNumber);
    }

    public void HandleAck(IPEndPoint sender, ushort packetId, byte packetType)
    {
        var key = new PacketKey(packetType, packetId, sender);
        
        if (!pendingPackets.TryRemove(key, out var packet))
            return;

        var latency = DateTime.UtcNow - packet.FirstSendTime;
        SrLogger.LogPacketSize(
            $"ACK received for packet {packetId} (type={packetType}) after {packet.SendCount} sends, latency={latency.TotalMilliseconds:F1}ms",
            SrLogTarget.Both);
    }
    
    // Checks if an ordered packet should be processed based on sequence number
    public ushort GetNextSequenceNumber(byte packetType, IPEndPoint destination)
    {
        var key = new SequenceKey(destination, packetType);
        var seq = sequenceNumbersByType.AddOrUpdate(key, 1, (_, current) => (current >= ushort.MaxValue) ? 1 : current + 1);

        return (ushort)seq;
    }

    public bool ShouldProcessOrderedPacket(IPEndPoint sender, ushort sequenceNumber, byte packetType,
        PacketReliability reliability, Action? processAction = null)
    {
        var key = new SequenceKey(sender, packetType);
        var lockObj = reorderLocks.GetOrAdd(key, _ => new object());

        lock (lockObj)
        {
            if (!lastProcessedSequence.TryGetValue(key, out var lastSequence))
            {
                // First packet from this sender for this type
                lastProcessedSequence[key] = sequenceNumber;
                return true;
            }

            if (IsSequenceNewer(sequenceNumber, lastSequence) || sequenceNumber == (ushort)(lastSequence + 1))
            {
                if (reliability == PacketReliability.UnreliableOrdered)
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
                            $"Buffered out-of-order packet: expected seq={expectedSequence}, got seq={sequenceNumber}, type={packetType}, buffer size={buffer.Count}",
                            SrLogTarget.Both);
                    }
                    else
                    {
                        SrLogger.LogPacketSize(
                            $"Reorder buffer full, dropping packet: seq={sequenceNumber}, type={packetType}",
                            SrLogTarget.Both);
                    }
                }
                else
                {
                    SrLogger.LogPacketAcknowledge(
                        $"Out-of-order packet dropped: expected seq={expectedSequence}, got seq={sequenceNumber}, type={packetType}",
                        SrLogTarget.Both);
                }
            }

            return false;
        }
    }

    // todo: review
    
    private void DrainReorderBuffer(SequenceKey key, object lockObj)
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
                    Thread.Sleep(10);
                    continue;
                }

                var now = DateTime.UtcNow;
                var keysToRemove = ArrayPool<PacketKey>.Shared.Rent(count);
                var removeCount = 0;

                foreach (var (key, packet) in pendingPackets)
                {
                    // Checks if packet has timed out
                    if (now - packet.FirstSendTime > MaxRetryTime || packet.SendCount >= MaxResendAttempts)
                    {
                        SrLogger.LogPacketAcknowledge(
                            $"Packet {packet.PacketId} (type={packet.PacketType}) failed after {packet.SendCount} attempts",
                            SrLogTarget.Both);
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
                                $"Resending packet {packet.PacketId} (type={packet.PacketType}) attempt #{packet.SendCount}",
                                SrLogTarget.Both);
                        }
                    }
                }

                // Removes timed out packets
                for (var i = 0; i < removeCount; i++)
                {
                    if (pendingPackets.TryRemove(keysToRemove[i], out var timedOutPacket))
                        PendingPacket.Return(timedOutPacket);
                }

                ArrayPool<PacketKey>.Shared.Return(keysToRemove);

                // todo: Should not cause problems, if it does, remove
                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"ResendLoop error: {ex}", SrLogTarget.Both);
            }
        }
    }

    private static bool IsSequenceNewer(ushort s1, ushort s2)
    {
        return ((s1 > s2) && (s1 - s2 <= 32768)) ||
               ((s1 < s2) && (s2 - s1 > 32768));
    }

    public int GetPendingPacketCount() => pendingPackets.Count;
}