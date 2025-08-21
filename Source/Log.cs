using BepInEx.Logging;
using System.Reflection;

namespace God;

/// <summary>
/// Plugin-specific logger.
/// </summary>
internal static class Log {
    /// <summary>
    /// String to prefix each log message with.
    /// </summary>
    private static string LogPrefix => $"[{Assembly.GetExecutingAssembly().GetName().Version}] ";
    
    /// <summary>
    /// The BepInEx logging source.
    /// </summary>
    private static ManualLogSource? _logSource;

    /// <summary>
    /// Initialize the logger.
    /// </summary>
    /// <param name="logSource">The BepInEx logger source.</param>
    internal static void Init(ManualLogSource logSource) {
        _logSource = logSource;
    }

    /// <summary>
    /// Log a debug message.
    /// </summary>
    /// <param name="debug">The debug message to log.</param>
    /// <returns></returns>
    internal static void Debug(object debug) => _logSource?.LogDebug(LogPrefix + debug);

    /// <summary>
    /// Log an error message.
    /// </summary>
    /// <param name="error">The error to log.</param>
    internal static void Error(object error) => _logSource?.LogError(LogPrefix + error);

    /// <summary>
    /// Log an error message.
    /// </summary>
    /// <param name="fatal">The fatal message to log.</param>
    internal static void Fatal(object fatal) => _logSource?.LogFatal(LogPrefix + fatal);

    /// <summary>
    /// Log an info message.
    /// </summary>
    /// <param name="info">The info message to log.</param>
    internal static void Info(object info) => _logSource?.LogInfo(LogPrefix + info);

    /// <summary>
    /// Log a message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    internal static void Message(object message) => _logSource?.LogMessage(LogPrefix + message);

    /// <summary>
    /// Log a warning.
    /// </summary>
    /// <param name="warning">The warning to log.</param>
    internal static void Warning(object warning) => _logSource?.LogWarning(LogPrefix + warning);
}