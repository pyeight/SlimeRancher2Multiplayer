using System.Collections.Concurrent;
using System.Text;

namespace SR2MP.Shared.Utils;

/// <summary>
/// A pooling class to help pool reusable string objects.
/// </summary>
public static class NetworkStringPool
{
    private static readonly ConcurrentDictionary<uint, string> pool = new();

    /// <summary>
    /// Gets a string representing a span of bytes. If the string does not exist, it creates one and caches it for future use.
    /// </summary>
    /// <param name="utf8Bytes">The bytes of the string.</param>
    /// <returns>The string represented by the bytes.</returns>
    public static string GetOrAdd(ReadOnlySpan<byte> utf8Bytes)
    {
        if (utf8Bytes.IsEmpty)
            return string.Empty;

        var hash = HashCalculator.ComputeHashOfBytes(utf8Bytes);

        if (pool.TryGetValue(hash, out var cachedString))
            return cachedString;

        var newString = Encoding.UTF8.GetString(utf8Bytes);
        pool[hash] = newString;
        return newString;
    }

    internal static void Clear() => pool.Clear();
}