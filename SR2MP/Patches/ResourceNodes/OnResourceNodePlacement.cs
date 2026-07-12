using System.Collections;
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World.ResourceNode;
using SR2MP.Packets.World;

namespace SR2MP.Patches.ResourceNodes;

[HarmonyPatch(typeof(ResourceNodeSpawner), nameof(ResourceNodeSpawner.SpawnNode))]
internal static class OnResourceNodeSpawned
{
    public static void Postfix(ResourceNodeSpawner __instance)
    {
        if (!Main.Server.IsRunning || HandlingPacket)
            return;

        StartCoroutine(SendPlacement(__instance));
    }
    
    private static IEnumerator SendPlacement(ResourceNodeSpawner spawner)
    {
        yield return null;

        if (!Main.Server.IsRunning || spawner == null || spawner._model == null)
            yield break;

        Main.Server.SendToAll(new ResourceNodePlacementPacket
        {
            Node = ResourceNodeManager.CreatePlacement(spawner._model)
        });
    }
}

[HarmonyPatch(typeof(ResourceNodeSpawner), nameof(ResourceNodeSpawner.DespawnNode))]
internal static class OnResourceNodeDespawned
{
    public static void Postfix(ResourceNodeSpawner __instance)
    {
        if (!Main.Server.IsRunning || HandlingPacket)
            return;

        var model = __instance._model;
        if (model == null)
            return;

        Main.Server.SendToAll(new ResourceNodePlacementPacket
        {
            Node = new ResourceNodePlacement { ID = model.nodeId, HasNode = false }
        });
    }
}

[HarmonyPatch(typeof(ResourceNodeDirector), nameof(ResourceNodeDirector.SpawnUpToMaxNodes))]
internal static class OnResourceNodeDirectorSpawn
{
    public static bool Prefix() => !Main.Client.IsConnected;
}
