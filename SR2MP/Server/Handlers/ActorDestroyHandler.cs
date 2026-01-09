using System.Net;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorDestroy)]
public sealed class ActorDestroyHandler : BasePacketHandler
{
    public ActorDestroyHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint clientEp)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorDestroyPacket>();

        if (!actorManager.Actors.Remove(packet.ActorId.Value, out var actor))
            return;

        SceneContext.Instance.GameModel.DestroyIdentifiableModel(actor);

        handlingPacket = true;
        Destroyer.DestroyActor(actor.GetGameObject(), "SR2MP.ActorDestroyHandler");
        handlingPacket = false;

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}