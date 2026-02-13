using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.Internal;

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

    protected abstract void Handle(T packet);

    protected void SendPacket<TOther>(TOther packet) where TOther : IPacket
    {
        Client.SendPacket(packet);
    }
}