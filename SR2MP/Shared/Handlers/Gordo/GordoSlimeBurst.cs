using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Gordo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoBurst)]
public sealed class GordoSlimeBurstHandler : BasePacketHandler<GordoBurstPacket>
{
    protected override bool Handle(GordoBurstPacket packet, IPEndPoint? _)
    {
        if (SceneContext.Instance.GameModel.gordos.TryGetValue(packet.ID, out var gordo))
        {
            gordo.GordoEatenCount = gordo.targetCount + 1;

            handlingPacket = true;

            if (gordo.gameObj)
                gordo.gameObj.GetComponent<GordoEat>().ImmediateReachedTarget();

            handlingPacket = false;
        }
        else
        {
            gordo = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoEatCount = 999999,
                gordoSeen = false,
                gameObj = null,
                targetCount = 50
            };

            SceneContext.Instance.GameModel.gordos.Add(packet.ID, gordo);
        }

        return true;
    }
}