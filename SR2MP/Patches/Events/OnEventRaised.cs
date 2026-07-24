using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Event;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Events;

[HarmonyPatch(typeof(EventDirector), nameof(EventDirector.RaiseEvent))]
internal static class OnEventRaised
{
    // Todo: Causes lags under **VERY SPECIFIC CIRCUMSTANCES**
    public static void Postfix(IGameEvent gameEvent, int count)
    {
        if (gameEvent == null) return;

        // This is intentionally left behind
        // SrLogger.LogDebug($"GameEvent raised: EventKey='{gameEvent.EventKey}' DataKey='{gameEvent.DataKey}' Count={count}");

        if (HandlingPacket) return;
        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        var eventKey = gameEvent.EventKey;
        if (string.IsNullOrEmpty(eventKey)) return;

        var isStringEvent = Resources.FindObjectsOfTypeAll<StringEventProducer>()
            .Any(producer => producer.KeyPrefix == eventKey);
        if (!isStringEvent) return;

        if (!NetworkEventManager.ShouldSync(eventKey, gameEvent.DataKey)) return;

        NetworkEventManager.SendEventRaised(eventKey, gameEvent.DataKey);
    }
}