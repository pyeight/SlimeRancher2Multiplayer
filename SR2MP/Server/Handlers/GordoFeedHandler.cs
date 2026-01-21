using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Gordo;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.GordoFeed)]
public sealed class GordoFeedHandler : BasePacketHandler<GordoFeedPacket>
{
    public GordoFeedHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(GordoFeedPacket packet, IPEndPoint clientEp)
    {
        if (SceneContext.Instance.GameModel.gordos.TryGetValue(packet.ID, out var gordo))
        {
            gordo.GordoEatenCount = packet.NewFoodCount;
        }
        else
        {
            gordo = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoEatCount = packet.NewFoodCount,
                gordoSeen = false,
                gameObj = null,
                targetCount = packet.RequiredFoodCount,
                identifiableType = actorManager.ActorTypes[packet.GordoType]
            };

            SceneContext.Instance.GameModel.gordos.Add(packet.ID, gordo);
        }

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}