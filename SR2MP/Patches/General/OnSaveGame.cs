using HarmonyLib;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Patches.General;

[HarmonyPatch(typeof(FileStorageProvider), nameof(FileStorageProvider.StoreGameData))]
internal static class OnSaveGame
{
    public static void Prefix(FileStorageProvider __instance, string name)
        => SaveBackupManager.BackupOnce(__instance.savePath, name);
}

[HarmonyPatch(typeof(FileStorageProvider), nameof(FileStorageProvider.GetGameData))]
internal static class OnLoadGame
{
    public static void Prefix(FileStorageProvider __instance, string fileName)
        => SaveBackupManager.TrackSave(__instance.savePath, fileName);
}

[HarmonyPatch(typeof(AutoSaveDirector), nameof(AutoSaveDirector.OnSessionEnded))]
internal static class OnSaveSessionEnded
{
    public static void Postfix() => SaveBackupManager.ResetSession();
}

[HarmonyPatch(typeof(SceneContext), nameof(SceneContext.Start))]
internal static class OnSaveBackupSubscribe
{
    private static bool subscribed;

    public static void Postfix()
    {
        if (subscribed) return;
        subscribed = true;

        Main.Server.OnServerStarted += SaveBackupManager.BackupBeforeHost;
        Main.Client.OnConnected += _ => SaveBackupManager.BackupBeforeConnect();
        Main.Client.OnDisconnected += () =>
        {
            if (MainThreadDispatcher.Instance)
                MainThreadDispatcher.Instance.Enqueue(SaveBackupManager.BackupAfterDisconnect);
        };
    }
}