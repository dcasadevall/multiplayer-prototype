#if UNITY_EDITOR
using UnityEditor;

namespace Core.Logging
{
    /// <summary>
    /// Provides a Unity editor menu for controlling log levels.
    /// Uses EditorPrefs to persist the selected level between editor sessions.
    /// </summary>
    public static class LogLevelMenu
    {
        private const string MenuPath = "Game/Log Level/";
        private const string EditorPrefKey = "LogLevel";
        
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Load saved editor preference when Unity starts
            LogSettings.MinimumLogLevel = (LogLevel)EditorPrefs.GetInt(EditorPrefKey, (int)LogLevel.Info);
        }
        
        [MenuItem(MenuPath + "Debug", false, 100)]
        private static void SetDebugLevel()
        {
            SetAndSaveLogLevel(LogLevel.Debug);
        }
        
        [MenuItem(MenuPath + "Debug", true)]
        private static bool ValidateDebugLevel()
        {
            Menu.SetChecked(MenuPath + "Debug", LogSettings.MinimumLogLevel == LogLevel.Debug);
            return true;
        }
        
        [MenuItem(MenuPath + "Info", false, 101)]
        private static void SetInfoLevel()
        {
            SetAndSaveLogLevel(LogLevel.Info);
        }
        
        [MenuItem(MenuPath + "Info", true)]
        private static bool ValidateInfoLevel()
        {
            Menu.SetChecked(MenuPath + "Info", LogSettings.MinimumLogLevel == LogLevel.Info);
            return true;
        }
        
        [MenuItem(MenuPath + "Warning", false, 102)]
        private static void SetWarnLevel()
        {
            SetAndSaveLogLevel(LogLevel.Warn);
        }
        
        [MenuItem(MenuPath + "Warning", true)]
        private static bool ValidateWarnLevel()
        {
            Menu.SetChecked(MenuPath + "Warning", LogSettings.MinimumLogLevel == LogLevel.Warn);
            return true;
        }
        
        [MenuItem(MenuPath + "Error", false, 103)]
        private static void SetErrorLevel()
        {
            SetAndSaveLogLevel(LogLevel.Error);
        }
        
        [MenuItem(MenuPath + "Error", true)]
        private static bool ValidateErrorLevel()
        {
            Menu.SetChecked(MenuPath + "Error", LogSettings.MinimumLogLevel == LogLevel.Error);
            return true;
        }
        
        [MenuItem(MenuPath + "Fatal", false, 104)]
        private static void SetFatalLevel()
        {
            SetAndSaveLogLevel(LogLevel.Fatal);
        }
        
        [MenuItem(MenuPath + "Fatal", true)]
        private static bool ValidateFatalLevel()
        {
            Menu.SetChecked(MenuPath + "Fatal", LogSettings.MinimumLogLevel == LogLevel.Fatal);
            return true;
        }
        
        private static void SetAndSaveLogLevel(LogLevel level)
        {
            LogSettings.MinimumLogLevel = level;
            EditorPrefs.SetInt(EditorPrefKey, (int)level);
        }
    }
}
#endif