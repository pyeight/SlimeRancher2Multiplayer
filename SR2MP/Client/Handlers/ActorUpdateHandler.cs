using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Slime;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ActorUpdate)]
public sealed class ActorUpdateHandler : BaseClientPacketHandler
{
    public ActorUpdateHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorUpdatePacket>();

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
    }
}