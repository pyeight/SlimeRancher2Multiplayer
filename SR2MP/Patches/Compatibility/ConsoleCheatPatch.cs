using HarmonyLib;
using SR2E;
using SR2E.Managers;

namespace SR2MP.Patches.Compatibility;

[HarmonyPatch(typeof(SR2ECommandManager), nameof(SR2ECommandManager.ExecuteByString), typeof(string), typeof(bool), typeof(bool))]
public static class ConsoleCheatPatch
{
    public static bool Prefix(string input, bool silent, bool alwaysPlay)
    {
        if (!(Main.Server.IsRunning() || Main.Client.IsConnected))
            return true;

        if (CheatsEnabled)
            return true;

        var containsCheat = false;
        
        // Code copied from SR2E
        string[] cmds = input.Split(';');
        foreach (string cc in cmds)
        {
            string c = cc.TrimStart(' ');
            if (!string.IsNullOrWhiteSpace(c))
            {
                bool spaces = c.Contains(" ");
                string cmd = spaces ? c.Substring(0, c.IndexOf(' ')) : c;

                if (CheatCommands.Contains(cmd))
                {
                    containsCheat = true;
                    break;
                }
            }
        }

        if (containsCheat)
        {
            SR2ELogManager.SendError("Cheats are disabled on this server!");
            return false;
        }

        return true;
    }
}