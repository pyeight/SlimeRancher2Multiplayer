using System.Text;
using MelonLoader;
using MelonLoader.Logging;
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

    [Flags]
    public enum LogTarget : byte
    {
        Neither = 0, // Required to follow the standard but PLEASE don't use this value!
        Main = 1 << 0,
        Sensitive = 1 << 1,
        Both = Main | Sensitive
    }

    private static readonly MelonLogger.Instance _melonLogger;
    private static readonly LogHandler _logHandler;
    private static readonly LogHandler _sensitiveLogHandler;

    static Logger()
    {
        _melonLogger = new MelonLogger.Instance("SR2MP", ColorARGB.FromArgb(77, 149, 203));

        var folderPath = Path.Combine(MelonEnvironment.UserDataDirectory, "SR2MP");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        _logHandler = new LogHandler(Path.Combine(folderPath, "latest.log"));
        _sensitiveLogHandler = new LogHandler(Path.Combine(folderPath, "sensitive.log"));
    }

    public static void LogMessage(object? message, LogTarget target = LogTarget.Main)
        => LogInternal(message, LogLevel.Message, target, SR2ELogManager.SendMessage, _melonLogger.Msg);

    public static void LogWarning(object? message, LogTarget target = LogTarget.Main)
        => LogInternal(message, LogLevel.Warning, target, SR2ELogManager.SendWarning, _melonLogger.Warning);

    public static void LogError(object? message, LogTarget target = LogTarget.Main)
        => LogInternal(message, LogLevel.Error, target, SR2ELogManager.SendError, _melonLogger.Error);

    public static void LogDebug(object? message, LogTarget target = LogTarget.Main)
        => LogInternal(message, LogLevel.Debug, target, null, null);

    public static void LogPacketSize(object? message, LogTarget target = LogTarget.Main)
    {
        if (Main.PacketSizeLogging)
            LogInternal(message, LogLevel.Message, target, null, _melonLogger.Msg);
    }


    private static void LogInternal(object? message, LogLevel level, LogTarget target, Action<string>? sr2eAction, Action<string>? melonAction)
    {
        var msgString = message?.ToString() ?? "message was null!";
        var formattedLine = Format(msgString, level);

        if (target.HasFlag(LogTarget.Main))
            _logHandler.Write(formattedLine);

        if (target.HasFlag(LogTarget.Sensitive))
            _sensitiveLogHandler.Write(formattedLine);

        if (target == LogTarget.Sensitive)
            msgString = $"A sensitive [{level}] message was logged!";

        sr2eAction?.Invoke(msgString);
        melonAction?.Invoke(msgString);
    }

    public static void LogMessage(object? publicMsg, object? sensitiveMsg)
        => LogSplit(publicMsg, sensitiveMsg, LogLevel.Message, SR2ELogManager.SendMessage, _melonLogger.Msg);

    public static void LogWarning(object? publicMsg, object? sensitiveMsg)
        => LogSplit(publicMsg, sensitiveMsg, LogLevel.Warning, SR2ELogManager.SendWarning, _melonLogger.Warning);

    public static void LogError(object? publicMsg, object? sensitiveMsg)
        => LogSplit(publicMsg, sensitiveMsg, LogLevel.Error, SR2ELogManager.SendError, _melonLogger.Error);

    public static void LogDebug(object? publicMsg, object? sensitiveMsg)
        => LogSplit(publicMsg, sensitiveMsg, LogLevel.Debug, null, null);

    public static void LogPacketSize(object? publicMsg, object? sensitiveMsg)
    {
        if (Main.PacketSizeLogging)
            LogSplit(publicMsg, sensitiveMsg, LogLevel.Message, null, _melonLogger.Msg);
    }
    
    private static void LogSplit(object? publicMsg, object? sensitiveMsg, LogLevel level, Action<string>? sr2eAction, Action<string>? melonAction)
    {
        var publicStr = publicMsg?.ToString() ?? "public message was null!";
        var sensitiveStr = sensitiveMsg?.ToString() ?? "sensitive message was null!";

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var levelStr = level.ToString().ToUpperInvariant();

        _logHandler.Write(FormatLocal(publicStr));
        _sensitiveLogHandler.Write(FormatLocal(sensitiveStr));

        sr2eAction?.Invoke(publicStr);
        melonAction?.Invoke(publicStr);

        string FormatLocal(string msg) => msg.StartsWith('[') ? msg : $"[{timestamp}] [{levelStr}] {msg}";
    }

    private static string Format(string message, LogLevel level)
    {
        return message.StartsWith('[')
            ? message // Assumed that the message is already formatted
            : $"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpperInvariant()}] {message}";
    }
    
    private sealed class LogHandler : IDisposable
    {
        private readonly StreamWriter? _writer;
        private readonly object _lock = new();

        public LogHandler(string path)
        {
            try
            {
                _writer = new StreamWriter(path, false, Encoding.UTF8) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to initialize log file at {path}: {ex.Message}");
            }
        }

        public void Write(string line)
        {
            if (_writer == null)
                return;

            lock (_lock)
            {
                try
                {
                    _writer.WriteLine(line);
                }
                catch {}
            }
        }

        public void Dispose() => _writer?.Dispose();
    }
}