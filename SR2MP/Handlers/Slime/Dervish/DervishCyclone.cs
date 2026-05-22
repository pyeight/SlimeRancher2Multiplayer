using System.Net;
using Il2CppMonomiPark.SlimeRancher.Slime.Dervish;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Slime.Dervish;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Slime.Dervish;

[PacketHandler((byte)PacketType.DervishCyclone)]
internal sealed class DervishCycloneHandler : BasePacketHandler<DervishCyclonePacket>
{
    protected override bool Handle(DervishCyclonePacket packet, IPEndPoint? _)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var model)) return false;

        if (!model.TryGetNetworkComponent(out var networkComponent)) return false;

        var spin = networkComponent.GetComponent<DervishSlimeSpin>();
        if (!spin) return false;

        HandlingPacket = true;

        if (packet.Active)
        {
            if (spin._cyclone) 
            {
                HandlingPacket = false;
                return false;
            }

            spin._floatDir = packet.FloatDir;

            var cyclone = spin.SpawnCyclone((DervishSlimeSpin.CycloneSize)packet.Size);
            if (!cyclone)
            {
                HandlingPacket = false;
                return false;
            }

            spin._cyclone = cyclone;
            spin._mode = DervishSlimeSpin.Mode.CYCLONE;
        }
        else
        {
            var vortexer = spin._currActorVortexer;
            if (vortexer)
            {
                vortexer.StartFade();

                var keepAligned = vortexer.GetComponent<KeepAlignedUnderActor>();
                if (keepAligned) keepAligned.enabled = false;
            }

            spin._cyclone = null;
            spin._mode = DervishSlimeSpin.Mode.NONE;
        }

        HandlingPacket = false;
        return true;
    }
}