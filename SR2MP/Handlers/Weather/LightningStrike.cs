using System.Net;
using SR2MP.Client.Managers;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Handlers.Weather;

[PacketHandler((byte)PacketType.LightningStrike)]
internal sealed class LightningStrikeHandler : BasePacketHandler<LightningStrikePacket>
{
    protected override bool Handle(LightningStrikePacket packet, IPEndPoint? _)
    {
        var lightning = Object.Instantiate(NetworkWeatherManager.Lightning.gameObject);
        lightning.name += " (Net)";
        lightning.transform.position = packet.Position;
        return true;
    }
}