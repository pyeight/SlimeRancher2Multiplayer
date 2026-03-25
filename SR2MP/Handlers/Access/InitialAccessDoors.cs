using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.World;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Access;

[PacketHandler((byte)PacketType.InitialAccessDoors, HandlerType.Client)]
internal sealed class InitialAccessDoorsHandler : BasePacketHandler<InitialAccessDoorsPacket>
{
    protected override bool Handle(InitialAccessDoorsPacket packet, IPEndPoint? _)
    {
        var gameModel = GameState;

        foreach (var door in packet.Doors)
        {
            if (gameModel.doors.TryGetValue(door.ID, out var doorModel))
            {
                doorModel.state = door.State;

                if (doorModel.gameObj)
                    doorModel.gameObj.GetComponent<AccessDoor>().CurrState = doorModel.state;
            }
            else
            {
                doorModel = new AccessDoorModel
                {
                    gameObj = null,
                    state = door.State
                };

                gameModel.doors.Add(door.ID, doorModel);
            }
        }

        return false;
    }
}