using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorSpawn)]
public sealed class ActorSpawnHandler : BasePacketHandler<ActorSpawnPacket>
{
    public ActorSpawnHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    protected override void Handle(ActorSpawnPacket packet, IPEndPoint clientEp)
    {
        if (actorManager.Actors.ContainsKey(packet.ActorId.Value))
        {
            SrLogger.LogPacketSize($"Actor {packet.ActorId.Value} already exists", SrLogTarget.Both);
            return;
        }

        actorManager.TrySpawnNetworkActor(packet.ActorId, packet.Position, packet.Rotation, packet.ActorType, packet.SceneGroup, out var actor);
        var slime = actor!.TryCast<SlimeModel>();
        if (slime != null)
            slime.Emotions = packet.Emotions;
        
        Main.Server.SendToAllExcept(packet, clientEp);
    }
}