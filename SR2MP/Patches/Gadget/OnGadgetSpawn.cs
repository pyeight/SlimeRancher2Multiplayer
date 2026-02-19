using System.Collections;
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using MelonLoader;
using SR2MP.Packets.Actor;
using SR2MP.Shared.Managers;
using Unity.Mathematics;

namespace SR2MP.Patches.Gadget;

[HarmonyPatch(typeof(GadgetDirector), nameof(GadgetDirector.InstantiateGadget))]
public static class OnGadgetSpawn
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
        
        var packet = new ActorSpawnPacket()
        {
            ActorId = gadget.GetActorId(),
            SceneGroup = (byte)NetworkSceneManager.GetPersistentID(sceneGroup),
            ActorType = type,
            Position = position,
            Rotation = rotation,
            Emotions = float4.zero
        };
        
        Main.SendToAllOrServer(packet);
    }
    
    public static void Postfix(
        GameObject __result,
        GameObject original,
        SceneGroup sceneGroup,
        Vector3 position,
        Quaternion rotation)
    {
        if (handlingPacket)
            return;
        
        MelonCoroutines.Start(SpawnOverNetwork(__result, sceneGroup, position, rotation));
    }
}