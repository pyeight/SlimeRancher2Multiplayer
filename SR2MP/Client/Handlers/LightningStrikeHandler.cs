using SR2MP.Client.Managers;
using SR2MP.Packets.FX;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.LightningStrike)]
public sealed class LightningStrikeHandler : BaseClientPacketHandler<LightningStrikePacket>
{
    public LightningStrikeHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(LightningStrikePacket packet)
    {
        var lightning = Object.Instantiate(NetworkWeatherManager.Lightning.gameObject);
        lightning.name += " (Net)";
        lightning.transform.position = packet.Position;
    }
}