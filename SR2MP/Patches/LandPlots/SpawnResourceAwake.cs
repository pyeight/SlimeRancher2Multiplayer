using HarmonyLib;
using SR2MP.Components.LandPlots;

namespace SR2MP.Patches.LandPlots;

[HarmonyPatch(typeof(SpawnResource), nameof(SpawnResource.Awake))]
internal static class SpawnResourceAwakePatch
{
    private static bool subscribedToServerStart;

    public static void Postfix(SpawnResource __instance)
    {
        ResetGrowTime(__instance);

        if (!subscribedToServerStart)
        {
            Main.Server.OnServerStarted += NetworkGarden.OnServerStarted;
            subscribedToServerStart = true;
        }

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        if (__instance.gameObject.GetComponent<NetworkGarden>() == null)
            __instance.gameObject.AddComponent<NetworkGarden>();
    }
    
    [HarmonyPatch(typeof(SpawnResource), nameof(SpawnResource.SetModel))]
    internal static class SpawnResourceSetModelPatch
    {
        public static void Postfix(SpawnResource __instance)
            => ResetGrowTime(__instance);
    }
    
    private static void ResetGrowTime(SpawnResource spawnResource)
    {
        //if (Main.Server.IsRunning || Main.Client.IsConnected) return;
        
        spawnResource.TryGetComponent<NetworkGarden>(out var networkGarden);

        if (networkGarden == null) return;

        if (!networkGarden.LocallyOwned) return;

        var model = spawnResource._model;
        if (model == null || model.nextSpawnTime < double.MaxValue)
            return;

        var timeDirector = SceneContext.Instance?.TimeDirector;
        if (timeDirector == null)
            return;

        var interval = spawnResource._resourceGrowerDefinition?.MinSpawnIntervalGameHours ?? 12f;
        model.nextSpawnTime = timeDirector.HoursFromNow(interval);

        SrLogger.LogDebug($"Reset grow time on spawner '{spawnResource._id}', next spawn in {interval} hours.");
    }
}