namespace Shared.Logging
{
    /// <summary>
    /// Defines the feature categories for logging.
    /// This allows for selective filtering of logs in the editor.
    /// </summary>
    public enum LoggedFeature
    {
        General,
        Networking,
        Ecs,
        Game,
        Input,
        Replication,
        Simulation,
        Prediction,
        Ui,
        Initialization,
        Scheduling
    }
}