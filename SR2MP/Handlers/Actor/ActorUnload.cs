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
        
        if (!string.IsNullOrEmpty(component.CurrentOwnerId) &&
            component.CurrentOwnerId != packet.SenderId)
            return false;

        if (!component.RegionMember!._hibernating)
        {
            component.LocallyOwned = true;
            component.CurrentOwnerId = LocalID;

            Main.SendToAllOrServer(new ActorTransferPacket
            {
                ActorId = packet.ActorId,
                OwnerId = LocalID
            });
            return false;
        }
        
        component.LocallyOwned = false;
        return true;
    }
}