using LiteNetLib;
using Il2CppMonomiPark.World;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.AccessDoor)]
public sealed class AccessDoorHandler : BasePacketHandler<AccessDoorPacket>
{
    public AccessDoorHandler(ClientManager clientManager)
        : base(clientManager) { }
    
    public override void Handle(AccessDoorPacket packet, NetPeer senderEndPoint)
    {
        var model = SceneContext.Instance.GameModel.doors[packet.ID];
        
        handlingPacket = true;
        model.gameObj.GetComponent<AccessDoor>().CurrState = packet.State;
        handlingPacket = false;
        
        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}