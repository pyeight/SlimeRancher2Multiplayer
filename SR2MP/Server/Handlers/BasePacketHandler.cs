using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Server.Models;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

public abstract class BasePacketHandler : IPacketHandler
{
    protected readonly NetworkManager networkManager;
    protected readonly ClientManager clientManager;

    protected BasePacketHandler(NetworkManager networkManager, ClientManager clientManager)
    {
        this.networkManager = networkManager;
        this.clientManager = clientManager;
    }

    public abstract void Handle(byte[] data, IPEndPoint senderEndPoint);
}