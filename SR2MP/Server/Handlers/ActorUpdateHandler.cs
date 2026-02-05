using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Slime;
using SR2MP.Packets.Actor;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorUpdate)]
public sealed class ActorUpdateHandler : BasePacketHandler<ActorUpdatePacket>
{
    public ActorUpdateHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(ActorUpdatePacket packet, IPEndPoint clientEp)
    {
        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
        {
            return;
        }
        var actor = model.Cast<ActorModel>();

        actor.lastPosition = packet.Position;
        actor.lastRotation = packet.Rotation;

        var slime = actor.TryCast<SlimeModel>();
        if (slime != null)
            slime.Emotions = packet.Emotions;
        var resource = actor.TryCast<ProduceModel>();
        if (resource != null)
        {
            resource.state = packet.ResourceState;
            resource.progressTime = packet.ResourceProgress;
        }
        if (!actor.TryGetNetworkComponent(out var networkComponent))
            return;

        networkComponent.SavedVelocity = packet.Velocity;
        networkComponent.nextPosition = packet.Position;
        networkComponent.nextRotation = packet.Rotation;

        if (networkComponent.regionMember?._hibernating == true)
        {
            networkComponent.transform.position = packet.Position;
            networkComponent.transform.rotation = packet.Rotation;
        }

        if (slime != null)
            networkComponent.GetComponent<SlimeEmotions>().SetAll(packet.Emotions);
        if (resource != null)
        {
            networkComponent.SetResourceState(packet.ResourceState, packet.ResourceProgress);
        }

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}