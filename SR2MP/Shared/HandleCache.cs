using System.Net;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared;

public readonly struct ServerHandleCache
{
    public readonly PacketReader Reader;
    public readonly IServerPacketHandler Handler;
    public readonly IPEndPoint ClientEp;
    
    public ServerHandleCache(PacketReader reader, IServerPacketHandler handler, IPEndPoint clientEp)
    {
        Reader = reader;
        Handler = handler;
        ClientEp = clientEp;
    }
}

public readonly struct ClientHandleCache
{
    public readonly PacketReader Reader;
    public readonly IClientPacketHandler Handler;
    
    public ClientHandleCache(PacketReader reader, IClientPacketHandler handler)
    {
        Reader = reader;
        Handler = handler;
    }
}