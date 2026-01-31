using System;
using System.Net;

namespace SR2MP.Shared.Utils;

public static class JoinCode
{
    private const int Ipv4Bytes = 6;
    private const int Ipv6Bytes = 18;
    private const int Ipv4CodeLength = 9;
    private const int Ipv6CodeLength = 25;

    public static string Encode(IPAddress address, ushort port)
    {
        var bytes = address.GetAddressBytes();
        // I honestly don't think an ipv6 address can even make it here to be honest
        // But if it does somehow! we can support it!
        if (bytes.Length == 4)
            return EncodeIPv4(bytes, port);
        if (bytes.Length == 16)
            return EncodeIPv6(bytes, port);

        throw new ArgumentException("Only IPv4 and IPv6 addresses are supported.", nameof(address));
    }

    public static bool TryDecode(string code, out IPAddress address, out ushort port, out string error)
    {
        address = null!;
        port = 0;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(code))
        {
            error = "Join code is empty.";
            return false;
        }

        code = code.Trim();

        int expectedBytes;
        if (code.Length == Ipv4CodeLength)
        {
            expectedBytes = Ipv4Bytes;
        }
        else if (code.Length == Ipv6CodeLength)
        {
            expectedBytes = Ipv6Bytes;
        }
        else
        {
            error = "Invalid join code length.";
            return false;
        }

        try
        {
            var decoded = Base58.Decode(code);
            if (decoded.Length > expectedBytes)
            {
                int extra = decoded.Length - expectedBytes;
                bool leadingZeros = true;
                for (int i = 0; i < extra; i++)
                {
                    if (decoded[i] != 0)
                    {
                        leadingZeros = false;
                        break;
                    }
                }

                if (!leadingZeros)
                {
                    error = "Join code decoded to unexpected length.";
                    return false;
                }

                var trimmed = new byte[expectedBytes];
                Buffer.BlockCopy(decoded, extra, trimmed, 0, expectedBytes);
                decoded = trimmed;
            }

            if (decoded.Length < expectedBytes)
            {
                var padded = new byte[expectedBytes];
                Buffer.BlockCopy(decoded, 0, padded, expectedBytes - decoded.Length, decoded.Length);
                decoded = padded;
            }

            var ipBytes = new byte[expectedBytes - 2];
            Buffer.BlockCopy(decoded, 0, ipBytes, 0, ipBytes.Length);
            port = (ushort)((decoded[expectedBytes - 2] << 8) | decoded[expectedBytes - 1]);

            if (port == 0)
            {
                error = "Join code contains an invalid port.";
                return false;
            }

            address = new IPAddress(ipBytes);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Join code is invalid: {ex.Message}";
            return false;
        }
    }

    private static string EncodeIPv4(byte[] addressBytes, ushort port)
    {
        var bytes = new byte[Ipv4Bytes];
        Buffer.BlockCopy(addressBytes, 0, bytes, 0, 4);
        bytes[4] = (byte)(port >> 8);
        bytes[5] = (byte)port;

        var encoded = Base58.Encode(bytes);
        return encoded.PadLeft(Ipv4CodeLength, '1');
    }

    private static string EncodeIPv6(byte[] addressBytes, ushort port)
    {
        var bytes = new byte[Ipv6Bytes];
        Buffer.BlockCopy(addressBytes, 0, bytes, 0, 16);
        bytes[16] = (byte)(port >> 8);
        bytes[17] = (byte)port;

        var encoded = Base58.Encode(bytes);
        return encoded.PadLeft(Ipv6CodeLength, '1');
    }
}
