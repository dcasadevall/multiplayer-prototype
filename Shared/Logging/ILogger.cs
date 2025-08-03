namespace Shared.Logging
{
    /// <summary>
    /// Provides a structured logging interface for application and system diagnostics.
    /// 
    /// <para>
    /// Implementations should support structured log messages with severity levels and
    /// optional contextual data. This enables consistent, filterable, and machine-readable
    /// logging across the application.
    /// </para>
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a debug-level message, typically used for development and troubleshooting.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">Optional structured arguments for formatting or context.</param>
        void Debug(string message, params object[]? args);

        /// <summary>
        /// Logs an informational message, used for general application flow and state.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">Optional structured arguments for formatting or context.</param>
        void Info(string message, params object[]? args);

        /// <summary>
        /// Logs a warning message, indicating a potential issue or unexpected situation.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">Optional structured arguments for formatting or context.</param>
        void Warn(string message, params object[]? args);

        /// <summary>
        /// Logs an error message, indicating a failure or critical problem.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">Optional structured arguments for formatting or context.</param>
        void Error(string message, params object[]? args);

        /// <summary>
        /// Logs a fatal error message, indicating an unrecoverable application or system failure.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">Optional structured arguments for formatting or context.</param>
        void Fatal(string message, params object[]? args);
    }
}