using HarmonyLib;
using Starlight.Managers;

namespace SR2MP.Patches.Compatibility;

[HarmonyPatch(typeof(StarlightCommandManager), nameof(StarlightCommandManager.ExecuteByString), typeof(string), typeof(bool), typeof(bool))]
internal static class ConsoleCheatPatch
{
    public static bool Prefix(string input)
    {
        if (!(Main.Server.IsRunning || Main.Client.IsConnected))
            return true;

        if (CheatsEnabled)
            return true;

        var containsCheat = false;

        // Code copied from Starlight
        var cmds = input.Split(';');
        foreach (var cc in cmds)
        {
            var c = cc.TrimStart(' ');
            if (string.IsNullOrWhiteSpace(c))
                continue;
            var spaces = c.Contains(' ');
            var cmd = spaces ? c[..c.IndexOf(' ')] : c;

            if (!CheatCommands.Contains(cmd))
                continue;
            containsCheat = true;
            break;
        }

        if (!containsCheat)
            return true;

        StarlightLogManager.SendError("Cheats are disabled on this server!");
        return false;
    }
}