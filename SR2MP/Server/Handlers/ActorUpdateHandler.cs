using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Server.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorUpdate)]
public class ActorUpdateHandler : BasePacketHandler
{
    public ActorUpdateHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint senderEndPoint)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorUpdatePacket>();

        if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var model))
        {
            return;
        }
        var actor = model.Cast<ActorModel>();
        
        actor.lastPosition = packet.Position;
        actor.lastRotation = packet.Rotation;
        
        if (actor.TryGetNetworkComponent(out var networkComponent))
        {
            networkComponent.SavedVelocity = packet.Velocity;
            networkComponent.nextPosition = packet.Position;
            networkComponent.nextRotation = packet.Rotation;
        }
        
        Main.Server.SendToAllExcept(packet, senderEndPoint);
    }
}