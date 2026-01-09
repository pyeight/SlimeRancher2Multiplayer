using SR2MP.Packets.Time;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.WorldTime)]
public sealed class WorldTimeHandler : BaseClientPacketHandler
{
    public WorldTimeHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<WorldTimePacket>();

        SceneContext.Instance.TimeDirector._worldModel.worldTime = packet.Time;
    }
}