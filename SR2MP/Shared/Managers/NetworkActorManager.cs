using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using MelonLoader;
using SR2MP.Components.Actor;

namespace SR2MP.Shared.Managers;

public sealed class NetworkActorManager
{
    public readonly Dictionary<long, IdentifiableModel> Actors = new Dictionary<long, IdentifiableModel>();

    public readonly Dictionary<int, IdentifiableType> ActorTypes = new Dictionary<int, IdentifiableType>();

    public static int GetPersistentID(IdentifiableType type)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(type);

    internal void Initialize(GameContext context)
    {
        ActorTypes.Clear();
        Actors.Clear();

        foreach (var type in context.AutoSaveDirector._saveReferenceTranslation._identifiableTypeLookup)
        {
            ActorTypes.TryAdd(GetPersistentID(type.value), type.value);
        }

        MelonCoroutines.Start(ZoneLoadingLoop());
    }

    internal IEnumerator ZoneLoadingLoop()
    {
        while (true)
        {
            yield return new WaitForSceneGroupLoad(false);
            yield return new WaitForSceneGroupLoad(true);
            
            if (!Main.Server.IsRunning() && !Main.Client.IsConnected)
                continue;

            if (!SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
                continue;

            var gameModel = SceneContext.Instance?.GameModel;
            if (!gameModel) 
                continue;
            
            SrLogger.LogMessage("Begining to load zone actors.");

            var scene = SystemContext.Instance.SceneLoader.CurrentSceneGroup;
            
            foreach (var actor in gameModel!.identifiables)
            {
                if (actor.value.ident.IsPlayer)
                    continue;
                
                if (actor.value.TryCast<ActorModel>() == null)
                    continue;
                
                var obj = actor.value.GetGameObject();
                if (obj)
                {
                    Object.Destroy(obj);
                    Actors.Remove(actor.value.actorId.Value);
                }
            }
            
            foreach (var actor2 in gameModel!.identifiables)
            {
                if (actor2.value.ident.IsPlayer)
                    continue;
                
                var model = actor2.value.TryCast<ActorModel>();

                if (model == null)
                    continue;
                
                if (!model.ident.prefab)
                    continue;

                if (actor2.value.sceneGroup == scene)
                {
                    handlingPacket = true;
                    var obj = InstantiationHelpers.InstantiateActorFromModel(model);
                    handlingPacket = false;
                    
                    if (!obj)
                        continue;

                    var networkComponent = obj.AddComponent<NetworkActor>();

                    networkComponent.previousPosition = model.lastPosition;
                    networkComponent.nextPosition = model.lastPosition;
                    networkComponent.previousRotation = model.lastRotation;
                    networkComponent.nextRotation = model.lastRotation;

                    actorManager.Actors.Add(model.actorId.Value, model);
                    
                    SrLogger.LogMessage($"Reloaded actor {model.actorId.Value} - {model.ident.name}");
                }
            }
            
        }
    }
}