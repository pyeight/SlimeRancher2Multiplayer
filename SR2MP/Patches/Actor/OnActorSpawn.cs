using System.Collections;
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using MelonLoader;
using SR2MP.Components.Actor;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Managers;

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

        var packet = new ActorSpawnPacket
        {
            ActorType = actorType,
            SceneGroup = sceneGroup,
            ActorId = id,
            Position = actor.transform.position,
            Rotation = actor.transform.rotation,
            FirstAppearance = appearance,
            SecondAppearance = secondAppearance,
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

        MelonCoroutines.Start(SpawnOverNetwork(actorType, (byte)sceneGroupId, __result, appearance, secondAppearance));
    }
}