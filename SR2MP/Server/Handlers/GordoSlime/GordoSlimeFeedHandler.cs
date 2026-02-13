using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Gordo;
using SR2MP.Packets.Utils;
using SR2MP.Server.Handlers.Internal;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoFeed)]
public sealed class GordoSlimeFeedHandler : BasePacketHandler<GordoFeedPacket>
{
    public GordoSlimeFeedHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(GordoFeedPacket packet, IPEndPoint clientEp)
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