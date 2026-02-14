using SR2MP.Packets.Upgrades;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerUpgrade)]
public sealed class PlayerUpgradeHandler : BasePacketHandler<PlayerUpgradePacket>
{
    public PlayerUpgradeHandler(bool isServerSide)
        : base(isServerSide) { }

    protected override bool Handle(PlayerUpgradePacket packet, IPEndPoint? _)
    {
        var model = SceneContext.Instance.PlayerState._model.upgradeModel;

        var upgrade = model.upgradeDefinitions.items._items.FirstOrDefault(
            x => x._uniqueId == packet.UpgradeID);

        handlingPacket = true;
        model.IncrementUpgradeLevel(upgrade);
        handlingPacket = false;

        return true;
    }
}