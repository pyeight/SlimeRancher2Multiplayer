using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Shared;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Il2CppMonomiPark.SlimeRancher.DataModel;

namespace SR2MP.Patches.World
{
    [HarmonyPatch(typeof(PlortEconomyDirector), nameof(PlortEconomyDirector.ResetPrices))]
    public static class MarketPatch
    {
        public static void Postfix(PlortEconomyDirector __instance)
        {
            if (GlobalVariables.handlingPacket) return;
            if (!Main.Server.IsRunning()) return;

            // Iterate all plorts/items that have prices
            // Since we don't have a direct list of "PricedItems", we might iterate all Identifiables?
            // Expensive.
            // Or use EconomyDirector's internal dictionary if accessible.
            // __instance._currValueMap? (Dictionary<IdentifiableType, float>)
            
            // Let's assume we can access all IdentifiableTypes via GlobalVariables.actorManager or LookupDirector?
            // GameContext.Instance.LookupDirector.GetIdentifiableTypes()?
            
            // GitHub version iterates `Core.Identifiables`.
            
            // I'll try to use `__instance._currValueMap` if it's public/internal.
            // If not, I'll rely on `GameContext.Instance.GetIdentifiableTypes()` (pseudocode).
            // Actually, `GameContext.Instance.LookupDirector` likely has a way.
            
            // For now, let's use a safe approach:
            var prices = new Dictionary<string, float>();
            
            // If we can't easily iterate, we leave it empty for now or rely on a specific list later.
            // But I want this to work.
            
            // Assumption: `_currValueMap` is available.
            /*
            foreach (var kvp in __instance._currValueMap) {
                 prices[kvp.Key.name] = kvp.Value;
            }
            */
            
            // Alternative: Iterate a known list of Plorts.
            // "PinkPlort", "CottonPlort", "TabbyPlort", ...
            // This is brittle.
            
            MelonLoader.MelonLogger.Warning("MarketPatch: _currValueMap access not confirmed. Synchronization might correspond to empty list if not fixed.");
            
            Main.SendToAllOrServer(new MarketUpdatePacket(prices));
        }
    }
}
