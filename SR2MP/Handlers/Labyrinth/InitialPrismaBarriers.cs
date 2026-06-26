using System.Net;
using Il2CppMonomiPark.SlimeRancher.Labyrinth;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.InitialPrismaBarriers, HandlerType.Client)]
internal sealed class InitialPrismaBarriersHandler : BasePacketHandler<InitialPrismaBarriersPacket>
{
    protected override bool Handle(InitialPrismaBarriersPacket packet, IPEndPoint? _)
    {
        foreach (var barrier in packet.Barriers)
        {
            if (GameState.AllPrismaBarriers().TryGetValue(barrier.ID, out var barrierModel))
            {
                barrierModel.ActivationTime = barrier.ActivationTime;
                if (barrierModel._gameObj)
                {
                    var barrierComponent = barrierModel._gameObj.GetComponent<PrismaBarrier>();
                    if (barrierComponent)
                        barrierComponent.SetActivationTime(barrier.ActivationTime);
                }
            }
            else
            {
                barrierModel = new PrismaBarrierModel(barrier.ID)
                {
                    _gameObj = null,
                    ActivationTime = barrier.ActivationTime
                };
                GameState.AllPrismaBarriers().Add(barrier.ID, barrierModel);
            }
        }

        return false;
    }
}
