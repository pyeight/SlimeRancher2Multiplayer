using System.Collections;
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Managers;
using Unity.Mathematics;

namespace SR2MP.Patches.Gadget;

[HarmonyPatch(typeof(GadgetDirector), nameof(GadgetDirector.InstantiateGadget))]
internal static class OnGadgetSpawn
{
    private static IEnumerator SpawnOverNetwork(
        GameObject result,
        SceneGroup sceneGroup,
        Vector3 position,
        Quaternion rotation)
    {
        yield return null;

        if (!result)
            yield break;

        var gadget = result.GetComponent<Il2CppMonomiPark.SlimeRancher.World.Gadget>();
        var type = NetworkActorManager.GetPersistentID(gadget.identType);

        GadgetModel? model = null;
        if (GameState.identifiables.TryGetValue(gadget.GetActorId(), out var identModel))
            model = identModel?.TryCast<GadgetModel>();

        var packet = new ActorSpawnPacket()
        {
            ActorId = gadget.GetActorId(),
            SceneGroup = (byte)NetworkSceneManager.GetPersistentID(sceneGroup),
            ActorType = type,
            Position = position,
            Rotation = rotation,
            Emotions = float4.zero,
            SpawnType = (byte)ActorSpawnType.Gadget,
            ChargeupTime = model?.waitForChargeupTime ?? 0,
            OwnerId = LocalID
        };

        Main.SendToAllOrServer(packet);

        NetworkGadgetManager.BroadcastGadgetLink(model);
    }

    public static void Postfix(
        GameObject __result,
        SceneGroup sceneGroup,
        Vector3 position,
        Quaternion rotation)
    {
        if (!HandlingPacket)
            StartCoroutine(SpawnOverNetwork(__result, sceneGroup, position, rotation));
    }
}