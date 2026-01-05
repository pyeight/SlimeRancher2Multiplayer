using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Shared.Handlers;

public abstract class BaseSharedPacketHandler : ISharedPacketHandler
{
    protected BaseSharedPacketHandler(NetworkManager networkManager, ClientManager clientManager) {}
    protected BaseSharedPacketHandler(Client.Client client, RemotePlayerManager playerManager) {}
    public BaseSharedPacketHandler() {}

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