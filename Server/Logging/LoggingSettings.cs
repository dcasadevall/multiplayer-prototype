using Server.Logging;
using Shared.Logging;

namespace Server.Logging
{
    public class LoggingSettings
    {
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Info;
        public Dictionary<LoggedFeature, bool> Features { get; set; } = new();
    }
}