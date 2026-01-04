using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.BroadcastFastForward)]
public sealed class FastForwardHandler : BaseClientPacketHandler
{
    public FastForwardHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void HandleClient(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<WorldTimePacket>();

        handlingPacket = true;
        SceneContext.Instance.TimeDirector.FastForwardTo(packet.Time);
        handlingPacket = false;
    }
}