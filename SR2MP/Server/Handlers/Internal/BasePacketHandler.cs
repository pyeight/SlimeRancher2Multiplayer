using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

// ReSharper disable InconsistentNaming

namespace SR2MP.Server.Handlers.Internal;

public abstract class BasePacketHandler<T> : IServerPacketHandler where T : IPacket, new()
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