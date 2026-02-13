using System.Net;
using SR2MP.Client.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Server.Handlers.Internal;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers.Weather;

[PacketHandler((byte)PacketType.LightningStrike)]
public sealed class LightningStrikeHandler : BasePacketHandler<LightningStrikePacket>
{
    public LightningStrikeHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(LightningStrikePacket packet, IPEndPoint clientEp)
    {
        var lightning = Object.Instantiate(NetworkWeatherManager.Lightning.gameObject);
        lightning.name += " (Net)";
        lightning.transform.position = packet.Position;

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}