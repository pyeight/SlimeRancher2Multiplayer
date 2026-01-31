using LiteNetLib;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorDestroy)]
public sealed class ActorDestroyHandler : BasePacketHandler<ActorDestroyPacket>
{
    public ActorDestroyHandler(ClientManager clientManager)
        : base(clientManager) { }

    public override void Handle(ActorDestroyPacket packet, NetPeer clientPeer)
    {
        if (!actorManager.Actors.Remove(packet.ActorId.Value, out var actor))
            return;

        SceneContext.Instance.GameModel.identifiables.Remove(packet.ActorId);
        SceneContext.Instance.GameModel.identifiablesByIdent[actor.ident].Remove(actor);
        SceneContext.Instance.GameModel.DestroyIdentifiableModel(actor);

        var obj = actor.GetGameObject();
        handlingPacket = true;
        if (obj)
            Destroyer.DestroyActor(actor.GetGameObject(), "SR2MP.ActorDestroyHandler");
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, clientPeer);
    }
}