using HarmonyLib;
using Activator = Il2CppSystem.Activator;
using Type = Il2CppSystem.Type;

namespace SR2MP.Patches.UI;

[HarmonyPatch(typeof(GUIStateObjects))]
internal static class GUIStateObjectsMultiPatch
{
    private static Dictionary<int, Il2CppSystem.Object> stateCache = new();
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GUIStateObjects.QueryStateObject))]
    internal static bool QueryStateObject(Type t, int controlID, ref Il2CppSystem.Object __result)
    {
        Il2CppSystem.Object il2cppObject = stateCache[controlID];
        __result = (t.IsInstanceOfType(il2cppObject) ? il2cppObject : null)!;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GUIStateObjects.GetStateObject))]
    public static bool GetStateObject(Type t, int controlID, ref Il2CppSystem.Object __result)
    {
        if (!stateCache.TryGetValue(controlID, out var instance) || instance.GetIl2CppType() != t)
        {
            instance = Activator.CreateInstance(t);
            stateCache[controlID] = instance;
        }
    
        __result = instance;
        return false;
    }
}