using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Shared.Handlers;

[PacketHandler((byte)PacketType.PlayerUpgrade)]
public sealed class PlayerUpgradeHandler : BaseSharedPacketHandler
{
    public PlayerUpgradeHandler(NetworkManager networkManager, ClientManager clientManager) {}
    public PlayerUpgradeHandler(Client.Client client, RemotePlayerManager playerManager) {}
    public override void Handle(byte[] data, IPEndPoint? clientEp = null)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<PlayerUpgradePacket>();

        var model = SceneContext.Instance.PlayerState._model.upgradeModel;

        var upgrade = model.upgradeDefinitions.items._items.FirstOrDefault(
            x => x._uniqueId == packet.UpgradeID);
       

        handlingPacket = true;
        model.IncrementUpgradeLevel(upgrade);
        handlingPacket = false;
        
        if (clientEp != null)
            Main.Server.SendToAllExcept(packet, clientEp);
    }
}