using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Script.UI.Pause;

namespace SR2MP.Patches.General;

[HarmonyPatch(typeof(PauseMenuDirector), nameof(PauseMenuDirector.Quit))]
public static class OnQuitSave
{
    public static void Postfix()
    {
        if (Main.Server.IsRunning())
            Main.Server.Close();
        if (Main.Client.IsConnected)
            Main.Client.Disconnect();
    }
}