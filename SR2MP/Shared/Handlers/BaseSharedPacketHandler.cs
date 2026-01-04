using System.Net;
using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Handlers;

public abstract class BaseSharedPacketHandler : IServerPacketHandler, IClientPacketHandler
{
    public void HandleServer(byte[] data, IPEndPoint clientEp)
    {
        Handle(data, clientEp);
    }

    public void HandleClient(byte[] data)
    {
        Handle(data);
    }

    public abstract void Handle(byte[] data, IPEndPoint? clientEp = null);
}