using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialPlayerUpgrades)]
public sealed class PlayerUpgradesLoadHandler : BaseClientPacketHandler<UpgradesPacket>
{
    public PlayerUpgradesLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(UpgradesPacket packet)
    {
        var upgradesList = GameContext.Instance.LookupDirector._upgradeDefinitions;
        foreach (var upgradeLevel in packet.Upgrades)
        {
            var upgrade = upgradesList.items._items.FirstOrDefault(x => x._uniqueId == upgradeLevel.Key);

            SceneContext.Instance.PlayerState._model.upgradeModel.SetUpgradeLevel(upgrade, upgradeLevel.Value);
        }
    }
}