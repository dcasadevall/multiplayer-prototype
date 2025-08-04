using Shared.Logging;

namespace Core.Logging
{
    public class UnityLogger : ILogger
    {
        public void Debug(string message, params object[] args) => Debug(LoggedFeature.General, message, args);
        public void Info(string message, params object[] args) => Info(LoggedFeature.General, message, args);
        public void Warn(string message, params object[] args) => Warn(LoggedFeature.General, message, args);
        public void Error(string message, params object[] args) => Error(LoggedFeature.General, message, args);
        public void Fatal(string message, params object[] args) => Fatal(LoggedFeature.General, message, args);
        
        public void Debug(LoggedFeature feature, string message, params object[] args)
        {
            if (!LogSettings.ShouldLog(LogLevel.Debug, feature)) return;
            Log(feature, LogLevel.Debug, message, args);
        }

        public void Info(LoggedFeature feature, string message, params object[] args)
        {
            if (!LogSettings.ShouldLog(LogLevel.Info, feature)) return;
            Log(feature, LogLevel.Info, message, args);
        }

        public void Warn(LoggedFeature feature, string message, params object[] args)
        {
            if (!LogSettings.ShouldLog(LogLevel.Warn, feature)) return;
            Log(feature, LogLevel.Warn, message, args);
        }

        public void Error(LoggedFeature feature, string message, params object[] args)
        {
            if (!LogSettings.ShouldLog(LogLevel.Error, feature)) return;
            Log(feature, LogLevel.Error, message, args);
        }

        public void Fatal(LoggedFeature feature, string message, params object[] args)
        {
            if (!LogSettings.ShouldLog(LogLevel.Fatal, feature)) return;
            Log(feature, LogLevel.Fatal, message, args);
        }

        private void Log(LoggedFeature feature, LogLevel level, string message, params object[] args)
        {
            var formattedMessage = args is { Length: > 0 }
                ? string.Format(message, args) 
                : message;

            var featureColorHex = LogSettings.GetFeatureColorHex(feature);
            var logString = $"[<color=#{featureColorHex}>{feature}</color>] [{level}] {formattedMessage}";

            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(logString);
                    break;
                case LogLevel.Warn:
                    UnityEngine.Debug.LogWarning(logString);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    UnityEngine.Debug.LogError(logString);
                    break;
            }
        }
    }
}