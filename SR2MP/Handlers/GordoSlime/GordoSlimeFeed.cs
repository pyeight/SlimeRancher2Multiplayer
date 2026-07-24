using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.GordoSlime;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoSlimeFeed)]
internal sealed class GordoSlimeFeedHandler : BasePacketHandler<GordoSlimeFeedPacket>
{
    protected override bool Handle(GordoSlimeFeedPacket packet, IPEndPoint? _)
    {
        if (GameState.gordos.TryGetValue(packet.ID, out var gordoSlime))
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
                identifiableType = ActorManager.ActorTypes[packet.GordoSlimeType]
            };

            GameState.gordos.Add(packet.ID, gordoSlime);
        }

        return true;
    }
}