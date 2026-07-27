using HarmonyLib;
using SR2MP.Components.LandPlots;
using SR2MP.Shared.Managers;

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
            Main.Server.OnServerStarted += NetworkGardenManager.OnServerStarted;
            Main.Client.OnDisconnected += NetworkGardenManager.OnDisconnected;
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

    [HarmonyPatch(typeof(SpawnResource), nameof(SpawnResource.Update))]
    internal static class SpawnResourceUpdatePatch
    {
        private static readonly Dictionary<int, float> NextCheckTime = new();

        public static void Postfix(SpawnResource __instance)
        {
            var now = UnityEngine.Time.time;
            var id = __instance.GetInstanceID();

            if (!NextCheckTime.TryGetValue(id, out var nextTime))
            {
                NextCheckTime[id] = now + UnityEngine.Random.Range(4f, 14f);
                return;
            }

            if (now < nextTime)
                return;

            NextCheckTime[id] = now + UnityEngine.Random.Range(4f, 14f);
            ResetGrowTime(__instance);
        }
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

        var definition = spawnResource._resourceGrowerDefinition;
        if (definition == null)
            return;

        var timeDirector = SceneContext.Instance?.TimeDirector;
        if (timeDirector == null)
            return;

        var interval = definition._minSpawnIntervalGameHours;
        model.nextSpawnTime = timeDirector.HoursFromNow(interval);

        SrLogger.LogGarden($"Reset grow time on spawner '{spawnResource._id}', next spawn in {interval} hours.");
    }
}