using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.World;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialAccessDoors)]
public sealed class DoorsLoadHandler : BaseClientPacketHandler<InitialAccessDoorsPacket>
{
    public DoorsLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(InitialAccessDoorsPacket packet)
    {
        var gameModel = SceneContext.Instance.GameModel;

        foreach (var door in packet.Doors)
        {
            if (gameModel.doors.TryGetValue(door.ID, out var doorModel))
            {
                doorModel.state = door.State;

                if (doorModel.gameObj)
                {
                    doorModel.gameObj.GetComponent<AccessDoor>().CurrState = doorModel.state;
                }
            }
            else
            {
                doorModel = new AccessDoorModel
                {
                    gameObj = null,
                    state = door.State,
                };

                gameModel.doors.Add(door.ID, doorModel);
            }
        }
    }
}