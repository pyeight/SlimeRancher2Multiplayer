using HarmonyLib;
using SR2E.Managers;

namespace SR2MP.Patches.Compatibility;

[HarmonyPatch(typeof(SR2ECommandManager), nameof(SR2ECommandManager.ExecuteByString), typeof(string), typeof(bool), typeof(bool))]
public static class ConsoleCheatPatch
{
    public static bool Prefix(string input)
    {
        if (!(Main.Server.IsRunning() || Main.Client.IsConnected))
            return true;

        if (cheatsEnabled)
            return true;

        var containsCheat = false;

        // Code copied from SR2E
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

        SR2ELogManager.SendError("Cheats are disabled on this server!");
        return false;
    }
}