using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Components.Actor;
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

        var toRemove = new CppCollections.Dictionary<ActorId, IdentifiableModel>(
            SceneContext.Instance.GameModel.identifiables
                .Cast<CppCollections.IDictionary<ActorId, IdentifiableModel>>());

        foreach (var actor in toRemove)
        {
            if (actor.value.ident.IsPlayer) continue;

            var gameObject = actor.value.GetGameObject();
            if (gameObject)
                Object.Destroy(gameObject);

            SceneContext.Instance.GameModel.DestroyIdentifiableModel(actor.value);
        }

        foreach (var actor in packet.Actors)
        {
            var type = actorManager.ActorTypes[actor.ActorType];
            if (type.IsPlayer) continue;

            var model = SceneContext.Instance.GameModel.CreateActorModel(
                    new ActorId(actor.ActorId),
                    type,
                    SystemContext.Instance.SceneLoader.DefaultGameplaySceneGroup,
                    actor.Position,
                    actor.Rotation)
                .TryCast<ActorModel>();

            if (model == null)
                continue;

            handlingPacket = true;
            try
            {
                var actorObject = InstantiationHelpers.InstantiateActorFromModel(model);

                if (!actorObject)
                    return;

                var networkComponent = actorObject.AddComponent<NetworkActor>();
                networkComponent.previousPosition = actor.Position;
                networkComponent.nextPosition = actor.Position;
                networkComponent.previousRotation = actor.Rotation;
                networkComponent.nextRotation = actor.Rotation;
                actorObject.transform.position = actor.Position;
                actorManager.Actors.Add(actor.ActorId, model);
            }
            catch (Exception ex)
            {
                SrLogger.LogError(
                    $"Error while loading actor with ID {actor.ActorId}\nActor Information: Type={type.name}\nError: {ex}",
                    SrLogger.LogTarget.Both);
            }

            handlingPacket = false;
        }
    }
}