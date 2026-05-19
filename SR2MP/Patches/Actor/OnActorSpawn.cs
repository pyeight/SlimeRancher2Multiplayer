using System.Collections;
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Player;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using Il2CppMonomiPark.SlimeRancher.VFX;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Managers;
using Unity.Mathematics;

namespace SR2MP.Patches.Actor;

[HarmonyPatch(typeof(InstantiationHelpers), nameof(InstantiationHelpers.InstantiateActor))]
internal static class OnActorSpawn
{
    private static IEnumerator SpawnOverNetwork(
        int actorType,
        byte sceneGroup,
        GameObject actor,
        SlimeAppearance.AppearanceSaveSet appearance,
        SlimeAppearance.AppearanceSaveSet secondAppearance)
    {
        yield return null;

        if (!actor)
            yield break;

        var id = actor.GetComponent<IdentifiableActor>().GetActorId();

        var emotions = float4.zero;
        var sleeping = false;
        var slimeModel = actor.GetComponent<IdentifiableActor>()._model.TryCast<SlimeModel>();
        if (slimeModel != null)
        {
            emotions = slimeModel.Emotions;
            sleeping = slimeModel.isSleeping;
        }

        var radiancy = ActorAppearanceType.Default;
        var slimeAppearanceApplicator = actor.GetComponent<SlimeAppearanceApplicator>();
        if (slimeAppearanceApplicator != null)
        {
            var currentAppearance = slimeAppearanceApplicator.Appearance;
            var def = actor.GetComponent<Identifiable>().identType.TryCast<SlimeDefinition>();
            if (def != null && currentAppearance != null)
            {
                if (currentAppearance == def.RadiantBase)
                    radiancy = ActorAppearanceType.BaseRadiant;
                else if (currentAppearance == def.RadiantLargo0)
                    radiancy = ActorAppearanceType.LargoRadiant0;
                else if (currentAppearance == def.RadiantLargo1)
                    radiancy = ActorAppearanceType.LargoRadiant1;
            }
        }

        var material = SprinkleMaterialType.none;
        var sprinkle = actor.GetComponent<RandomMaterial>();

        if (sprinkle != null)
        {
            var materialName = sprinkle._renderers
                .Select(r => r.sharedMaterial?.name.Replace(" (Instance)", ""))
                .FirstOrDefault();

            if (Enum.TryParse(materialName, out SprinkleMaterialType type))
                material = type;
        }

        var packet = new ActorSpawnPacket
        {
            ActorType = actorType,
            SceneGroup = sceneGroup,
            ActorId = id,
            Position = actor.transform.position,
            Rotation = actor.transform.rotation,
            Emotions = emotions,
            Sleeping = sleeping,
            FirstAppearance = appearance,
            SecondAppearance = secondAppearance,
            Radiancy = (byte)radiancy,
            MaterialIndex = (byte)material
        };

        Main.SendToAllOrServer(packet);
    }

    public static void Postfix(
        GameObject __result,
        GameObject original,
        SceneGroup sceneGroup,
        Vector3 position,
        Quaternion rotation,
        bool nonActorOk = false,
        SlimeAppearance.AppearanceSaveSet appearance = SlimeAppearance.AppearanceSaveSet.NONE,
        SlimeAppearance.AppearanceSaveSet secondAppearance = SlimeAppearance.AppearanceSaveSet.NONE,
        Il2CppSystem.Nullable<AmmoSlot.AmmoMetadata> metadata = null!,
        bool ignoreEmotions = false,
        bool setCollected = false)
    {
        if (HandlingPacket) return;

        var networkActor = __result.AddComponent<NetworkActor>();
        networkActor.LocallyOwned = true;

        var actorType = NetworkActorManager.GetPersistentID(original.GetComponent<Identifiable>().identType);
        var sceneGroupId = NetworkSceneManager.GetPersistentID(sceneGroup);

        ActorManager.Actors[__result.GetComponent<IdentifiableActor>()._model.actorId.Value] =
            __result.GetComponent<IdentifiableActor>()._model;

        StartCoroutine(SpawnOverNetwork(actorType, (byte)sceneGroupId, __result, appearance, secondAppearance));
    }
}