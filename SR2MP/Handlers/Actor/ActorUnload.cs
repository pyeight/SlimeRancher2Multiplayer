using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorUnload)]
internal sealed class ActorUnloadHandler : BasePacketHandler<ActorUnloadPacket>
{
    protected override bool Handle(ActorUnloadPacket packet, IPEndPoint? _)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return false;

        if (!actor.TryGetNetworkComponent(out var component))
            return false;

        if (!component.RegionMember)
            return false;

        if (!component.RegionMember!._hibernating)
        {
            component.LocallyOwned = true;

            var ownershipPacket = new ActorTransferPacket
            {
                ActorId = packet.ActorId,
                OwnerId = LocalID
            };
            Main.SendToAllOrServer(ownershipPacket);
            return false;
        }

        return true;
    }
}