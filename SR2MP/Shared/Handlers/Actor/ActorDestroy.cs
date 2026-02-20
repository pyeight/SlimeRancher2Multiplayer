using System.Net;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Handlers.Internal;

namespace SR2MP.Shared.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorDestroy)]
public sealed class ActorDestroyHandler : BasePacketHandler<ActorDestroyPacket>
{
    protected override bool Handle(ActorDestroyPacket packet, IPEndPoint? _)
    {
        //if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
        //{
        //    SrLogger.LogDebug($"Actor {packet.ActorId.Value} doesn't exist (already destroyed?)", SrLogTarget.Both);
        //    return false;
        //}

        if (!SceneContext.Instance.GameModel.TryGetIdentifiableModel(packet.ActorId, out var actor))
        {
            // SrLogger.LogError($"Tried to destroy actor that doesn't exist!\n\tID: {packet.ActorId}", SrLogTarget.Both);
            return false;
        }
        
        SceneContext.Instance.GameModel.identifiables.Remove(packet.ActorId);
        SceneContext.Instance.GameModel.identifiablesByIdent[actor.ident].Remove(actor);
        SceneContext.Instance.GameModel.DestroyIdentifiableModel(actor);

        var obj = actor.GetGameObject();
        handlingPacket = true;

        if (obj)
            Destroyer.DestroyAny(actor.GetGameObject(), "SR2MP.ActorDestroyHandler");

        handlingPacket = false;
        return true;
    }
}