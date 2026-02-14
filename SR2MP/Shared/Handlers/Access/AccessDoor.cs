using System.Net;
using Il2CppMonomiPark.World;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Handlers.Internal;

namespace SR2MP.Shared.Handlers.Access;

[PacketHandler((byte)PacketType.AccessDoor)]
public sealed class AccessDoorHandler : BasePacketHandler<AccessDoorPacket>
{
    protected override bool Handle(AccessDoorPacket packet, IPEndPoint? _)
    {
        var model = SceneContext.Instance.GameModel.doors[packet.ID];

        handlingPacket = true;
        model.gameObj.GetComponent<AccessDoor>().CurrState = packet.State;
        handlingPacket = false;

        return true;
    }
}