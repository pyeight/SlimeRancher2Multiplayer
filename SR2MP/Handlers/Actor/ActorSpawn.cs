using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorSpawn)]
internal sealed class ActorSpawnHandler : BasePacketHandler<ActorSpawnPacket>
{
    protected override bool Handle(ActorSpawnPacket packet, IPEndPoint? _)
    {
        if (ActorManager.Actors.ContainsKey(packet.ActorId.Value))
        {
            SrLogger.LogPacketSize($"Actor {packet.ActorId.Value} already exists");
            return false;
        }

        ActorManager.TrySpawnNetworkActor(packet.ActorId, packet.Position, packet.Rotation, packet.ActorType, packet.SceneGroup, out var actor);

        if (actor == null)
            return true;

        if (packet.MaterialIndex != (byte)SprinkleMaterialType.none)
        {
            var gameObj = actor.GetGameObject();
            if (gameObj)
                StartCoroutine(NetworkActorManager.ApplySprinkleMaterial(gameObj, (SprinkleMaterialType)packet.MaterialIndex));
        }

        var slime = actor.TryCast<SlimeModel>();
        if (slime == null)
            return true;

        slime.firstAppearanceSaveSet = packet.FirstAppearance;
        slime.secondAppearanceSaveSet = packet.SecondAppearance;
        slime.Emotions = packet.Emotions;
        slime.isSleeping = packet.Sleeping;

        if (packet.Radiancy != (byte)ActorAppearanceType.Default)
            NetworkActorManager.ApplyRadiancy(slime, (ActorAppearanceType)packet.Radiancy);

        return true;
    }
}