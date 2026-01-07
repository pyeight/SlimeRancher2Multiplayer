using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Utils;
using PriceDictionary = Il2CppSystem.Collections.Generic.Dictionary<Il2Cpp.IdentifiableType, Il2CppMonomiPark.SlimeRancher.Economy.PlortEconomyDirector.CurrValueEntry>;

namespace SR2MP.Patches.Economy;

[HarmonyPatch(typeof(PlortEconomyDirector), nameof(PlortEconomyDirector.ResetPrices))]
public class PriceResetPatch
{
    public static bool Prefix() 
        => !Main.Client.IsConnected;
    
    public static void Postfix(PlortEconomyDirector __instance, WorldModel worldModel, int day)
    {
        if (Main.Client.IsConnected)
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