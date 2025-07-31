using Shared.Logging;

namespace Server.Logging;

/// <summary>
/// A simple <see cref="ILogger"/> implementation that writes structured log messages to the console.
/// Supports log levels and message formatting.
/// </summary>
public class ConsoleLogger : ILogger
{
    /// <inheritdoc />
    public void Debug(string message, params object[]? args)
    {
        WriteLog("DEBUG", ConsoleColor.Gray, message, args);
    }

    /// <inheritdoc />
    public void Info(string message, params object[]? args)
    {
        WriteLog("INFO", ConsoleColor.White, message, args);
    }

    /// <inheritdoc />
    public void Warn(string message, params object[]? args)
    {
        WriteLog("WARN", ConsoleColor.Yellow, message, args);
    }

    /// <inheritdoc />
    public void Error(string message, params object[]? args)
    {
        WriteLog("ERROR", ConsoleColor.Red, message, args);
    }

    /// <inheritdoc />
    public void Fatal(string message, params object[]? args)
    {
        WriteLog("FATAL", ConsoleColor.Magenta, message, args);
    }

    /// <summary>
    /// Writes a formatted log message to the console with the specified level and color.
    /// </summary>
    /// <param name="level">The log level label.</param>
    /// <param name="color">The console color for the message.</param>
    /// <param name="message">The log message format string.</param>
    /// <param name="args">Optional arguments for formatting.</param>
    private void WriteLog(string level, ConsoleColor color, string message, params object[]? args)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        var formatted = args != null && args.Length > 0 ? string.Format(message, args) : message;
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {formatted}");
        Console.ForegroundColor = previousColor;
    }
}