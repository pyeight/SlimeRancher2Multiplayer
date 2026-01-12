using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Loading;
using SR2MP.Shared.Managers;
using SR2MP.Packets.Utils;

namespace SR2MP.Client.Handlers;

[PacketHandler((byte)PacketType.InitialActors)]
public sealed class ActorsLoadHandler : BaseClientPacketHandler
{
    public ActorsLoadHandler(Client client, RemotePlayerManager playerManager)
        : base(client, playerManager) { }

    public override void Handle(byte[] data)
    {
        using var reader = new PacketReader(data);
        var packet = reader.ReadPacket<ActorsPacket>();

        actorManager.Actors.Clear();

        var toRemove = new CppCollections.List<IdentifiableModel>(
            SceneContext.Instance.GameModel.identifiables._values
               .Cast<CppCollections.IEnumerable<IdentifiableModel>>());

        foreach (var actor in toRemove)
        {
            if (actor.ident.IsPlayer) continue;

            var gameObject = actor.GetGameObject();
            if (gameObject)
                Object.Destroy(gameObject);

            SceneContext.Instance.GameModel.DestroyIdentifiableModel(actor);
        }

        SceneContext.Instance.GameModel._actorIdProvider._nextActorId = packet.StartingActorID;

        foreach (var actor in packet.Actors)
        {
            actorManager.TrySpawnNetworkActor(new ActorId(actor.ActorId), actor.Position, actor.Rotation, actor.ActorType, actor.Scene, out _);
        }
    }
}