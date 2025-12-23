using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.UI.Framework.Data;
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
        bool showUiNotification = true,
        IUIDisplayData sourceOfChange = null!)
    {
        if (handlingPacket) return;
        
        var currency = currencyDefinition.PersistenceId;

        var packet = new CurrencyPacket
        {
            Type = (byte)PacketType.CurrencyAdjust,
            Adjust = adjust,
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
        int adjust,
        IUIDisplayData sourceOfChange = null!)
    {
        if (handlingPacket) return;

        var currencyId = currency.PersistenceId;

        var packet = new CurrencyPacket
        {
            Type = (byte)PacketType.CurrencyAdjust,
            Adjust = -adjust,
            CurrencyType = (byte)currencyId,
            ShowUINotification = true,
        };
        
        Main.SendToAllOrServer(packet);
    }
}