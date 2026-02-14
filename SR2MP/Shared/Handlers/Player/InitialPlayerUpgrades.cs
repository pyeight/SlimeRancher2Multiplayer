using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Player;

[PacketHandler((byte)PacketType.InitialPlayerUpgrades, HandlerType.Client)]
public sealed class PlayerUpgradesLoadHandler : BasePacketHandler<InitialUpgradesPacket>
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