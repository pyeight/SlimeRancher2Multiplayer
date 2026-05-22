namespace SR2MP.Shared.Utils;

internal static class HashCalculator
{
    private const uint Offset = 2166136261u;
    private const uint Prime = 16777619u;

    // public static ushort FoldHash(uint hash) => (ushort)(hash ^ (hash >> 16));

    public static uint ComputeHashOfBytes(ReadOnlySpan<byte> bytes)
    {
        unchecked
        {
            var hash = Offset;

            foreach (var b in bytes)
            {
                hash ^= b;
                hash *= Prime;
            }

            return hash;
        }
    }

    public static uint ComputeHashOfString(string @string)
    {
        unchecked
        {
            var hash = Offset;

            foreach (var ch in @string)
            {
                hash ^= ch;
                hash *= Prime;
            }

            return hash;
        }
    }

    public static uint Hash32(this string defName)
    {
        uint number = uint.MaxValue;
        foreach (var c in defName)
            number = ((number << 11) + number + c);
        return number;
    }

    public static ushort Hash16(this string defName)
    {
        ushort number = 65535;
        foreach (var c in defName)
            number = (ushort)((number << 5) + number + c);
        return number;
    }
}