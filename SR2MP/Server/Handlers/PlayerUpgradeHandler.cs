using LiteNetLib;
using SR2MP.Packets.Upgrades;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerUpgrade)]
public sealed class PlayerUpgradeHandler : BasePacketHandler<PlayerUpgradePacket>
{
    public PlayerUpgradeHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(PlayerUpgradePacket packet, NetPeer senderEndPoint)
    {
        var model = SceneContext.Instance.PlayerState._model.upgradeModel;

        var upgrade = model.upgradeDefinitions.items._items.FirstOrDefault(
            x => x._uniqueId == packet.UpgradeID);

        handlingPacket = true;
        model.IncrementUpgradeLevel(upgrade);
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}