using System.Net;
using Il2CppMonomiPark.World;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.AccessDoor)]
public sealed class AccessDoorHandler : BasePacketHandler<AccessDoorPacket>
{
    public AccessDoorHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }
    
    public override void Handle(AccessDoorPacket packet, IPEndPoint senderEndPoint)
    {
        var model = SceneContext.Instance.GameModel.doors[packet.ID];
        
        handlingPacket = true;
        model.gameObj.GetComponent<AccessDoor>().CurrState = packet.State;
        handlingPacket = false;
        
        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}