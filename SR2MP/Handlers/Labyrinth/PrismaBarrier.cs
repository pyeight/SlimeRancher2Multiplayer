using System.Net;
using Il2CppMonomiPark.SlimeRancher.Labyrinth;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.World;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Labyrinth;

[PacketHandler((byte)PacketType.PrismaBarrier)]
internal sealed class PrismaBarrierHandler : BasePacketHandler<PrismaBarrierPacket>
{
    protected override bool Handle(PrismaBarrierPacket packet, IPEndPoint? _)
    {
        if (GameState.AllPrismaBarriers().TryGetValue(packet.ID, out var model))
        {
            HandlingPacket = true;
            model.ActivationTime = packet.ActivationTime;
            model.NotifyParticipants();
            if (model._gameObj)
            {
                var barrier = model._gameObj.GetComponent<PrismaBarrier>();
                if (barrier)
                    barrier.SetActivationTime(packet.ActivationTime);
            }
            HandlingPacket = false;
        }
        return true;
    }
}
