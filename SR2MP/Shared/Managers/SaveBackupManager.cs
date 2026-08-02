using MelonLoader.Utils;

namespace SR2MP.Shared.Managers;

internal enum BackupPhase
{
    Offline,
    BeforeHost,
    WhileHost,
    BeforeConnect,
    WhileConnected,
    AfterDisconnect,
    Manual
}

internal static class SaveBackupManager
{
    private const string SaveExtension = ".sav";
    private const string TimeFormat = "yyyy-MM-dd_HH-mm-ss";

    private static readonly string[] ExtraFiles
        = { "slimerancher.prf", "slimerancher.cfg" };

    private static readonly string[] StoreFolders
        = { "Steam", "Epic", "EpicGames", "Xbox", "Microsoft", "Standalone" };
    
    private static readonly HashSet<string> SessionBackups = new(StringComparer.Ordinal);

    private static string? saveDirectory;
    private static string? currentSave;

    internal static string BackupRoot 
        => Path.Combine(MelonEnvironment.UserDataDirectory, "SR2MP", "SaveBackups");

    internal static void ResetSession()
    {
        currentSave = null;
        SessionBackups.Clear();
    }

    internal static void TrackSave(string directory, string fileName)
    {
        saveDirectory = directory;
        currentSave = GetSaveIdent(fileName);
    }

    internal static void BackupOnce(string directory, string fileName)
    {
        var save = GetSaveIdent(fileName);

        saveDirectory = directory;
        currentSave = save;

        if (Main.SaveBackups)
            Backup(directory, save, CurrentPhase(), false);
    }

    internal static void BackupBeforeHost()
        => TryBackupForcefully(BackupPhase.BeforeHost, true);

    internal static void BackupBeforeConnect()
        => TryBackupForcefully(BackupPhase.BeforeConnect, true);

    internal static void BackupAfterDisconnect()
        => TryBackupForcefully(BackupPhase.AfterDisconnect, false);

    internal static bool BackupNow()
        => BackupCurrent(BackupPhase.Manual, true);
    
    private static void TryBackupForcefully(BackupPhase phase, bool force)
    {
        if (Main.SaveBackupDisableForced)
            return;

        BackupCurrent(phase, force);
    }

    private static bool IsPeriodic(BackupPhase phase)
        => phase is BackupPhase.Offline or BackupPhase.WhileHost or BackupPhase.WhileConnected;

    private static BackupPhase CurrentPhase()
        => Main.Server.IsRunning ? BackupPhase.WhileHost 
            : Main.Client.IsJoined ? BackupPhase.WhileConnected
            : BackupPhase.Offline;

    private static bool BackupCurrent(BackupPhase phase, bool force)
    {
        var directory = GetSaveDirectory();

        if (directory == null)
        {
            SrLogger.LogWarning("SaveBackupManager: could not locate the save folder.");
            return false;
        }

        var save = currentSave ?? GetMostRecentSave(directory);

        if (save != null)
            return Backup(directory, save, phase, force);

        SrLogger.LogWarning("SaveBackupManager: no saves to back up.");
        return false;
    }

