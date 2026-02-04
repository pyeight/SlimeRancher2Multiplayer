using SR2MP.Packets.World;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.WorldTime)]
public sealed class WorldTimeHandler : BaseClientPacketHandler<WorldTimePacket>
{
    public WorldTimeHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(WorldTimePacket packet)
    {
        SceneContext.Instance.TimeDirector._worldModel.worldTime = packet.Time;
    }
}