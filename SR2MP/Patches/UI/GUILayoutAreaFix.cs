using HarmonyLib;

namespace SR2MP.Patches.UI;

[HarmonyPatch(typeof(GUILayout), nameof(GUILayout.BeginArea), typeof(Rect), typeof(GUIContent), typeof(GUIStyle))]
public static class GUILayoutAreaFix
{
    // This is directly copied from https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/IMGUI/GUILayout.cs
    // because interop failed to recover the function.
    //
    // There are minor changes so IL2CPP doesn't freak out.
    public static bool Prefix(Rect screenRect, GUIContent content, GUIStyle style)
    {
        GUIUtility.CheckOnGUI();
        var g = GUILayoutUtility.BeginLayoutArea(style, Il2CppSystem.Type.GetType("UnityEngine.GUILayoutGroup"));
        if (Event.current.type == EventType.Layout)
        {
            g.resetCoords = true;
            g.minWidth = g.maxWidth = screenRect.width;
            g.minHeight = g.maxHeight = screenRect.height;
            g.rect = Rect.MinMaxRect(screenRect.xMin, screenRect.yMin, g.rect.xMax, g.rect.yMax);
        }

        GUI.BeginGroup(g.rect, content, style);

        return false;
    }
}