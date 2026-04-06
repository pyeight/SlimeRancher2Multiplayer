using System.Collections.Concurrent;
using System.Text;

namespace SR2MP.Packets.Utils;

internal static class NetworkStringPool
{
    private static readonly ConcurrentDictionary<uint, string> pool = new();

    public static string GetOrAdd(Span<byte> utf8Bytes)
    {
        if (utf8Bytes.IsEmpty)
            return string.Empty;

        var hash = ComputeFNV1aHash(utf8Bytes);

        if (pool.TryGetValue(hash, out var cachedString))
            return cachedString;

        var newString = Encoding.UTF8.GetString(utf8Bytes);
        pool[hash] = newString;
        return newString;
    }

    private static uint ComputeFNV1aHash(Span<byte> bytes)
    {
        unchecked
        {
            const uint fnvOffset = 2166136261;
            const uint fnvPrime = 16777619;

            var hash = fnvOffset;

            foreach (var b in bytes)
            {
                hash ^= b;
                hash *= fnvPrime;
            }

            return hash;
        }
    }

    public static void Clear() => pool.Clear();
}