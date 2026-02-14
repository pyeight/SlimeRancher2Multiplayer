using SR2MP.Packets.Utils;
using SR2MP.Packets.World;
using SR2MP.Shared.Handlers.Internal;
using System.Net;

namespace SR2MP.Shared.Handlers.Refinery;

[PacketHandler((byte)PacketType.RefineryUpdate)]
public sealed class RefineryUpdateHandler : BasePacketHandler<RefineryUpdatePacket>
{
    protected override bool Handle(RefineryUpdatePacket packet, IPEndPoint? _)
    {
        if (!actorManager.ActorTypes.TryGetValue(packet.ItemID, out var identType))
            return false;

        handlingPacket = true;
        SceneContext.Instance.GadgetDirector._model.SetCount(identType, packet.ItemCount);
        handlingPacket = false;

        return true;
    }
}