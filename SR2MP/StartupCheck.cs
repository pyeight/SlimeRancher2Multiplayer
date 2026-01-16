using System.Runtime.InteropServices;
using MelonLoader;

namespace SR2MP
{
    public static class StartupCheck
    {
        private const string RequiredGameVersion = BuildInfo.ExactRequiredGameVersion;
        private const string VersionUrl = "https://raw.githubusercontent.com/pyeight/SlimeRancher2Multiplayer/refs/heads/master/latestModVersion.txt";
        private const string DiscordUrl = BuildInfo.Discord;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ShellExecuteW(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

        private const uint MB_OK = 0x00000000;
        private const uint MB_ICONERROR = 0x00000010;
        private const uint MB_ICONWARNING = 0x00000030;

        private static bool shouldQuit = false;

        public static void Initialize()
        {
            var installedGameVersion = MelonLoader.InternalUtils.UnityInformationHandler.GameVersion;
            installedGameVersion = installedGameVersion.Split(' ')[0];

            if (installedGameVersion != RequiredGameVersion)
            {
                MessageBoxW(
                    IntPtr.Zero,
                    "SR2MP is incompatible with this game version.\n\n" +
                    $"Required: {RequiredGameVersion}\n" +
                    $"Detected: {installedGameVersion}",
                    "SR2MP – Incompatible Game Version",
                    MB_OK | MB_ICONERROR
                );
                Application.Quit();
                return;
            }
            
            Task.Run(async () => await CheckModVersion());
            
            MelonCoroutines.Start(QuitCoroutine());
        }

        private static System.Collections.IEnumerator QuitCoroutine()
        {
            while (!shouldQuit)
            {
                yield return null;
            }
            Application.Quit();
        }

        private static async Task CheckModVersion()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    string currentModVersion = BuildInfo.DisplayVersion;

                    string latestVersion = (await client.GetStringAsync(VersionUrl)).Trim();

                    int comparison = CompareVersions(currentModVersion, latestVersion);

                    if (comparison < 0)
                    {
                        MessageBoxW(
                            IntPtr.Zero,
                            "Your SR2MP mod is outdated!\n\n" +
                            $"Your version: {currentModVersion}\n" +
                            $"Latest version: {latestVersion}\n\n" +
                            "Click OK to join our Discord or get a new version from NexusMods",
                            "SR2MP – Update Available",
                            MB_OK | MB_ICONWARNING
                        );
                        ShellExecuteW(IntPtr.Zero, "open", DiscordUrl, null!, null!, 1);
                        shouldQuit = true;
                    }
                    else if (comparison > 0)
                    {
                        MessageBoxW(
                            IntPtr.Zero,
                            "Your SR2MP mod version is newer than the latest release.\n\n" +
                            $"Your version: {currentModVersion}\n" +
                            $"Latest version: {latestVersion}\n\n" +
                            "This version is not officially supported and may not work correctly.",
                            "SR2MP – Unsupported Version",
                            MB_OK | MB_ICONWARNING
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogWarning($"Failed to check SR2MP version\n{ex}", SrLogTarget.Both);
            }
        }

        private static int CompareVersions(string version1, string version2)
        {
            var v1Parts = version1.Split('.');
            var v2Parts = version2.Split('.');
            int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

            for (int i = 0; i < maxLength; i++)
            {
                int v1 = i < v1Parts.Length && int.TryParse(v1Parts[i], out int v1Val) ? v1Val : 0;
                int v2 = i < v2Parts.Length && int.TryParse(v2Parts[i], out int v2Val) ? v2Val : 0;

                if (v1 < v2) return -1;
                if (v1 > v2) return 1;
            }
            return 0;
        }
    }
}