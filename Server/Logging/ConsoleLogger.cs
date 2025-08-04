using Shared.Logging;

namespace Server.Logging
{
    public class ConsoleLogger(LoggingSettings settings) : ILogger
    {
        private readonly LoggingSettings _settings = settings;
        private static readonly Dictionary<LoggedFeature, ConsoleColor> _featureColors = new();

        private static readonly ConsoleColor[] _predefinedColors =
        [
            ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.Yellow, ConsoleColor.Magenta,
            ConsoleColor.Blue, ConsoleColor.Red, ConsoleColor.White, ConsoleColor.DarkCyan,
            ConsoleColor.DarkGreen, ConsoleColor.DarkYellow
        ];

        static ConsoleLogger()
        {
            foreach (LoggedFeature feature in Enum.GetValues(typeof(LoggedFeature)))
            {
                int colorIndex = (int)feature % _predefinedColors.Length;
                _featureColors[feature] = _predefinedColors[colorIndex];
            }
        }

        public void Debug(string message, params object[]? args) => Debug(LoggedFeature.General, message, args);
        public void Info(string message, params object[]? args) => Info(LoggedFeature.General, message, args);
        public void Warn(string message, params object[]? args) => Warn(LoggedFeature.General, message, args);
        public void Error(string message, params object[]? args) => Error(LoggedFeature.General, message, args);
        public void Fatal(string message, params object[]? args) => Fatal(LoggedFeature.General, message, args);

        public void Debug(LoggedFeature feature, string message, params object[]? args)
        {
            WriteLog("DEBUG", ConsoleColor.Gray, feature, message, args);
        }

        public void Info(LoggedFeature feature, string message, params object[]? args)
        {
            WriteLog("INFO", ConsoleColor.White, feature, message, args);
        }

        public void Warn(LoggedFeature feature, string message, params object[]? args)
        {
            WriteLog("WARN", ConsoleColor.Yellow, feature, message, args);
        }

        public void Error(LoggedFeature feature, string message, params object[]? args)
        {
            WriteLog("ERROR", ConsoleColor.Red, feature, message, args);
        }

        public void Fatal(LoggedFeature feature, string message, params object[]? args)
        {
            WriteLog("FATAL", ConsoleColor.Magenta, feature, message, args);
        }

        private void WriteLog(string level, ConsoleColor levelColor, LoggedFeature feature, string message, params object[]? args)
        {
            // The check is now done here, based on the config file
            if ((int)_settings.MinimumLogLevel > (int)GetLogLevelFromString(level) ||
                !_settings.Features.GetValueOrDefault(feature, true))
            {
                return;
            }

            var originalColor = Console.ForegroundColor;

            Console.Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [");

            Console.ForegroundColor = _featureColors[feature];
            Console.Write($"{feature.ToString().ToUpper()}");

            Console.ForegroundColor = originalColor;
            Console.Write("] [");

            Console.ForegroundColor = levelColor;
            Console.Write(level);

            Console.ForegroundColor = originalColor;
            Console.Write("] ");

            var formatted = args is { Length: > 0 } ? string.Format(message, args) : message;
            Console.WriteLine(formatted);

            Console.ForegroundColor = originalColor;
        }

        private LogLevel GetLogLevelFromString(string level) => level switch
        {
            "DEBUG" => LogLevel.Debug,
            "INFO" => LogLevel.Info,
            "WARN" => LogLevel.Warn,
            "ERROR" => LogLevel.Error,
            "FATAL" => LogLevel.Fatal,
            _ => LogLevel.Debug
        };
    }
}