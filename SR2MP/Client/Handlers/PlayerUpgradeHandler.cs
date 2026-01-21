using SR2MP.Packets.Upgrades;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.PlayerUpgrade)]
public sealed class PlayerUpgradeHandler : BaseClientPacketHandler<PlayerUpgradePacket>
{
    public PlayerUpgradeHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(PlayerUpgradePacket packet)
    {
        var model = SceneContext.Instance.PlayerState._model.upgradeModel;

        var upgrade = model.upgradeDefinitions.items._items.FirstOrDefault(
            x => x._uniqueId == packet.UpgradeID);

        handlingPacket = true;
        model.IncrementUpgradeLevel(upgrade);
        handlingPacket = false;
    }
}