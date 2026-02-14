using System.Net;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;

namespace SR2MP.Shared.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorUnload)]
public sealed class ActorUnloadHandler : BasePacketHandler<ActorUnloadPacket>
{
    public ActorUnloadHandler(bool isServerSide) : base(isServerSide) { }

    protected override bool Handle(ActorUnloadPacket packet, IPEndPoint? _)
    {
        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return false;

        if (!actor.TryGetNetworkComponent(out var component))
            return false;

        if (!component.regionMember)
            return false;

        if (!component.regionMember!._hibernating)
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