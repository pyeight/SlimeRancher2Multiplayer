using System.Net;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Server.Handlers.Internal;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorDestroy)]
public sealed class ActorDestroyHandler : BasePacketHandler<ActorDestroyPacket>
{
    public ActorDestroyHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(ActorDestroyPacket packet, IPEndPoint clientEp)
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

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}