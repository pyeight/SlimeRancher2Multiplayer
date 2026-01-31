using System;
using System.Numerics;
using System.Text;

namespace SR2MP.Shared.Utils;

// We use Base58 because it excludes characters that look similar to eachother unlike Base64
// It encodes ipv4 + port to 9 characters, so its actually feasible to type
// The drawback is that we have to actually write it but oh well
public static class Base58
{
    private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private static readonly int[] Indexes = CreateIndexes();

    public static string Encode(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (data.Length == 0)
            return string.Empty;

        int zeroCount = 0;
        while (zeroCount < data.Length && data[zeroCount] == 0)
            zeroCount++;

        var intData = new BigInteger(data, isUnsigned: true, isBigEndian: true);
        var sb = new StringBuilder();
        while (intData > 0)
        {
            intData = BigInteger.DivRem(intData, 58, out var remainder);
            sb.Insert(0, Alphabet[(int)remainder]);
        }

        for (int i = 0; i < zeroCount; i++)
            sb.Insert(0, '1');

        return sb.ToString();
    }

    public static byte[] Decode(string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.Length == 0)
            return Array.Empty<byte>();

        int zeroCount = 0;
        while (zeroCount < input.Length && input[zeroCount] == '1')
            zeroCount++;

        BigInteger intData = BigInteger.Zero;
        foreach (char c in input)
        {
            int digit = c < 128 ? Indexes[c] : -1;
            if (digit < 0)
                throw new FormatException($"Invalid Base58 character '{c}'.");

            intData = (intData * 58) + digit;
        }

        var bytes = intData.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (bytes.Length == 0)
            bytes = Array.Empty<byte>();

        var result = new byte[zeroCount + bytes.Length];
        Buffer.BlockCopy(bytes, 0, result, zeroCount, bytes.Length);
        return result;
    }

    private static int[] CreateIndexes()
    {
        var indexes = new int[128];
        for (int i = 0; i < indexes.Length; i++)
            indexes[i] = -1;

        for (int i = 0; i < Alphabet.Length; i++)
            indexes[Alphabet[i]] = i;

        return indexes;
    }
}
