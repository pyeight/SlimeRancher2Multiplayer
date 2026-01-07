using System.Net;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Shared.Handlers;

[PacketHandler((byte)PacketType.ActorDestroy)]
public sealed class ActorDestroyHandler : BaseSharedPacketHandler
{
    public ActorDestroyHandler(NetworkManager networkManager, ClientManager clientManager) {}
    public ActorDestroyHandler(Client.Client client, RemotePlayerManager playerManager) {}
    
    public override void Handle(byte[] data, IPEndPoint? clientEp = null)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorDestroyPacket>();

        if (!actorManager.Actors.Remove(packet.ActorId.Value, out var actor))
            return;

        SceneContext.Instance.GameModel.DestroyIdentifiableModel(actor);

        handlingPacket = true;
        Destroyer.DestroyActor(actor.GetGameObject(), "SR2MP.ActorDestroyHandler");
        handlingPacket = false;

        if (clientEp != null)
            Main.Server.SendToAllExcept(packet, clientEp);
    }
}