    private static bool Backup(string directory, string save, BackupPhase phase, bool force)
    {
        if (!force && !SessionBackups.Add($"{save}|{phase}"))
            return false;

        try
        {
            var autosaves = GetAutosaves(directory, save);

            if (autosaves.Count == 0)
            {
                SrLogger.LogDebug($"SaveBackupManager: no autosaves of {save} in {directory}");
                return false;
            }

            var folder = Sanitize(save);

            if (!force && IsPeriodic(phase) && IsUnchanged(folder, autosaves))
            {
                SrLogger.LogDebug($"SaveBackupManager: {save} is unchanged since the last backup");
                return false;
            }

            var backupFolder = NewBackupFolder(folder);

            try
            {
                var target = Directory.CreateDirectory(
                    Path.Combine(backupFolder, FolderName(phase), RestorePath(directory)));

                foreach (var file in autosaves.Concat(GetExtraFiles(directory)))
                    File.Copy(file.FullName, Path.Combine(target.FullName, file.Name), true);
            }
            catch
            {
                Discard(backupFolder);
                throw;
            }

            Prune(folder);

            SrLogger.LogMessage($"Backed up {autosaves.Count} autosave(s) of {save} ({FolderName(phase)})",
                $"Backed up {autosaves.Count} autosave(s) of {save} from {directory} into {backupFolder}");

            return true;
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"SaveBackupManager: backup failed: {ex}");
            return false;
        }
    }

    private static string GetSaveIdent(string fileName)
    {
        var name = fileName.EndsWith(SaveExtension, StringComparison.OrdinalIgnoreCase)
            ? fileName[..^SaveExtension.Length]
            : fileName;

        var separator = name.LastIndexOf('_');
        return separator > 0 ? name[..separator] : name;
    }

    private static string? GetMostRecentSave(string directory)
        => new DirectoryInfo(directory).EnumerateFiles('*' + SaveExtension)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Select(file => GetSaveIdent(file.Name))
            .FirstOrDefault();

    private static string? GetSaveDirectory()
    {
        var path = GameContext.Instance?.AutoSaveDirector?._storageProvider?.TryCast<FileStorageProvider>()?.savePath;

        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            return saveDirectory = path;

        return saveDirectory ??= SearchSaveDirectory();
    }
    
    private static string? SearchSaveDirectory()
    {
        var root = Application.persistentDataPath;

        if (!Directory.Exists(root))
            return null;

        // I could have written this different 20 times
        // for the time I spent on this,
        // however, this is more fancy (and less readable),
        // So we go with this one
        // (I definetly (spelling stays the same) did not make it worse on purpose, I would never)
        return StoreFolders.Select(store => Path.Combine(root, store))
            .Where(Directory.Exists)
            .Concat(Directory.EnumerateDirectories(root))
            .SelectMany(store => Directory.EnumerateDirectories(store).Prepend(store))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Prepend(root)
            .Select(candidate => (candidate, written: NewestAutosaveWrite(candidate)))
            .Where(entry => entry.written > DateTime.MinValue)
            .OrderByDescending(entry => entry.written)
            .Select(entry => entry.candidate)
            .FirstOrDefault();
    }

    private static DateTime NewestAutosaveWrite(string directory)
    {
        try
        {
            return new DirectoryInfo(directory).EnumerateFiles('*' + SaveExtension)
                .Select(file => file.LastWriteTimeUtc)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();
        }
        catch (Exception)
        {
            return DateTime.MinValue;
        }
    }

    private static List<FileInfo> GetAutosaves(string directory, string save)
    {
        var prefix = save + '_';

        return new DirectoryInfo(directory).EnumerateFiles(prefix + '*' + SaveExtension)
            .Where(file => IsAutosave(file.Name, prefix))
            .OrderBy(file => file.Name, StringComparer.Ordinal)
            .ToList();
    }
    
    private static bool IsAutosave(string fileName, string prefix)
    {
        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !fileName.EndsWith(SaveExtension, StringComparison.OrdinalIgnoreCase))
            return false;

        var index = fileName[prefix.Length..^SaveExtension.Length];
        return index.Length > 0 && index.All(c => c is >= '0' and <= '9');
    }

    private static IEnumerable<FileInfo> GetExtraFiles(string directory)
        => ExtraFiles.Select(name => new FileInfo(Path.Combine(directory, name))).Where(file => file.Exists);
    
    private static string RestorePath(string directory)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        var user = Path.GetFileName(full);

        if (string.IsNullOrEmpty(user))
            return "Saves";

        var store = Path.GetFileName(Path.GetDirectoryName(full) ?? string.Empty);
        return string.IsNullOrEmpty(store) ? user : Path.Combine(store, user);
    }
    
    private static bool IsUnchanged(string save, List<FileInfo> autosaves)
    {
        var newest = SaveBackups(save).FirstOrDefault();

        if (newest == null)
            return false;

        var backed = new DirectoryInfo(newest).GetFiles("*", SearchOption.AllDirectories)
            .GroupBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        return autosaves.All(autosave => backed.TryGetValue(autosave.Name, out var copy)
                                         && copy.Length == autosave.Length
                                         && copy.LastWriteTimeUtc == autosave.LastWriteTimeUtc);
    }

    private static string NewBackupFolder(string save)
    {
        var stamp = DateTime.Now.ToString(TimeFormat);
        var backupFolder = Path.Combine(BackupRoot, stamp, save);
        var attempt = 1;

        while (Directory.Exists(backupFolder))
            backupFolder = Path.Combine(BackupRoot, $"{stamp}_{attempt++}", save);

        Directory.CreateDirectory(backupFolder);
        return backupFolder;
    }

    // Backups are pruned by phase
    private static void Prune(string save)
    {
        if (!Main.SaveBackupPruning)
            return;

        var outdated = SaveBackups(save)
            .GroupBy(PhaseOf, StringComparer.OrdinalIgnoreCase)
            .SelectMany(phase => phase.Skip(Main.SaveBackupCount))
            .ToArray();

        foreach (var backup in outdated)
            Discard(backup);
    }

    // Drops one backup of one save,
    // and the timestamp folder around it once no other save is left in it
    private static void Discard(string saveBackup)
    {
        try
        {
            Directory.Delete(saveBackup, true);
        }
        catch (Exception ex)
        {
            SrLogger.LogWarning($"SaveBackupManager: could not delete {saveBackup}: {ex.Message}");
            return;
        }

        DeleteIfEmpty(Path.GetDirectoryName(saveBackup));
    }

    private static void DeleteIfEmpty(string? folder)
    {
        try
        {
            if (folder != null && Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                Directory.Delete(folder);
        }
        catch (Exception ex)
        {
            // Harmless, we dont need to send a warning
            SrLogger.LogDebug($"SaveBackupManager: could not remove empty {folder}: {ex.Message}");
        }
    }

    // Every backup of one save, newest first
    // Very fancy, I know
    private static IEnumerable<string> SaveBackups(string save)
        => (Directory.Exists(BackupRoot)
                ? Directory.EnumerateDirectories(BackupRoot)
                    .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
                : Enumerable.Empty<string>())
            .Select(backup => Path.Combine(backup, save))
            .Where(Directory.Exists);

    private static string PhaseOf(string saveBackup)
        => Directory.EnumerateDirectories(saveBackup).Select(Path.GetFileName).FirstOrDefault() ?? string.Empty;

    private static string FolderName(BackupPhase phase) => phase.ToString().ToLowerInvariant();

    private static string Sanitize(string name)
        => string.Join('_', name.Split(Path.GetInvalidFileNameChars()));
}