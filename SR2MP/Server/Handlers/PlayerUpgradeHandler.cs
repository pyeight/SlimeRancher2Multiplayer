using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.PlayerUpgrade)]
public sealed class PlayerUpgradeHandler : BasePacketHandler
{
    public PlayerUpgradeHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, string clientIdentifier)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerUpgradePacket>();

        var model = SceneContext.Instance.PlayerState._model.upgradeModel;

        var upgrade = model.upgradeDefinitions.items._items.FirstOrDefault(
            x => x._uniqueId == packet.UpgradeID);
       

        handlingPacket = true;
        model.IncrementUpgradeLevel(upgrade);
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, clientIdentifier);
    }
}