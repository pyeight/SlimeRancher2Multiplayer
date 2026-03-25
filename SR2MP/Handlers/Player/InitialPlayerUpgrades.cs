using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Player;

[PacketHandler((byte)PacketType.InitialPlayerUpgrades, HandlerType.Client)]
internal sealed class PlayerUpgradesLoadHandler : BasePacketHandler<InitialUpgradesPacket>
{
    protected override bool Handle(InitialUpgradesPacket packet, IPEndPoint? _)
    {
        var upgradesList = GameContext.Instance.LookupDirector._upgradeDefinitions;

        foreach (var upgradeLevel in packet.Upgrades)
        {
            var upgrade = upgradesList.items._items.FirstOrDefault(x => x._uniqueId == upgradeLevel.Key);
            SceneContext.Instance.PlayerState._model.upgradeModel.SetUpgradeLevel(upgrade, upgradeLevel.Value);
        }

        return false;
    }
}