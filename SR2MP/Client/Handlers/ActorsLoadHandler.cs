using Il2CppAssets.Script.Util.Extensions;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
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

        var gameModel = SceneContext.Instance.GameModel;

        var toRemove = new CppCollections.List<IdentifiableModel>(
            gameModel.identifiables
               .Cast<CppCollections.IEnumerable<IdentifiableModel>>());

        foreach (var actor in toRemove)
        {
            if (actor.ident.IsPlayer) continue;

            var gameObject = actor.GetGameObject();
            if (gameObject)
                Object.Destroy(gameObject);
            gameModel.DestroyIdentifiableModel(actor);
        }

        gameModel._actorIdProvider._nextActorId = packet.StartingActorID;
        
        foreach (var actor in packet.Actors)
        {
            actorManager.TrySpawnNetworkActor(new ActorId(actor.ActorId), actor.Position, actor.Rotation, actor.ActorType, actor.Scene, out _);
        }
    }
}