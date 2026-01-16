using Il2CppMonomiPark.SlimeRancher.World;
using Il2CppMonomiPark.World;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.AccessDoor)]
public sealed class AccessDoorHandler : BaseClientPacketHandler
{
    public AccessDoorHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }
    
    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<AccessDoorPacket>();

        var model = SceneContext.Instance.GameModel.doors[packet.ID];
        
        handlingPacket = true;
        model.gameObj.GetComponent<AccessDoor>().CurrState = packet.State;
        handlingPacket = false;
    }
}