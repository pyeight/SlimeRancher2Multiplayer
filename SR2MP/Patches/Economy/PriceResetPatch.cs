using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Economy;

namespace SR2MP.Patches.Economy;

[HarmonyPatch(typeof(PlortEconomyDirector), nameof(PlortEconomyDirector.ResetPrices))]
public static class PriceResetPatch
{
    public static bool Prefix()
        => !Main.Client.IsConnected;

    public static void Postfix()
    {
        if (!Main.Server.IsRunning())
            return;

        var packet = new MarketPricePacket() { Prices = MarketPricesArray! };

        Main.SendToAllOrServer(packet);
    }
}