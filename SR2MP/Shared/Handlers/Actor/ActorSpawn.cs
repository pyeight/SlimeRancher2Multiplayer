using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;
using SR2MP.Shared.Utils;

namespace SR2MP.Shared.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorSpawn)]
public sealed class ActorSpawnHandler : BasePacketHandler<ActorSpawnPacket>
{
    protected override bool Handle(ActorSpawnPacket packet, IPEndPoint? _)
    {
        if (actorManager.Actors.ContainsKey(packet.ActorId.Value))
        {
            SrLogger.LogPacketSize($"Actor {packet.ActorId.Value} already exists", SrLogTarget.Both);
            return false;
        }

        actorManager.TrySpawnNetworkActor(packet.ActorId, packet.Position, packet.Rotation, packet.ActorType, packet.SceneGroup, out var actor);

        if (actor!.TryCast<SlimeModel>(out var slime))
            slime.Emotions = packet.Emotions;

        return true;
    }
}