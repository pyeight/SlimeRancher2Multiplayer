using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Gordo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoFeed)]
public sealed class GordoSlimeFeedHandler : BasePacketHandler<GordoFeedPacket>
{
    protected override bool Handle(GordoFeedPacket packet, IPEndPoint? _)
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

        return true;
    }
}