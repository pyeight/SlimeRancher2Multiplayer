using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ActorDestroy)]
public sealed class ActorDestroyHandler : BaseClientPacketHandler
{
    public ActorDestroyHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void HandleClient(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorDestroyPacket>();

        if (!actorManager.Actors.Remove(packet.ActorId.Value, out var actor))
            return;

        SceneContext.Instance.GameModel.DestroyIdentifiableModel(actor);

        handlingPacket = true;
        Destroyer.DestroyActor(actor.GetGameObject(), "SR2MP.ActorDestroyHandler");
        handlingPacket = false;
    }
}