using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.UI.Framework.Data;
using SR2MP.Packets.Economy;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Economy;

[HarmonyPatch(typeof(PlayerState))]
internal static class CurrencyPatch
{
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerState.AddCurrency))]
    public static void AddCurrency(
        PlayerState __instance,
        ICurrency currencyDefinition,
        bool showUiNotification,
        IUIDisplayData sourceOfChange)
    {
        if (HandlingPacket) return;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (currencyDefinition == null)
            return;

        var currency = currencyDefinition.PersistenceId;

        var packet = new CurrencyPacket
        {
            NewAmount = __instance._model.GetCurrencyAmount(currencyDefinition),
            CurrencyType = (byte)currency,
            ShowUINotification = showUiNotification,
            SourceIdent = GetSourceIdent(sourceOfChange)
        };

        Main.SendToAllOrServer(packet);
    }

    [HarmonyPostfix, HarmonyPatch(nameof(PlayerState.SpendCurrency))]
    public static void SpendCurrency(
        PlayerState __instance,
        ICurrency currency,
        IUIDisplayData sourceOfChange)
    {
        if (HandlingPacket) return;

        var currencyId = currency.PersistenceId;

        var packet = new CurrencyPacket
        {
            NewAmount = __instance._model.GetCurrencyAmount(currency),
            CurrencyType = (byte)currencyId,
            ShowUINotification = true,
            SourceIdent = GetSourceIdent(sourceOfChange)
        };

        Main.SendToAllOrServer(packet);
    }
    
    private static int GetSourceIdent(IUIDisplayData? sourceOfChange)
    {
        var identType = sourceOfChange?.TryCast<IdentifiableType>();
        return identType != null ? NetworkActorManager.GetPersistentID(identType) : -1;
    }
}