using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.World;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Handlers.Access;

[PacketHandler((byte)PacketType.AccessDoor)]
internal sealed class AccessDoorHandler : BasePacketHandler<AccessDoorPacket>
{
    protected override bool Handle(AccessDoorPacket packet, IPEndPoint? _)
    {
        if (!GameState.doors.TryGetValue(packet.ID, out var model))
        {
            model = new AccessDoorModel
            {
                gameObj = null,
                state = packet.State
            };

            GameState.doors.Add(packet.ID, model);
            return true;
        }

        model.state = packet.State;

        if (model.gameObj)
        {
            HandlingPacket = true;
            model.gameObj.GetComponent<AccessDoor>().CurrState = packet.State;
            HandlingPacket = false;
        }

        return true;
    }
}