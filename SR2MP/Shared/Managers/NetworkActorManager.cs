using System.Collections;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Slime;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;

namespace SR2MP.Shared.Managers;

internal sealed partial class NetworkActorManager
{
    public readonly Dictionary<long, IdentifiableModel> Actors = new();
    public readonly Dictionary<int, IdentifiableType> ActorTypes = new();

    public static int GetPersistentID(IdentifiableType type)
        => GameContext.Instance.AutoSaveDirector._saveReferenceTranslation.GetPersistenceId(type);

    internal void Initialize(GameContext context)
    {
        ActorTypes.Clear();
        Actors.Clear();

        foreach (var type in context.AutoSaveDirector._saveReferenceTranslation._identifiableTypeLookup)
            ActorTypes.TryAdd(GetPersistentID(type.value), type.value);

        ActorTypes[-1] = null!;

        StartCoroutine(ZoneLoadingLoop());
    }

    private IEnumerator ZoneLoadingLoop()
    {
        while (true)
        {
            yield return new WaitForSceneGroupLoad(false);
            yield return new WaitForSceneGroupLoad();

            if (!Main.Server.IsRunning && !Main.Client.IsConnected)
                continue;

            if (!SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
                continue;

            var gameModel = SceneContext.Instance?.GameModel;
            if (!gameModel)
                continue;

            var scene = SystemContext.Instance.SceneLoader.CurrentSceneGroup;

            foreach (var actor in gameModel!.identifiables)
            {
                if (actor.value.ident.IsPlayer)
                    continue;

                if (actor.value.TryCast<ActorModel>() == null)
                    continue;

                var obj = actor.value.GetGameObject();
                if (!obj)
                    continue;
                Object.Destroy(obj);
                Actors.Remove(actor.value.actorId.Value);
            }

            foreach (var actor2 in gameModel.identifiables)
            {
                if (actor2.value.ident.IsPlayer)
                    continue;

                var model = actor2.value.TryCast<ActorModel>();

                if (model == null)
                    continue;

                if (!model.ident.prefab)
                    continue;

                if (actor2.value.sceneGroup != scene)
                    continue;
                HandlingPacket = true;
                var obj = InstantiationHelpers.InstantiateActorFromModel(model);
                HandlingPacket = false;

                if (!obj)
                    continue;

                var networkComponent = obj.AddComponent<NetworkActor>();

                networkComponent.previousPosition = model.lastPosition;
                networkComponent.nextPosition = model.lastPosition;
                networkComponent.previousRotation = model.lastRotation;
                networkComponent.nextRotation = model.lastRotation;

                ActorManager.Actors.Add(model.actorId.Value, model);
            }

            yield return TakeOwnershipOfNearby();
        }
    }

    private static bool ActorIDAlreadyInUse(ActorId id)
        => SceneContext.Instance?.GameModel?.TryGetIdentifiableModel(id, out _) ?? false;

    public static long GetHighestActorIdInRange(long min, long max)
    {
        var result = min;
        foreach (var actor in GameState.identifiables)
        {
            var id = actor.value.actorId.Value;
            if (id < min || id >= max)
                continue;
            if (id > result)
            {
                result = id;
            }
        }

        return result;
    }

    internal IEnumerator TakeOwnershipOfNearby()
    {
        const int max = 12;

        var player = SceneContext.Instance.player;

        var bounds = new Bounds(player.transform.position, new Vector3(325, 1000, 325));

        var i = 0;
        foreach (var actor in Actors)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (actor.Value == null)
                continue;

            if (!bounds.Contains(actor.Value.lastPosition))
                continue;

            if (actor.Value.TryGetNetworkComponent(out var netActor))
                continue;

            if (netActor == null)
                continue;

            netActor.LocallyOwned = true;

            var actorId = netActor.ActorId;
            if (actorId.Value == 0)
            {
                yield break;
            }

            var packet = new ActorTransferPacket { ActorId = actorId, OwnerId = LocalID };
            Main.SendToAllOrServer(packet);
            i++;

            if (i <= max)
                continue;
            yield return null;
            i = 0;
        }
    }

    private static GadgetModel? GetLinkedGadget(GadgetModel model)
        => GameState.identifiables._entries.FirstOrDefault(x =>
                x.value != null &&
                model != null &&
                x.value.ident == model?.ident
                && model != x.value
                && (model.ident.Cast<GadgetDefinition>().BuyInPairs
                    || model.ident.Cast<GadgetDefinition>().LinkedDefinition
                    || model.ident.Cast<GadgetDefinition>().LinkedGadgetRange != 0f))?
            .value.Cast<GadgetModel>()!;

    internal static bool ApplyRadiancy(SlimeModel slime, ActorAppearanceType radiancy = ActorAppearanceType.Default)
    {
        if (slime == null) return false;

        var gameObj = slime.GetGameObject();
        if (!gameObj) return false;

        var applicator = gameObj.GetComponent<SlimeAppearanceApplicator>();
        if (!applicator) return false;

        var def = gameObj.GetComponent<Identifiable>().identType.TryCast<SlimeDefinition>();
        if (!def) return false;
        
        if (radiancy == ActorAppearanceType.Default && slime.IsRadiant)
        {
            if (def!.RadiantBase && def.RadiantBase.AppearType == SlimeAppearance.AppearanceType.RADIANT_BASE)
                radiancy = ActorAppearanceType.BaseRadiant;
            else if (def.RadiantLargo0 &&
                     def.RadiantLargo0.AppearType == SlimeAppearance.AppearanceType.RADIANT_LARGO_0)
                radiancy = ActorAppearanceType.LargoRadiant0;
            else if (def.RadiantLargo1 &&
                     def.RadiantLargo1.AppearType == SlimeAppearance.AppearanceType.RADIANT_LARGO_1)
                radiancy = ActorAppearanceType.LargoRadiant1;
        }

        var newAppearance = radiancy switch
        {
            ActorAppearanceType.BaseRadiant => def!.RadiantBase,
            ActorAppearanceType.LargoRadiant0 => def!.RadiantLargo0,
            ActorAppearanceType.LargoRadiant1 => def!.RadiantLargo1,
            _ => applicator.Appearance
        };

        if (!newAppearance) return false;

        var slimeRadiant = gameObj.GetComponent<SlimeRadiant>();
        if (slimeRadiant)
        {
            slimeRadiant.SetRadiant();
            slimeRadiant.SetRadiantAppearance();
        }

        slime.GetAmmoMetadata().Radiant = true;
        applicator.Appearance = newAppearance;
        applicator.ApplyAppearance();
        applicator.HandleChosenAppearanceChanged(def, newAppearance);

        return true;
    }
}