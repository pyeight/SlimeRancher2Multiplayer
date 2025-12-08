using MelonLoader;
using MelonLoader.Utils;
using SR2E.Managers;

namespace SR2MP;

public static class Logger
{
    private static readonly MelonLogger.Instance melonLogger;

    internal static string logPath;
    internal static string sensitiveLogPath;

    static Logger()
    {
        melonLogger = new MelonLogger.Instance("SR2MP");

        string folderPath = Path.Combine(MelonEnvironment.UserDataDirectory, "SR2MP");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        logPath = Path.Combine(folderPath, "latest.log");
        sensitiveLogPath = Path.Combine(folderPath, "sensitive.log");

        try
        {
            File.WriteAllText(logPath, $"{DateTime.Now} [SR2MP] Log Initialized!");
            File.WriteAllText(sensitiveLogPath, $"{DateTime.Now} [SR2MP] Sensitive Log Initialized!");
        }
        catch (Exception ex)
        {
            melonLogger.Error($"Failed to initialize log files: {ex}");
        }
    }

    public static void Log(string message) => Log(message, 100);

    public static void Log(string message, int sr2eSize)
    {
        SR2ELogManager.SendMessage($"<size={sr2eSize}%>{message}</size>");
        melonLogger.Msg(message);
        WriteToFile("[INFO]", message, false);
    }

    public static void LogSensitive(string message)
    {
        WriteToFile("[SENSITIVE]", message, true);
    }

    public static void Warn(string message)
    {
        SR2ELogManager.SendWarning(message);
        melonLogger.Warning(message);
        WriteToFile("[WARNING]", message, false);
    }

    public static void WarnSensitive(string message)
    {
        melonLogger.Warning("Warning created in sensitive.log!");
        WriteToFile("[WARNING]", message, true);
    }


    public static void Error(string message)
    {
        SR2ELogManager.SendError(message);
        melonLogger.Error(message);
        WriteToFile("[ERROR]", message, false);
    }

    public static void ErrorSensitive(string message)
    {
        melonLogger.Error("Error created in sensitive.log!");
        WriteToFile("[ERROR]", message, true);
    }

    public static void Debug(string message)
    {
        WriteToFile("[DEBUG]", message, false);
    }

    public static void DebugSensitive(string message)
    {
        WriteToFile("[DEBUG]", message, true);
    }

    private static void WriteToFile(string level, string message, bool sensitive)
    {
        try
        {
            string line = $"{DateTime.Now:HH:mm:ss} {level} {message}";
            File.AppendAllText(sensitive ? sensitiveLogPath : logPath, "\n" + line);
        }
        catch (Exception ex)
        {
            melonLogger.Error($"Failed to write to log file: {ex}");
        }
    }
}
