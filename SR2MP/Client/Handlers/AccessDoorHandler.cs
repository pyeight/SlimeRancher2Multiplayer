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
        
        handlingPacket = true;
        SceneContext.Instance.GameModel.doors[packet.ID]
            .gameObj.GetComponent<AccessDoor>()
            .CurrState = AccessDoor.State.OPEN;
        handlingPacket = false;
    }
}