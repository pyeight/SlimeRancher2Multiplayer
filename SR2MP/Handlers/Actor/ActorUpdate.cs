using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Slime;
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

        SlimeModel? slime = null;
        ProduceModel? resource = null;
        // PlortModel? plort = null;

        var actorId = packet.ActorId;
        if (actorId.Value != 0 && GameState.identifiables.TryGetValue(actorId, out var identModel))
        {
            if (identModel == null)
                SrLogger.LogWarning("IdentifiableModel is null in update handler!");

            slime = identModel!.TryCast<SlimeModel>();
            resource = identModel.TryCast<ProduceModel>();
            // plort = identModel.TryCast<PlortModel>();
        }

        if (!actor.TryGetNetworkComponent(out var networkComponent))
            return false;

        networkComponent.OnNetworkUpdate(packet);

        if (networkComponent.RegionMember?._hibernating == true)
        {
            networkComponent.transform.position = packet.Position;
            networkComponent.transform.rotation = packet.Rotation;
        }

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (packet.UpdateType)
        {
            case ActorUpdateType.Slime when slime != null:
            {
                slime.isSleeping = packet.Sleeping;

                var slimeEmotions = networkComponent.GetComponent<SlimeEmotions>();
                if (slimeEmotions)
                    slimeEmotions.SetAll(packet.Emotions);
                break;
            }

            case ActorUpdateType.Resource when resource != null:
            {
                resource.state = packet.ResourceState;
                resource.progressTime = packet.ResourceProgress;

                networkComponent.SetResourceState(packet.ResourceState, packet.ResourceProgress, true);
                break;
            }
        }

        return true;
    }
}