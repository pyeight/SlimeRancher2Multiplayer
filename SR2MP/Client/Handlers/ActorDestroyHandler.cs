using Il2Cpp;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ActorDestroy)]
public class ActorDestroyHandler : BaseClientPacketHandler
{
    public ActorDestroyHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorDestroyPacket>();

        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
            return;
        
        actorManager.Actors.Remove(packet.ActorId.Value);
        
        SceneContext.Instance.GameModel.DestroyIdentifiableModel(actor);

        handlingPacket = true;
        Destroyer.DestroyActor(actor.GetGameObject(), "SR2MP.ActorDestroyHandler");
        handlingPacket = false;
    }
}