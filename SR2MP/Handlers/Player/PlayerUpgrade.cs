using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Upgrade;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerUpgrade)]
public sealed class PlayerUpgradeHandler : BasePacketHandler<PlayerUpgradePacket>
{
    protected override bool Handle(PlayerUpgradePacket packet, IPEndPoint? _)
    {
        var model = SceneContext.Instance.PlayerState._model.upgradeModel;

        var upgrade = model.upgradeDefinitions.items._items.FirstOrDefault(
            x => x._uniqueId == packet.UpgradeID);

        HandlingPacket = true;
        model.IncrementUpgradeLevel(upgrade);
        HandlingPacket = false;

        return true;
    }
}