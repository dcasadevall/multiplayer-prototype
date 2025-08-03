using ILogger = Shared.Logging.ILogger;

namespace Core.Logging
{
    /// <summary>
    /// Unity implementation of ILogger that uses Unity's Debug.Log system.
    /// 
    /// <para>
    /// This logger bridges the shared ILogger interface with Unity's logging system,
    /// providing consistent logging across both server and client while leveraging
    /// Unity's built-in logging capabilities.
    /// </para>
    /// </summary>
    public class UnityLogger : ILogger
    {
        /// <summary>
        /// Logs a debug-level message using Unity's Debug.Log.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">Optional structured arguments for formatting or context.</param>
        public void Debug(string message, params object[] args)
        {
            if (!LogSettings.ShouldLog(LogLevel.Debug))
            {
                return;
            }
            
            var formattedMessage = args != null && args.Length > 0 
                ? string.Format(message, args) 
                : message;
            UnityEngine.Debug.Log($"[DEBUG] {formattedMessage}");
        }

        /// <summary>
        /// Logs an informational message using Unity's Debug.Log.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">Optional structured arguments for formatting or context.</param>
        public void Info(string message, params object[] args)
        {
            if (!LogSettings.ShouldLog(LogLevel.Info))
            {
                return;
            }
            
            var formattedMessage = args != null && args.Length > 0 
                ? string.Format(message, args) 
                : message;
            UnityEngine.Debug.Log($"[INFO] {formattedMessage}");
        }

        /// <summary>
        /// Logs a warning message using Unity's Debug.LogWarning.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">Optional structured arguments for formatting or context.</param>
        public void Warn(string message, params object[] args)
        {
            if (!LogSettings.ShouldLog(LogLevel.Warn))
            {
                return;
            }
            
            var formattedMessage = args != null && args.Length > 0 
                ? string.Format(message, args) 
                : message;
            UnityEngine.Debug.LogWarning($"[WARN] {formattedMessage}");
        }

        /// <summary>
        /// Logs an error message using Unity's Debug.LogError.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">Optional structured arguments for formatting or context.</param>
        public void Error(string message, params object[] args)
        {
            if (!LogSettings.ShouldLog(LogLevel.Error))
            {
                return;
            }
            
            var formattedMessage = args != null && args.Length > 0 
                ? string.Format(message, args) 
                : message;
            UnityEngine.Debug.LogError($"[ERROR] {formattedMessage}");
        }

        /// <summary>
        /// Logs a fatal error message using Unity's Debug.LogError.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="args">Optional structured arguments for formatting or context.</param>
        public void Fatal(string message, params object[] args)
        {
            if (!LogSettings.ShouldLog(LogLevel.Fatal))
            {
                return;
            }
            
            var formattedMessage = args != null && args.Length > 0 
                ? string.Format(message, args) 
                : message;
            UnityEngine.Debug.LogError($"[FATAL] {formattedMessage}");
        }
    }
} 