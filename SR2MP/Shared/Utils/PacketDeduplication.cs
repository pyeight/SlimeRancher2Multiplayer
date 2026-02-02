using System.Collections.Concurrent;

namespace SR2MP.Shared.Utils;

public static class PacketDeduplication
{
    // Key format: "PacketType_UniqueId"
    private static readonly ConcurrentDictionary<string, DateTime> ProcessedPackets = new();
    
    private static readonly TimeSpan PacketMemoryDuration = TimeSpan.FromSeconds(30);
    
    private static int processCounter = 0;
    private const int CleanupInterval = 100;
    
    public static bool IsDuplicate(string packetType, string uniqueId)
    {
        var key = $"{packetType}_{uniqueId}";
        
        if (++processCounter >= CleanupInterval)
        {
            processCounter = 0;
            Cleanup();
        }
        
        if (ProcessedPackets.TryAdd(key, DateTime.UtcNow))
        {
            return false;
        }
        
        return true;
    }
    
    public static void MarkProcessed(string packetType, string uniqueId)
    {
        var key = $"{packetType}_{uniqueId}";
        ProcessedPackets[key] = DateTime.UtcNow;
    }

    public static void Clear()
    {
        ProcessedPackets.Clear();
        SrLogger.LogPacketSize("Packet deduplication cache cleared", SrLogTarget.Both);
        Cleanup();
    }
    
    private static void Cleanup()
    {
        var now = DateTime.UtcNow;
        var toRemove = new List<string>();

        foreach (var kvp in ProcessedPackets)
        {
            if (now - kvp.Value > PacketMemoryDuration)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            ProcessedPackets.TryRemove(key, out _);
        }

        if (toRemove.Count > 0)
        {
            SrLogger.LogPacketSize($"Cleaned up {toRemove.Count} old packet records", SrLogTarget.Both);
        }
    }

    public static int GetTrackedPacketCount() => ProcessedPackets.Count;
}