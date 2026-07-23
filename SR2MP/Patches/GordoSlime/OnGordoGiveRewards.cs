using System.Collections;
using HarmonyLib;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.GordoSlime;

[HarmonyPatch(typeof(GordoRewardsBase), nameof(GordoRewardsBase.GiveRewards))]
internal static class OnGordoGiveRewards
{
    public static bool Prefix(GordoRewardsBase __instance, out HashSet<int>? __state)
    {
        __state = null;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return true;

        var eat = __instance._eat;
        if (!eat) return true;

        var id = eat.Id;
        if (NetworkGordoSlimeManager.HasGivenRewards(id))
            return false;

        NetworkGordoSlimeManager.MarkRewarded(id);
        
        __state = new HashSet<int>();
        foreach (var actor in Resources.FindObjectsOfTypeAll<IdentifiableActor>())
        {
            if (actor)
                __state.Add(actor.GetInstanceID());
        }

        return true;
    }

    public static void Postfix(HashSet<int>? __state)
    {
        if (HandlingPacket) return;
        if (__state == null) return;

        StartCoroutine(SpawnOverNetwork(__state));
    }

    private static IEnumerator SpawnOverNetwork(HashSet<int> existingBefore)
    {
        yield return new WaitForSeconds(1f);

        foreach (var identifiableActor in Resources.FindObjectsOfTypeAll<IdentifiableActor>())
        {
            if (!identifiableActor || existingBefore.Contains(identifiableActor.GetInstanceID()))
                continue;

            ActorManager.RegisterSpawnOverNetwork(identifiableActor.gameObject);
        }
    }
}
