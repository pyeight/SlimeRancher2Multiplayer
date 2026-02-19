using System.Collections.Concurrent;
using System.Net;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Managers;

public sealed class ReliabilityManager
{
    private sealed class PendingPacket
    {
        public byte[][] Chunks { get; init; } = null!;
        public IPEndPoint Destination { get; init; } = null!;
        public ushort PacketId { get; init; }
        public byte PacketType { get; init; }
        public PacketReliability Reliability { get; init; }
        public DateTime FirstSendTime { get; init; }
        public DateTime LastSendTime { get; set; }
        public int SendCount { get; set; }
        public ushort SequenceNumber { get; init; }
    }

    private readonly ConcurrentDictionary<string, PendingPacket> pendingPackets = new();
    private readonly ConcurrentDictionary<string, ushort> lastProcessedSequence = new();

    private readonly ConcurrentDictionary<byte, int> sequenceNumbersByType = new();

    private readonly Action<byte[], IPEndPoint> sendRawCallback;

    private Thread? resendThread;
    private volatile bool isRunning;

    private static readonly TimeSpan ResendInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaxRetryTime = TimeSpan.FromSeconds(10);
    private const int MaxResendAttempts = 50;

    public ReliabilityManager(Action<byte[], IPEndPoint> sendRawCallback)
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
        pendingPackets.Clear();
        lastProcessedSequence.Clear();
        sequenceNumbersByType.Clear();

        SrLogger.LogMessage("ReliabilityManager stopped", SrLogTarget.Both);
    }

    public void TrackPacket(byte[][] chunks, IPEndPoint destination, ushort packetId,
        byte packetType, PacketReliability reliability, ushort sequenceNumber)
    {
        if (reliability == PacketReliability.Unreliable)
            return;

        var key = GetPacketKey(destination, packetId);
        pendingPackets[key] = new PendingPacket
        {
            Chunks = chunks,
            Destination = destination,
            PacketId = packetId,
            PacketType = packetType,
            Reliability = reliability,
            FirstSendTime = DateTime.UtcNow,
            LastSendTime = DateTime.UtcNow,
            SendCount = 1,
            SequenceNumber = sequenceNumber
        };
    }

    public void HandleAck(IPEndPoint sender, ushort packetId, byte packetType)
    {
        var key = GetPacketKey(sender, packetId);

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
        var key = GetSequenceKey(sender, packetType);

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
                var now = DateTime.UtcNow;
                var toRemove = new List<string>();

                foreach (var kvp in pendingPackets)
                {
                    var packet = kvp.Value;

                    // Checks if packet has timed out
                    if (now - packet.FirstSendTime > MaxRetryTime || packet.SendCount >= MaxResendAttempts)
                    {
                        SrLogger.LogWarning(
                            $"Packet {packet.PacketId} (type={packet.PacketType}) failed after {packet.SendCount} attempts",
                            SrLogTarget.Both);
                        toRemove.Add(kvp.Key);
                        continue;
                    }

                    // Checks if packet should be resent
                    if (now - packet.LastSendTime > ResendInterval)
                    {
                        foreach (var chunk in packet.Chunks)
                        {
                            sendRawCallback(chunk, packet.Destination);
                        }

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
                foreach (var key in toRemove)
                {
                    pendingPackets.TryRemove(key, out _);
                }

                // todo: Should not cause problems, if it does, remove
                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"ResendLoop error: {ex}", SrLogTarget.Both);
            }
        }
    }

    private static string GetPacketKey(IPEndPoint endpoint, ushort packetId)
    {
        return $"{endpoint}_{packetId}";
    }

    private static string GetSequenceKey(IPEndPoint endpoint, byte packetType)
    {
        return $"{endpoint}_{packetType}";
    }

    private static bool IsSequenceNewer(ushort s1, ushort s2)
    {
        return ((s1 > s2) && (s1 - s2 <= 32768)) ||
               ((s1 < s2) && (s2 - s1 > 32768));
    }

    public int GetPendingPacketCount() => pendingPackets.Count;
}