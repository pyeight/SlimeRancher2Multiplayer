using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.GordoSlime;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoBurst)]
public sealed class GordoSlimeBurstHandler : BasePacketHandler<GordoSlimeBurstPacket>
{
    protected override bool Handle(GordoSlimeBurstPacket packet, IPEndPoint? _)
    {
        if (GameState.gordos.TryGetValue(packet.ID, out var gordoSlime))
        {
            gordoSlime.GordoEatenCount = gordoSlime.targetCount + 1;

            HandlingPacket = true;

            if (gordoSlime.gameObj)
                gordoSlime.gameObj.GetComponent<GordoEat>().ImmediateReachedTarget();

            HandlingPacket = false;
        }
        else
        {
            gordoSlime = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoEatCount = 999999,
                gordoSeen = false,
                gameObj = null,
                targetCount = 50
            };

            GameState.gordos.Add(packet.ID, gordoSlime);
        }

        return true;
    }
}