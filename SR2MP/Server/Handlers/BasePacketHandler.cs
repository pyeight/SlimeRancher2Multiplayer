using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
// ReSharper disable InconsistentNaming

namespace SR2MP.Server.Handlers;

public abstract class BaseServerPacketHandler : IServerPacketHandler
{
    protected readonly NetworkManager networkManager;
    protected readonly ClientManager clientManager;

    protected BaseServerPacketHandler(NetworkManager networkManager, ClientManager clientManager)
    {
        this.networkManager = networkManager;
        this.clientManager = clientManager;
    }

    public abstract void HandleServer(byte[] data, IPEndPoint clientEp);
}