using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Event;

namespace SR2MP.Shared.Managers;

internal static class NetworkGordoSlimeManager
{
    private static readonly HashSet<string> PoppedGordoSlimeIds = new();
    private static readonly HashSet<string> RewardsGivenIds = new();

    private static StringEventProducer? burstEventProducer;

    internal static void MarkPopped(string id) => PoppedGordoSlimeIds.Add(id);

    internal static bool HasGivenRewards(string id) => RewardsGivenIds.Contains(id);

    internal static void MarkRewarded(string id) => RewardsGivenIds.Add(id);

    internal static void ApplyState(string id, GordoModel model)
    {
        if (!model.gameObj) return;

        var gordoSlimeComponent = model.gameObj.GetComponent<GordoEat>();
        if (!gordoSlimeComponent) return;

        gordoSlimeComponent.SetModel(model);

        if (!PoppedGordoSlimeIds.Contains(id)) return;

        RaiseBurstEvent(id);
        model.gameObj.SetActive(false);
    }
    
    internal static void ReapplyOnSpawn(GordoEat gordoSlime)
    {
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        burstEventProducer ??= gordoSlime._onBurstEvent;

        if (!PoppedGordoSlimeIds.Contains(gordoSlime.Id)) return;
        
        RaiseBurstEvent(gordoSlime.Id);
        gordoSlime.gameObject.SetActive(false);
    }
    
    internal static void RaiseBurstEvent(string id)
        => NetworkEventManager.RaiseLocallyOnce(burstEventProducer?.KeyPrefix ?? GordoBurstEventKey, id);
    
    internal static void MarkPoppedInModel(GordoModel model)
    {
        if (model == null || model.HasPopped()) return;

        model.GordoEatenCount = GordoEat.ALREADY_BURST_FLAG;
    }
    
    internal static string? ResolveGordoSlimeId(GordoModel model)
    {
        if (model.gameObj)
        {
            var gordoSlime = model.gameObj.GetComponent<GordoEat>();
            if (gordoSlime != null && !string.IsNullOrEmpty(gordoSlime.Id))
                return gordoSlime.Id;
        }

        foreach (var pair in GameState.gordos)
        {
            if (pair.value.Equals(model))
                return pair.key;
        }

        return null;
    }
}
