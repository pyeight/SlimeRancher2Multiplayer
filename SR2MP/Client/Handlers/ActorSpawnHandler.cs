using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.ActorSpawn)]
public class ActorSpawnHandler : BaseClientPacketHandler
{
    public ActorSpawnHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorSpawnPacket>();
        
        var model = SceneContext.Instance.GameModel.CreateActorModel(
            packet.ActorId,
            actorManager.ActorTypes[packet.ActorType],
            SystemContext.Instance.SceneLoader.DefaultGameplaySceneGroup,
            packet.Position,
            packet.Rotation)
            .TryCast<ActorModel>();

        if (model != null)
        {
            handlingPacket = true;
            var actor = InstantiationHelpers.InstantiateActorFromModel(model);
            handlingPacket = false;
            if (actor)
            {
                var networkComponent = actor.AddComponent<NetworkActor>();
                networkComponent.previousPosition = packet.Position;
                networkComponent.nextPosition = packet.Position;
                networkComponent.previousRotation = packet.Rotation;
                networkComponent.nextRotation = packet.Rotation;
                actor.transform.position = packet.Position;
                actorManager.Actors.Add(packet.ActorId.Value, model);
            }
        }
    }
}