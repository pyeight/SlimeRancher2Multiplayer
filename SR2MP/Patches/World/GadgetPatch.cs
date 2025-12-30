using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.World;
using UnityEngine;
using SR2MP.Components.World;
using SR2MP.Packets.Shared;
using SR2MP.Packets.Utils;

namespace SR2MP.Patches.World;

[HarmonyPatch(typeof(Gadget))]
public static class GadgetPatch
{
    [HarmonyPatch(nameof(Gadget.Awake))]
    [HarmonyPostfix]
    public static void AwakePostfix(Gadget __instance)
    {
        if (__instance.GetComponent<NetworkGadget>()) return;

        var netGadget = __instance.gameObject.AddComponent<NetworkGadget>();

        if (!string.IsNullOrEmpty(GlobalVariables.currentlyInstantiatingGadgetId))
        {
            netGadget.GadgetId = GlobalVariables.currentlyInstantiatingGadgetId;
            GlobalVariables.currentlyInstantiatingGadgetId = string.Empty;
        }
        else
        {
            netGadget.GadgetId = global::System.Guid.NewGuid().ToString();
        }

        if (!GlobalVariables.gadgetsById.ContainsKey(netGadget.GadgetId))
        {
            GlobalVariables.gadgetsById.Add(netGadget.GadgetId, __instance.gameObject);
        }
    }
}
