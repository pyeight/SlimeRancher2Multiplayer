using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Server.Managers;
using SR2MP.Shared.Managers;

namespace SR2MP.Server.Handlers;

[PacketHandler((byte)PacketType.ActorSpawn)]
public sealed class ActorSpawnHandler : BasePacketHandler
{
    public ActorSpawnHandler(NetworkManager networkManager, ClientManager clientManager)
        : base(networkManager, clientManager) { }

    public override void Handle(byte[] data, IPEndPoint clientEp)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorSpawnPacket>();
       
        actorManager.TrySpawnNetworkActor(packet.ActorId, packet.Position, packet.Rotation, packet.ActorType, packet.SceneGroup, out _);

        Main.Server.SendToAllExcept(packet, clientEp);
    }
}