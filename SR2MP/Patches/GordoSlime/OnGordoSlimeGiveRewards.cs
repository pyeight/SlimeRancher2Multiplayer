using System.Collections;
using HarmonyLib;
using SR2MP.Packets.GordoSlime;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.GordoSlime;

[HarmonyPatch(typeof(GordoRewardsBase), nameof(GordoRewardsBase.GiveRewards))]
internal static class OnGordoSlimeGiveRewards
{
    internal sealed class RewardSnapshot
    {
        public string GordoSlimeId = string.Empty;
        public readonly HashSet<int> ActorIds = new();
        public readonly HashSet<int> BreakableIds = new();
    }

    public static bool Prefix(GordoRewardsBase __instance, out RewardSnapshot? __state)
    {
        __state = null;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return true;

        var eat = __instance._eat;
        if (!eat) return true;

        var id = eat.Id;
        if (NetworkGordoSlimeManager.HasGivenRewards(id))
            return false;

        NetworkGordoSlimeManager.MarkRewarded(id);

        __state = new RewardSnapshot { GordoSlimeId = id };

        foreach (var actor in Resources.FindObjectsOfTypeAll<IdentifiableActor>())
        {
            if (actor)
                __state.ActorIds.Add(actor.GetInstanceID());
        }
        
        foreach (var breakable in Resources.FindObjectsOfTypeAll<BreakOnImpact>())
        {
            if (breakable)
                __state.BreakableIds.Add(breakable.GetInstanceID());
        }

        return true;
    }

    public static void Postfix(GordoRewardsBase __instance, RewardSnapshot? __state)
    {
        if (HandlingPacket) return;
        if (__state == null) return;

        StartCoroutine(SpawnOverNetwork(__instance, __state));
    }

    private static IEnumerator SpawnOverNetwork(GordoRewardsBase gordoSlimeRewards, RewardSnapshot existingBefore)
    {
        yield return new WaitForSeconds(1f);

        foreach (var identifiableActor in Resources.FindObjectsOfTypeAll<IdentifiableActor>())
        {
            if (!identifiableActor || existingBefore.ActorIds.Contains(identifiableActor.GetInstanceID()))
                continue;

            ActorManager.RegisterSpawnOverNetwork(identifiableActor.gameObject);
        }

        foreach (var breakable in Resources.FindObjectsOfTypeAll<BreakOnImpact>())
        {
            if (breakable && !existingBefore.BreakableIds.Contains(breakable.GetInstanceID()))
                StartCoroutine(SpawnRewardOverNetwork(gordoSlimeRewards, existingBefore.GordoSlimeId, breakable));
        }
    }

    private static IEnumerator SpawnRewardOverNetwork(GordoRewardsBase gordoSlimeRewards, string gordoSlimeId, BreakOnImpact breakable)
    {
        var elapsed = 0f;
        while (elapsed < 5f)
        {
            if (!breakable || breakable._breaking) yield break;

            var body = breakable._body;
            if (body && body.IsSleeping())
                break;

            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        if (!breakable || breakable._breaking) yield break;

        var rewardPrefabs = gordoSlimeRewards.TryCast<GordoRewards>()?.RewardPrefabs;
        if (rewardPrefabs == null) yield break;

        var cloneName = breakable.gameObject.name.Replace("(Clone)", string.Empty);
        var prefabIndex = -1;
        for (var i = 0; i < rewardPrefabs.Length; i++)
        {
            if (rewardPrefabs[i] && rewardPrefabs[i].name == cloneName)
            {
                prefabIndex = i;
                break;
            }
        }

        if (prefabIndex < 0)
        {
            SrLogger.LogWarning($"OnGordoSlimeGiveRewards: couldn't match reward '{cloneName}' to a RewardPrefabs entry, not syncing it.");
            yield break;
        }

        Main.SendToAllOrServer(new GordoSlimeRewardPacket
        {
            GordoSlimeId = gordoSlimeId,
            PrefabIndex = prefabIndex,
            Position = breakable.transform.position,
            Rotation = breakable.transform.rotation
        });
    }
}
