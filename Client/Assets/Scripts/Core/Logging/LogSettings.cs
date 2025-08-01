using UnityEngine;

namespace Core.Logging
{
    /// <summary>
    /// Stores and manages log level settings for the Unity client.
    /// </summary>
    public static class LogSettings
    {
        // Simple static field for runtime log level - no need to persist between plays
        private static LogLevel _currentLogLevel = LogLevel.Info;
        
        /// <summary>
        /// Gets or sets the current minimum log level.
        /// Messages below this level will not be logged.
        /// </summary>
        public static LogLevel MinimumLogLevel
        {
            get => _currentLogLevel;
            set => _currentLogLevel = value;
        }
        
        /// <summary>
        /// Thread-safe method to check if a given log level should be logged.
        /// </summary>
        public static bool ShouldLog(LogLevel level)
        {
            return level >= _currentLogLevel;
        }
    }
}