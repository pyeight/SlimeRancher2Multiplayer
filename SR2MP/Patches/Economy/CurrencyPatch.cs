using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.UI.Framework.Data;
using SR2MP.Packets.Economy;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Economy;

[HarmonyPatch(typeof(PlayerState))]
internal static class CurrencyPatch
{
    // Needed for Set/AddCurrency patches,
    // the postfixes already send a packet with UI notification/source info
    private static bool handlingCurrencyCall;

    [HarmonyPrefix, HarmonyPatch(nameof(PlayerState.AddCurrency))]
    public static void AddCurrencyPrefix() => handlingCurrencyCall = true;
    
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
    
    // Lucky Slime coins start
    [HarmonyPrefix, HarmonyPatch(nameof(PlayerState.AddCurrencyDisplayDelta))]
    public static void AddCurrencyDisplayDeltaPrefix() => handlingCurrencyCall = true;

    [HarmonyPostfix, HarmonyPatch(nameof(PlayerState.AddCurrencyDisplayDelta))]
    public static void AddCurrencyDisplayDelta(
        PlayerState __instance,
        ICurrency currencyDefinition,
        bool showUiNotification,
        IUIDisplayData sourceOfChange)
    {
        if (HandlingPacket) return;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (currencyDefinition == null)
            return;

        var packet = new CurrencyPacket
        {
            NewAmount = __instance._model.GetCurrencyAmount(currencyDefinition),
            CurrencyType = (byte)currencyDefinition.PersistenceId,
            ShowUINotification = showUiNotification,
            SourceIdent = GetSourceIdent(sourceOfChange)
        };

        Main.SendToAllOrServer(packet);
    }

    [HarmonyFinalizer, HarmonyPatch(nameof(PlayerState.AddCurrencyDisplayDelta))]
    public static Exception? AddCurrencyDisplayDeltaFinalizer(Exception? __exception)
        => CurrencyFinalizer(__exception);
    
    // Lucky slime coins end
    
    [HarmonyPrefix, HarmonyPatch(nameof(PlayerState.SpendCurrency))]
    public static void SpendCurrencyPrefix() => handlingCurrencyCall = true;
    
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

    [HarmonyFinalizer, HarmonyPatch(nameof(PlayerState.SpendCurrency))]
    public static Exception? SpendCurrencyFinalizer(Exception? __exception)
        => CurrencyFinalizer(__exception);
    
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerState.HandleModelCurrencyChanged))]
    public static void HandleModelCurrencyChanged(ICurrency currency, int oldAmount, int newAmount)
    {
        if (HandlingPacket || handlingCurrencyCall) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        if (oldAmount == newAmount)
            return;
        
        if (SystemContext.Instance.SceneLoader.IsSceneLoadInProgress)
            return;

        Main.SendToAllOrServer(new CurrencyPacket
        {
            NewAmount = newAmount,
            CurrencyType = (byte)currency.PersistenceId,
            ShowUINotification = true,
            SourceIdent = -1
        });
    }
    
    private static int GetSourceIdent(IUIDisplayData? sourceOfChange)
    {
        var identType = sourceOfChange?.TryCast<IdentifiableType>();
        return identType != null ? NetworkActorManager.GetPersistentID(identType) : -1;
    }
    
    [HarmonyFinalizer, HarmonyPatch(nameof(PlayerState.AddCurrency))]
    public static Exception? CurrencyFinalizer(Exception? __exception)
    {
        handlingCurrencyCall = false;
        return __exception;
    }
}