using System.Text;
using JetBrains.Annotations;
using MelonLoader;
using MelonLoader.Logging;
using MelonLoader.Utils;
using Starlight.Managers;
// ReSharper disable InconsistentNaming

namespace SR2MP;

/// <summary>
/// A utility class for handling formatted logging across file outputs, MelonLoader consoles, and Starlight management.
/// </summary>
[PublicApi]
public static class Logger
{
    private enum LogLevel : byte
    {
        Debug,
        Message,
        Warning,
        Error
    }

    /// <summary>
    /// Specifies the file destination(s) for a logged message.
    /// </summary>
    [Flags]
    public enum LogTarget : byte
    {
        /// <summary>
        /// Do not log to any file target.
        /// </summary>
        /// <remarks>Required to follow the standard but PLEASE don't use this value!</remarks>
        Neither = 0,

        /// <summary>
        /// Log to the main public log file.
        /// </summary>
        Main = 1 << 0,

        /// <summary>
        /// Log to the sensitive log file.
        /// </summary>
        Sensitive = 1 << 1,

        /// <summary>
        /// Log to both the main and sensitive log files.
        /// </summary>
        Both = Main | Sensitive
    }

    private static readonly MelonLogger.Instance _melonLogger;
    private static readonly LogHandler _logHandler;
    private static readonly LogHandler _sensitiveLogHandler;

    static Logger()
    {
        _melonLogger = new MelonLogger.Instance("Ranching Together", ColorARGB.FromArgb(77, 149, 203));

        var folderPath = Path.Combine(MelonEnvironment.UserDataDirectory, "SR2MP");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        _logHandler = new LogHandler(Path.Combine(folderPath, "latest.log"));
        _sensitiveLogHandler = new LogHandler(Path.Combine(folderPath, "sensitive.log"));
    }

    /// <summary>
    /// Logs a standard informational message.
    /// </summary>
    /// <inheritdoc cref="LogInternal"/>
    public static void LogMessage(object? message, SrLogTarget target = SrLogTarget.Both)
        => LogInternal(message, LogLevel.Message, target, StarlightLogManager.SendMessage, _melonLogger.Msg);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <inheritdoc cref="LogInternal"/>
    public static void LogWarning(object? message, SrLogTarget target = SrLogTarget.Both)
        => LogInternal(message, LogLevel.Warning, target, StarlightLogManager.SendWarning, _melonLogger.Warning);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <inheritdoc cref="LogInternal"/>
    public static void LogError(object? message, SrLogTarget target = SrLogTarget.Both)
        => LogInternal(message, LogLevel.Error, target, StarlightLogManager.SendError, _melonLogger.Error);

    /// <summary>
    /// Logs a debug message, which bypasses Starlight and MelonLoader outputs.
    /// </summary>
    /// <inheritdoc cref="LogInternal"/>
    public static void LogDebug(object? message, SrLogTarget target = SrLogTarget.Both)
        => LogInternal(message, LogLevel.Debug, target, null, null);

    /// <summary>
    /// Logs packet size information, if packet size logging is globally enabled.
    /// </summary>
    /// <inheritdoc cref="LogInternal"/>
    public static void LogPacketSize(object? message, SrLogTarget target = SrLogTarget.Both)
    {
        if (Main.PacketSizeLogging)
            LogInternal(message, LogLevel.Message, target, null, _melonLogger.Msg);
    }

    /// <summary>
    /// Logs packet acknowledgment information, if packet acknowledgment logging is globally enabled.
    /// </summary>
    /// <inheritdoc cref="LogInternal"/>
    public static void LogPacketAcknowledge(object? message, SrLogTarget target = SrLogTarget.Both)
    {
        if (Main.PacketAcknowledgeLogging)
            LogInternal(message, LogLevel.Warning, target, null, _melonLogger.Msg);
    }

