using HarmonyLib;

namespace SR2MP.Patches.UI;

[HarmonyPatch(typeof(GUIStateObjects))]
internal static class GUIStateObjectsMultiPatch
{
    private static Dictionary<int, Il2CppSystem.Object> stateCache = new();
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GUIStateObjects.QueryStateObject))]
    internal static bool QueryStateObject(Il2CppSystem.Type t, int controlID, ref Il2CppSystem.Object __result)
    {
        Il2CppSystem.Object il2cppObject = stateCache[controlID];
        __result = (t.IsInstanceOfType(il2cppObject) ? il2cppObject : null)!;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GUIStateObjects.GetStateObject))]
    public static bool GetStateObject(Il2CppSystem.Type t, int controlID, ref Il2CppSystem.Object __result)
    {
        if (!stateCache.TryGetValue(controlID, out var instance) || instance.GetIl2CppType() != t)
        {
            instance = Il2CppSystem.Activator.CreateInstance(t);
            stateCache[controlID] = instance;
        }
    
        __result = instance;
        return false;
    }
}