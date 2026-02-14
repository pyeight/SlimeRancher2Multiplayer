using SR2MP.Client.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Weather;

[PacketHandler((byte)PacketType.LightningStrike)]
public sealed class LightningStrikeHandler : BasePacketHandler<LightningStrikePacket>
{
    protected override bool Handle(LightningStrikePacket packet, IPEndPoint? _)
    {
        var lightning = Object.Instantiate(NetworkWeatherManager.Lightning.gameObject);
        lightning.name += " (Net)";
        lightning.transform.position = packet.Position;
        return true;
    }
}