using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Slime;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;

namespace SR2MP.Shared.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorUpdate)]
public sealed class ActorUpdateHandler : BasePacketHandler<ActorUpdatePacket>
{
    protected override bool Handle(ActorUpdatePacket packet, IPEndPoint? _)
    {
        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
            return false;

        var actor = model.Cast<ActorModel>();

        actor.lastPosition = packet.Position;
        actor.lastRotation = packet.Rotation;

        var slime = actor.TryCast<SlimeModel>();
        var resource = actor.TryCast<ProduceModel>();

        if (packet.UpdateType == ActorUpdateType.Resource && resource != null)
        {
            resource.state = packet.ResourceState;
            resource.progressTime = packet.ResourceProgress;
        }

        if (!actor.TryGetNetworkComponent(out var networkComponent))
            return false;
        
        networkComponent.OnNetworkUpdate(packet);

        if (networkComponent.regionMember?._hibernating == true)
        {
            networkComponent.transform.position = packet.Position;
            networkComponent.transform.rotation = packet.Rotation;
        }

        if (packet.UpdateType == ActorUpdateType.Slime && slime != null)
            networkComponent.GetComponent<SlimeEmotions>().SetAll(packet.Emotions);

        if (packet.UpdateType == ActorUpdateType.Resource && resource != null)
            networkComponent.SetResourceState(packet.ResourceState, packet.ResourceProgress);

        return true;
    }
}