using System.Collections;
using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.World;
using SR2MP.Packets.Economy;

namespace SR2MP.Patches.Economy;

[HarmonyPatch(typeof(NewbuckRewardSpawn), nameof(NewbuckRewardSpawn.SpawnRewardFrom))]
internal static class OnNewbuckRewardSpawned
{
    public static void Postfix()
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        StartCoroutine(SendReward());
    }

    private static IEnumerator SendReward()
    {
        yield return new WaitFrames(5);

        if (!Main.Server.IsRunning && !Main.Client.IsConnected)
            yield break;

        var currency = GameContext.Instance.LookupDirector._currencyList[0].Cast<ICurrency>();

        Main.SendToAllOrServer(new CurrencyPacket
        {
            NewAmount = SceneContext.Instance.PlayerState.GetCurrency(currency),
            CurrencyType = (byte)currency.PersistenceId,
            ShowUINotification = true,
            SourceIdent = -1
        });
    }
}
