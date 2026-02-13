using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Client.Handlers.Internal;
using SR2MP.Packets.Gordo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoFeed)]
public sealed class GordoSlimeFeedHandler : BaseClientPacketHandler<GordoFeedPacket>
{
    public GordoSlimeFeedHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    protected override void Handle(GordoFeedPacket packet)
    {
        if (SceneContext.Instance.GameModel.gordos.TryGetValue(packet.ID, out var gordoSlime))
        {
            gordoSlime.GordoEatenCount = packet.NewFoodCount;
        }
        else
        {
            gordoSlime = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoEatCount = packet.NewFoodCount,
                gordoSeen = false,
                gameObj = null,
                targetCount = packet.RequiredFoodCount,
                identifiableType = actorManager.ActorTypes[packet.GordoType]
            };

            SceneContext.Instance.GameModel.gordos.Add(packet.ID, gordoSlime);
        }
    }
}