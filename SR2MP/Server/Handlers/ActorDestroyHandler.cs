using System.Net;
using Il2Cpp;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorDestroy)]
public class ActorDestroyHandler : BasePacketHandler
{
    public ActorDestroyHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
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
        
        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}