using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.World;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;

namespace SR2MP.Shared.Handlers.Access;

[PacketHandler((byte)PacketType.InitialAccessDoors, HandlerType.Client)]
public sealed class InitialAccessDoorsHandler : BasePacketHandler<InitialAccessDoorsPacket>
{
    public InitialAccessDoorsHandler(bool isServerSide) : base(isServerSide) { }

    protected override bool Handle(InitialAccessDoorsPacket packet, IPEndPoint? _)
    {
        var gameModel = SceneContext.Instance.GameModel;

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