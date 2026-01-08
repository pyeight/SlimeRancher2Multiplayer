using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Loading;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.Economy;

[HarmonyPatch(typeof(PlortEconomyDirector), nameof(PlortEconomyDirector.ResetPrices))]
public static class PriceResetPatch
{
    public static bool Prefix() 
        => !Main.Client.IsConnected;
    
    public static void Postfix(PlortEconomyDirector __instance, WorldModel worldModel, int day)
    {
        if (!Main.Server.IsRunning())
            return;
        
        new Dictionary<byte, byte>();
        var packet = new MarketPricePacket()
        {
            Type = (byte)PacketType.MarketPriceChange,
            Prices = MarketPricesArray!,
        };
        
        Main.SendToAllOrServer(packet);
    }
}