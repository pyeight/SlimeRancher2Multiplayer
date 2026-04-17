using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Handlers.Refinery;

[PacketHandler((byte)PacketType.RefineryUpdate)]
internal sealed class RefineryUpdateHandler : BasePacketHandler<RefineryUpdatePacket>
{
    protected override bool Handle(RefineryUpdatePacket packet, IPEndPoint? _)
    {
        if (!ActorManager.ActorTypes.TryGetValue(packet.ItemID, out var identType))
            return false;

        HandlingPacket = true;
        SceneContext.Instance.GadgetDirector._model.SetCount(identType, packet.ItemCount);
        HandlingPacket = false;

        return true;
    }
}