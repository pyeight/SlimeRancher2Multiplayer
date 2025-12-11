using MelonLoader;
using MelonLoader.Utils;
using SR2E.Managers;

namespace SR2MP;

public static class Logger
{
    private enum LogLevel : byte
    {
        Debug,
        Message,
        Warning,
        Error
    }

    private static readonly MelonLogger.Instance melonLogger;
    private static readonly LogHandler log;
    private static readonly LogHandler sensitiveLog;

    static Logger()
    {
        melonLogger = new MelonLogger.Instance("SR2MP");

        string folderPath = Path.Combine(MelonEnvironment.UserDataDirectory, "SR2MP");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        log = new LogHandler(false, Path.Combine(folderPath, "latest.log"));
        sensitiveLog = new LogHandler(false, Path.Combine(folderPath, "sensitive.log"));

        log.WriteToFile("Log Initialized!", LogLevel.Message);
        sensitiveLog.WriteToFile("Sensitive Log Initialized!", LogLevel.Message);
    }

    public static void LogMessage(object message) => log.Log(message, LogLevel.Message, SR2ELogManager.SendMessage, melonLogger.Msg, WriteToFile);

    public static void LogMessageSensitive(object message) => sensitiveLog.Log(message, LogLevel.Message, SR2ELogManager.SendMessage, melonLogger.Msg, WriteToFile);

    public static void LogMessageBoth(object message)
    {
        LogMessage(message);
        LogMessageSensitive(message);
    }

    public static void LogWarning(object message) => log.Log(message, LogLevel.Warning, SR2ELogManager.SendWarning, melonLogger.Warning, WriteToFile);

    public static void LogWarningSensitive(object message) => sensitiveLog.Log(message, LogLevel.Warning, SR2ELogManager.SendWarning, melonLogger.Warning, WriteToFile);

    public static void LogWarningBoth(object message)
    {
        LogWarning(message);
        LogWarningSensitive(message);
    }

    public static void LogError(object message) => log.Log(message, LogLevel.Error, SR2ELogManager.SendError, melonLogger.Error, WriteToFile);

    public static void LogErrorSensitive(object message) => sensitiveLog.Log(message, LogLevel.Error, SR2ELogManager.SendError, melonLogger.Error, WriteToFile);

    public static void LogErrorBoth(object message)
    {
        LogError(message);
        LogErrorSensitive(message);
    }

    public static void LogDebug(object message) => log.Log(message, LogLevel.Debug, null!, null!, WriteToFile);

    public static void LogDebugSensitive(object message) => sensitiveLog.Log(message, LogLevel.Debug, null!, null!, WriteToFile);

    public static void LogDebugBoth(object message)
    {
        LogDebug(message);
        LogDebugSensitive(message);
    }

    private static void WriteToFile(object message, LogLevel level, bool sensitive)
    {
        try
        {
            string line = Format(message, level);
            (sensitive ? sensitiveLog : log).AddToFile(line, level);
        }
        catch (Exception ex)
        {
            melonLogger.Error($"Failed to write to log file: {ex}");
        }
    }

    private static void WriteToBothFiles(LogLevel level, object message)
    {
        try
        {
            string line = Format(message, level);
            log.AddToFile(line, level);
            sensitiveLog.AddToFile(line, level);
        }
        catch (Exception ex)
        {
            melonLogger.Error($"Failed to write to log file: {ex}");
        }
    }

    private static string Format(object message, LogLevel level)
    {
        var str = message.ToString()!;
        return str.StartsWith('[')
            ? str // Assumes the message is already formatted as needed
            : $"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpperInvariant()}] {str}";
    }

    private sealed class LogHandler(bool isSensitive, string logsPath)
    {
        private bool sensitive = isSensitive;
        private string logPath = logsPath;

        public void AddToFile(object message, LogLevel level) => ModifyFile(message, level, File.AppendAllText, true);

        public void WriteToFile(object message, LogLevel level) => ModifyFile(message, level, File.WriteAllText, false);

        private void ModifyFile(object message, LogLevel level, Action<string, string> write, bool adding)
        {
            try
            {
                write(logPath, (adding ? "\n" : string.Empty) + Format(message, level));
            }
            catch (Exception ex)
            {
                melonLogger.Error($"Failed to write to {(sensitive ? "sensitive " : string.Empty)}log file: {ex}");
            }
        }

        public void Log(object message, LogLevel level, Action<string> sr2e, Action<string> melon, Action<object, LogLevel, bool> write)
        {
            var str = message.ToString()!;
            sr2e?.Invoke(str);
            melon?.Invoke(str);
            write(str, level, sensitive);
        }
    }
}
