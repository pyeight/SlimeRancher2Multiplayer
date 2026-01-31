using System.Net;
using LiteNetLib.Utils;

namespace SR2MP.Shared.Utils;

// Lets us see if a server is online, reachable, and healthy before attempting connection
public static class ServerInfoProtocol
{
    private static readonly byte[] Magic = { (byte)'S', (byte)'R', (byte)'2', (byte)'I' };
    private const byte Version = 1;
    private const byte RequestType = 1;
    private const byte ResponseType = 2;

    public static void WriteRequest(NetDataWriter writer)
    {
        writer.Reset();
        writer.Put(Magic);
        writer.Put(Version);
        writer.Put(RequestType);
    }

    public static void WriteResponse(NetDataWriter writer, IReadOnlyList<(string PlayerId, string Username)> players)
    {
        writer.Reset();
        writer.Put(Magic);
        writer.Put(Version);
        writer.Put(ResponseType);
        writer.Put((ushort)players.Count);
        foreach (var (playerId, username) in players)
        {
            writer.Put(playerId ?? string.Empty);
            writer.Put(username ?? string.Empty);
        }
    }

    public static bool TryReadRequest(NetDataReader reader)
    {
        if (!TryReadHeader(reader, out var type))
            return false;
        return type == RequestType;
    }

    public static bool TryReadResponse(NetDataReader reader, out List<(string PlayerId, string Username)> players)
    {
        players = new List<(string PlayerId, string Username)>();
        if (!TryReadHeader(reader, out var type))
            return false;
        if (type != ResponseType)
            return false;

        if (reader.GetRemainingBytesSpan().Length < sizeof(ushort))
            return false;

        var count = reader.GetUShort();
        for (int i = 0; i < count; i++)
        {
            var playerId = reader.GetString();
            var username = reader.GetString();
            players.Add((playerId, username));
        }

        return true;
    }

    private static bool TryReadHeader(NetDataReader reader, out byte type)
    {
        type = 0;
        var remaining = reader.GetRemainingBytesSpan().Length;
        if (remaining < Magic.Length + 2)
            return false;

        var magic = new byte[Magic.Length];
        reader.GetBytes(magic, 0, magic.Length);
        for (int i = 0; i < Magic.Length; i++)
        {
            if (magic[i] != Magic[i])
                return false;
        }

        var version = reader.GetByte();
        if (version != Version)
            return false;

        type = reader.GetByte();
        return true;
    }
}
