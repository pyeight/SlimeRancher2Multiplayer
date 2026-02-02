using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ActorDestroy)]
public sealed class ActorDestroyHandler : BaseClientPacketHandler<ActorDestroyPacket>
{
    public ActorDestroyHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(ActorDestroyPacket packet)
    {
        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
        {
            SrLogger.LogPacketSize($"Actor {packet.ActorId.Value} doesn't exist (already destroyed?)", SrLogTarget.Both);
            return;
        }

        SceneContext.Instance.GameModel.identifiables.Remove(packet.ActorId);
        SceneContext.Instance.GameModel.identifiablesByIdent[actor.ident].Remove(actor);
        SceneContext.Instance.GameModel.DestroyIdentifiableModel(actor);

        var obj = actor.GetGameObject();
        handlingPacket = true;
        if (obj)
            Destroyer.DestroyActor(actor.GetGameObject(), "SR2MP.ActorDestroyHandler");
        handlingPacket = false;
    }
}