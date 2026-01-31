using LiteNetLib;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
// ReSharper disable InconsistentNaming

namespace SR2MP.Server.Handlers;

public abstract class BasePacketHandler<T> where T : PacketBase, new()
{
    protected readonly ClientManager clientManager;

    protected BasePacketHandler(ClientManager clientManager)
    {
        this.clientManager = clientManager;
    }

    public abstract void Handle(T packet, NetPeer clientPeer);
}
