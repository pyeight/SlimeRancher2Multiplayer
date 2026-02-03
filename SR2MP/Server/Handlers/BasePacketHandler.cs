using System.Net;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
// ReSharper disable InconsistentNaming

namespace SR2MP.Server.Handlers;

public abstract class BasePacketHandler<T> : IPacketHandler where T : IPacket, new()
{
    protected readonly NetworkManager networkManager;
    protected readonly ClientManager clientManager;

    protected BasePacketHandler(NetworkManager networkManager, ClientManager clientManager)
    {
        this.networkManager = networkManager;
        this.clientManager = clientManager;
    }

    public void Handle(byte[] data, IPEndPoint clientEp)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<T>();

        Handle(packet, clientEp);
    }

    protected abstract void Handle(T packet, IPEndPoint clientEp);
}