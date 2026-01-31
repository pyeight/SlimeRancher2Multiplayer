using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

public abstract class BaseClientPacketHandler<T> where T : PacketBase, new()
{
    protected readonly Client Client;
    protected readonly RemotePlayerManager PlayerManager;

    protected BaseClientPacketHandler(Client client, RemotePlayerManager playerManager)
    {
        Client = client;
        PlayerManager = playerManager;
    }

    public abstract void Handle(T packet);
    
    protected void SendPacket<TOther>(TOther packet) where TOther : PacketBase
    {
        Client.SendPacket(packet);
    }
}