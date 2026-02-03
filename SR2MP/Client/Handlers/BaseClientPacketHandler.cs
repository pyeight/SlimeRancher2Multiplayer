using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

public abstract class BaseClientPacketHandler<T> : IClientPacketHandler where T : IPacket, new()
{
    protected readonly Client Client;
    protected readonly RemotePlayerManager PlayerManager;

    protected BaseClientPacketHandler(Client client, RemotePlayerManager playerManager)
    {
        Client = client;
        PlayerManager = playerManager;
    }

    public void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<T>();

        Handle(packet);
    }

    public abstract void Handle(T packet);

    protected void SendPacket<TOther>(TOther packet) where TOther : IPacket
    {
        Client.SendPacket(packet);
    }
}