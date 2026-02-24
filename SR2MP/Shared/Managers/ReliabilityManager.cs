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
        public SplitResult SplitData;
        public IPEndPoint Destination = null!;
        public ushort PacketId;
        public byte PacketType;
        public PacketReliability Reliability;
        public DateTime FirstSendTime;
        public DateTime LastSendTime;
        public int SendCount;
        public ushort SequenceNumber;
        
        public bool IsRecycled { get; set; }

        public void Initialize(SplitResult splitData, IPEndPoint destination, ushort packetId,
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
    }

    private readonly ConcurrentDictionary<PacketKey, PendingPacket> pendingPackets = new();
    private readonly ConcurrentDictionary<SequenceKey, ushort> lastProcessedSequence = new();
    private readonly ConcurrentDictionary<byte, int> sequenceNumbersByType = new();

    private readonly Action<ArraySegment<byte>, IPEndPoint> sendRawCallback;

    private Thread? resendThread;
    private volatile bool isRunning;

    private static readonly TimeSpan ResendInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaxRetryTime = TimeSpan.FromSeconds(10);
    private const int MaxResendAttempts = 50;

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
        {
            packet.Recycle();
            RecyclePool<PendingPacket>.Return(packet);
        }

        pendingPackets.Clear();
        lastProcessedSequence.Clear();
        sequenceNumbersByType.Clear();

        SrLogger.LogMessage("ReliabilityManager stopped", SrLogTarget.Both);
    }

    public void TrackPacket(SplitResult splitData, IPEndPoint destination, ushort packetId,
        byte packetType, PacketReliability reliability, ushort sequenceNumber)
    {
        if (reliability == PacketReliability.Unreliable)
            return;

        var key = new PacketKey(packetType, packetId, destination);
        var packet = RecyclePool<PendingPacket>.Borrow();
        packet.Initialize(splitData, destination, packetId, packetType, reliability, sequenceNumber);
        pendingPackets[key] = packet;
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
    public bool ShouldProcessOrderedPacket(IPEndPoint sender, ushort sequenceNumber, byte packetType)
    {
        var key = new SequenceKey(sender, packetType);

        if (!lastProcessedSequence.TryGetValue(key, out var lastSequence))
        {
            // First packet from this sender for this type
            lastProcessedSequence[key] = sequenceNumber;
            return true;
        }

        // Checks if this is the next expected sequence number
        var expectedSequence = (ushort)(lastSequence + 1);

        if (sequenceNumber == expectedSequence)
        {
            lastProcessedSequence[key] = sequenceNumber;
            return true;
        }

        if (IsSequenceNewer(sequenceNumber, lastSequence))
        {
            SrLogger.LogWarning(
                $"Out-of-order packet dropped: expected seq={expectedSequence}, got seq={sequenceNumber}, type={packetType}",
                SrLogTarget.Both);
        }

        return false;
    }

    // Gets the next sequence number for ReliableOrdered packets
    public ushort GetNextSequenceNumber(byte packetType)
    {
        var seq = sequenceNumbersByType.AddOrUpdate(
            packetType,
            1,
            (_, current) => (current >= ushort.MaxValue) ? 1 : current + 1
        );

        return (ushort)seq;
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
                        SrLogger.LogWarning(
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
                            SrLogger.LogWarning(
                                $"Resending packet {packet.PacketId} (type={packet.PacketType}) attempt #{packet.SendCount}",
                                SrLogTarget.Both);
                        }
                    }
                }

                // Removes timed out packets
                for (var i = 0; i < removeCount; i++)
                {
                    if (pendingPackets.TryRemove(keysToRemove[i], out var timedOutPacket))
                        RecyclePool<PendingPacket>.Return(timedOutPacket);
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