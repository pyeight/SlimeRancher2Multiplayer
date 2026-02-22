using System.Buffers;
using System.Collections.Concurrent;

namespace SR2MP.Shared.Utils;

public static class PacketDeduplication
{
    // Key format: "PacketType_UniqueId"
    private static readonly ConcurrentDictionary<PacketKey, DateTime> ProcessedPackets = new();

    private static readonly TimeSpan PacketMemoryDuration = TimeSpan.FromSeconds(30);

    private static int processCounter;
    private const int CleanupInterval = 100;

    public static bool IsDuplicate(PacketKey key)
    {
        if (++processCounter >= CleanupInterval)
        {
            processCounter = 0;
            Cleanup();
        }

        return !ProcessedPackets.TryAdd(key, DateTime.UtcNow);
    }

    public static void MarkProcessed(PacketKey key)
        => ProcessedPackets[key] = DateTime.UtcNow;

    public static void Clear()
    {
        ProcessedPackets.Clear();
        SrLogger.LogPacketSize("Packet deduplication cache cleared", SrLogTarget.Both);
        Cleanup();
    }

    private static void Cleanup()
    {
        var count = ProcessedPackets.Count;
        if (count == 0) return;

        var keysToRemove = ArrayPool<PacketKey>.Shared.Rent(count);
        var removeCount = 0;
        var now = DateTime.UtcNow;

        foreach (var kvp in ProcessedPackets)
        {
            if (now - kvp.Value > PacketMemoryDuration)
            {
                keysToRemove[removeCount++] = kvp.Key;
            }
        }

        for (var i = 0; i < removeCount; i++)
        {
            ProcessedPackets.TryRemove(keysToRemove[i], out _);
        }

        if (removeCount > 0)
        {
            SrLogger.LogPacketSize($"Cleaned up {removeCount} old packet records", SrLogTarget.Both);
        }

        ArrayPool<PacketKey>.Shared.Return(keysToRemove);
    }

    public static int GetTrackedPacketCount() => ProcessedPackets.Count;
}