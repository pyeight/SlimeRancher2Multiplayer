using SR2MP.Client.Managers;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

public abstract class BaseClientPacketHandler : IClientPacketHandler
{
    protected readonly Client Client;
    protected readonly RemotePlayerManager PlayerManager;

    protected BaseClientPacketHandler(Client client, RemotePlayerManager playerManager)
    {
        Client = client;
        PlayerManager = playerManager;
    }

    public abstract void Handle(byte[] data);

    protected void SendPacket(IPacket packet)
    {
        Client.SendPacket(packet);
    }
}