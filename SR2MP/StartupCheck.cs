using System.Runtime.InteropServices;
using MelonLoader;

namespace SR2MP;

public static class StartupCheck
{
    private const string RequiredGameVersion = BuildInfo.ExactRequiredGameVersion;
    private const string VersionUrl = "https://raw.githubusercontent.com/pyeight/SlimeRancher2Multiplayer/refs/heads/master/latestModVersion.txt";
    private const string DiscordUrl = BuildInfo.Discord;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ShellExecuteW(
        IntPtr hwnd,
        string lpOperation,
        string lpFile,
        string lpParameters,
        string lpDirectory,
        int nShowCmd
    );

    private const uint MB_OK = 0x00000000;
    private const uint MB_ICONERROR = 0x00000010;
    private const uint MB_ICONWARNING = 0x00000030;
    private const int SW_SHOWNORMAL = 1;
    private const float TIMEOUT = 30f;

    private static volatile bool shouldQuit = false;

    public static void Initialize()
    {
        var installedGameVersion = MelonLoader.InternalUtils.UnityInformationHandler.GameVersion;

        var versionParts = installedGameVersion.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (versionParts.Length > 0)
        {
            installedGameVersion = versionParts[0];
        }

        switch (CompareVersions(installedGameVersion, RequiredGameVersion))
        {
            case < 0:
                ShowMessageBox(
                    "SR2MP is incompatible with this game version.\n\n" +
                    $"Required: {RequiredGameVersion}\n" +
                    $"Detected: {installedGameVersion}",
                    "SR2MP – Incompatible Game Version",
                    MB_OK | MB_ICONERROR, true
                );
                Application.Quit();
                return;
            case > 0:
                ShowMessageBox(
                    "You are running a newer game version than SR2MP was built for.\n\n" +
                    $"Required: {RequiredGameVersion}\n" +
                    $"Detected: {installedGameVersion}\n\n" +
                    "The mod may still work, but issues are possible.",
                    "SR2MP – Newer Game Version Detected",
                    MB_OK | MB_ICONWARNING, false
                );
                break;
        }

        Task.Run(async () => await CheckModVersionAsync());
        MelonCoroutines.Start(QuitCoroutine());
    }

    private static System.Collections.IEnumerator QuitCoroutine()
    {
        var elapsed = 0f;

        while (!shouldQuit && elapsed < TIMEOUT)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (shouldQuit)
        {
            Application.Quit();
        }
    }

    private static async Task CheckModVersionAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            const string currentModVersion = BuildInfo.DisplayVersion;
            var latestVersion = (await client.GetStringAsync(VersionUrl)).Trim();

            switch (CompareVersions(currentModVersion, latestVersion))
            {
                case < 0:
                    ShowMessageBox(
                        "Your SR2MP mod is outdated!\n\n" +
                        $"Your version: {currentModVersion}\n" +
                        $"Latest version: {latestVersion}\n\n" +
                        "Click OK to join our Discord or get a new version from NexusMods.\n" +
                        "The game will close after clicking OK.",
                        "SR2MP – Update Available",
                        MB_OK | MB_ICONWARNING, true
                    );

                    OpenUrl(DiscordUrl);
                    shouldQuit = true;
                    break;

                case > 0:
                    ShowMessageBox(
                        "Your SR2MP mod version is newer than the latest release.\n\n" +
                        $"Your version: {currentModVersion}\n" +
                        $"Latest version: {latestVersion}\n\n" +
                        "This version is not officially supported and may not work correctly.",
                        "SR2MP – Unsupported Version",
                        MB_OK | MB_ICONWARNING, false
                    );
                    break;
            }
        }
        catch (TaskCanceledException)
        {
            SrLogger.LogWarning("SR2MP version check timed out", SrLogTarget.Both);
        }
        catch (HttpRequestException ex)
        {
            SrLogger.LogWarning($"SR2MP version check failed: Network error\n{ex.Message}", SrLogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Failed to check SR2MP version\n{ex}", SrLogTarget.Both);
        }
    }

    private static void ShowMessageBox(string text, string caption, uint type, bool error)
    {
        try
        {
            MessageBoxW(IntPtr.Zero, text, caption, type);

            if (error)
                SrLogger.LogError($"{caption}\n{text}", SrLogTarget.Both);
            else
                SrLogger.LogWarning($"{caption}\n{text}", SrLogTarget.Both);
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"{caption}\n{text}\n{ex}", SrLogTarget.Both);
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            ShellExecuteW(IntPtr.Zero, "open", url, null!, null!, SW_SHOWNORMAL);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"Could not open URL: {url}\n{ex.Message}", SrLogTarget.Both);
        }
    }

    private static int CompareVersions(string version1, string version2)
    {
        var v1Parts = version1.Split('.');
        var v2Parts = version2.Split('.');
        var maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

        for (var i = 0; i < maxLength; i++)
        {
            var v1 = i < v1Parts.Length && int.TryParse(v1Parts[i], out var v1Val) ? v1Val : 0;
            var v2 = i < v2Parts.Length && int.TryParse(v2Parts[i], out var v2Val) ? v2Val : 0;

            if (v1 < v2) return -1;
            if (v1 > v2) return 1;
        }
        return 0;
    }
}