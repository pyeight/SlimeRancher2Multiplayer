using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorUpdate)]
internal sealed class ActorUpdateHandler : BasePacketHandler<ActorUpdatePacket>
{
    protected override bool Handle(ActorUpdatePacket packet, IPEndPoint? _)
    {
        if (!ActorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            return false;

        var actor = model.Cast<ActorModel>();

        actor.lastPosition = packet.Position;
        actor.lastRotation = packet.Rotation;

        if (!actor.TryGetNetworkComponent(out var networkComponent))
            return false;

        networkComponent.OnNetworkWorldUpdate(packet);

        if (networkComponent.RegionMember?._hibernating == true)
        {
            networkComponent.transform.position = packet.Position;
            networkComponent.transform.rotation = packet.Rotation;
        }

        return true;
    }
}