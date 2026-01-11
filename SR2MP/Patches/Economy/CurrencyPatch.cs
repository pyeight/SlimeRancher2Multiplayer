using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Economy;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.Economy;

[HarmonyPatch(typeof(PlayerState))]
public static class CurrencyPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerState.AddCurrency))]
    public static void AddCurrency(
        PlayerState __instance,
        ICurrency currencyDefinition,
        int adjust,
        bool showUiNotification)
    {
        if (handlingPacket) return;

        if (currencyDefinition == null)
            return;
        
        var currency = currencyDefinition.PersistenceId;

        var packet = new CurrencyPacket
        {
            Type = (byte)PacketType.CurrencyAdjust,
            NewAmount = __instance._model.GetCurrencyAmount(currencyDefinition),
            CurrencyType = (byte)currency,
            ShowUINotification = showUiNotification,
        };

        Main.SendToAllOrServer(packet);
    }
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerState.SpendCurrency))]
    public static void SpendCurrency(
        PlayerState __instance,
        ICurrency currency,
        int adjust)
    {
        if (handlingPacket) return;

        var currencyId = currency.PersistenceId;

        var packet = new CurrencyPacket
        {
            Type = (byte)PacketType.CurrencyAdjust,
            NewAmount = __instance._model.GetCurrencyAmount(currency),
            CurrencyType = (byte)currencyId,
            ShowUINotification = true,
        };

        Main.SendToAllOrServer(packet);
    }
}