    /// <summary>
    /// Backing method for logging messages of various levels.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="level">The message level.</param>
    /// <param name="target">The intended log file targets.</param>
    /// <param name="sr2EAction">The Starlight logging action.</param>
    /// <param name="melonAction">The MelonLoader logging action.</param>
    private static void LogInternal(object? message, LogLevel level, SrLogTarget target, Action<string>? sr2EAction, Action<string>? melonAction)
    {
        if (target == SrLogTarget.Neither)
            return;

        var msgString = message?.ToString() ?? "message was null!";
        var formattedLine = Format(msgString, level);

        if (target.HasFlag(SrLogTarget.Main))
            _logHandler.Write(formattedLine);

        if (target.HasFlag(SrLogTarget.Sensitive))
            _sensitiveLogHandler.Write(formattedLine);

        if (target == SrLogTarget.Sensitive)
            msgString = $"A sensitive [{level}] message was logged!";

        sr2EAction?.Invoke(msgString);
        melonAction?.Invoke(msgString);
    }

    /// <summary>
    /// Logs a standard informational message, separating public output from sensitive file output.
    /// </summary>
    /// <inheritdoc cref="LogSplit"/>
    public static void LogMessage(object? publicMsg, object? sensitiveMsg)
        => LogSplit(publicMsg, sensitiveMsg, LogLevel.Message, StarlightLogManager.SendMessage, _melonLogger.Msg);

    /// <summary>
    /// Logs a warning message, separating public output from sensitive file output.
    /// </summary>
    /// <inheritdoc cref="LogSplit"/>
    public static void LogWarning(object? publicMsg, object? sensitiveMsg)
        => LogSplit(publicMsg, sensitiveMsg, LogLevel.Warning, StarlightLogManager.SendWarning, _melonLogger.Warning);

    /// <summary>
    /// Logs an error message, separating public output from sensitive file output.
    /// </summary>
    /// <inheritdoc cref="LogSplit"/>
    public static void LogError(object? publicMsg, object? sensitiveMsg)
        => LogSplit(publicMsg, sensitiveMsg, LogLevel.Error, StarlightLogManager.SendError, _melonLogger.Error);

    /// <summary>
    /// Logs a debug message, separating public output from sensitive file output.
    /// </summary>
    /// <inheritdoc cref="LogSplit"/>
    public static void LogDebug(object? publicMsg, object? sensitiveMsg)
        => LogSplit(publicMsg, sensitiveMsg, LogLevel.Debug, null, null);

    /// <summary>
    /// Logs packet size information if globally enabled, separating public output from sensitive file output.
    /// </summary>
    /// <inheritdoc cref="LogSplit"/>
    public static void LogPacketSize(object? publicMsg, object? sensitiveMsg)
    {
        if (Main.PacketSizeLogging)
            LogSplit(publicMsg, sensitiveMsg, LogLevel.Message, null, _melonLogger.Msg);
    }

    /// <summary>
    /// Backing method for logging separate messages of separate importance.
    /// </summary>
    /// <param name="publicMsg">The message sent to public logs and MelonLoader.</param>
    /// <param name="sensitiveMsg">The detailed message sent only to the sensitive log file.</param>
    /// <param name="level">The message level.</param>
    /// <param name="sr2EAction">The Starlight logging action.</param>
    /// <param name="melonAction">The MelonLoader logging action.</param>
    private static void LogSplit(object? publicMsg, object? sensitiveMsg, LogLevel level, Action<string>? sr2EAction, Action<string>? melonAction)
    {
        var publicStr = publicMsg?.ToString() ?? "public message was null!";
        var sensitiveStr = sensitiveMsg?.ToString() ?? "sensitive message was null!";

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var levelStr = level.ToString().ToUpperInvariant();

        _logHandler.Write(FormatLocal(publicStr));
        _sensitiveLogHandler.Write(FormatLocal(sensitiveStr));

        sr2EAction?.Invoke(publicStr);
        melonAction?.Invoke(publicStr);

        string FormatLocal(string msg) => msg.StartsWith('[') ? msg : $"[{timestamp}] [{levelStr}] {msg}";
    }

    private static string Format(string message, LogLevel level) => message.StartsWith('[')
        ? message // Assumed that the message is already formatted
        : $"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpperInvariant()}] {message}";

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
                catch
                {
                    // ignored
                }
            }
        }

        public void Dispose() => _writer?.Dispose();
    }
}