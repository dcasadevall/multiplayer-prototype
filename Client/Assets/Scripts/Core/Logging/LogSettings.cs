using System;
using System.Collections.Generic;
using Shared.Logging;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Core.Logging
{
    /// <summary>
    /// Stores and manages log level and feature settings for the Unity client.
    /// </summary>
    public static class LogSettings
    {
        public static LogLevel MinimumLogLevel { get; set; } = LogLevel.Info;

        private static readonly Dictionary<LoggedFeature, bool> _featureStates = new();
        private static readonly Dictionary<LoggedFeature, Color> _featureColors = new();

        private static readonly Color[] _predefinedColors = {
            new Color(0.27f, 0.63f, 0.96f), // Blue
            new Color(0.96f, 0.48f, 0.27f), // Orange
            new Color(0.27f, 0.96f, 0.42f), // Green
            new Color(0.96f, 0.82f, 0.27f), // Yellow
            new Color(0.69f, 0.42f, 0.96f), // Purple
            new Color(0.96f, 0.42f, 0.69f), // Pink
            new Color(0.42f, 0.96f, 0.82f), // Teal
            new Color(0.96f, 0.27f, 0.27f), // Red
            new Color(0.8f, 0.8f, 0.8f),    // Light Grey
            new Color(0.5f, 0.7f, 0.5f)     // Muted Green
        };
        
        public static bool ShouldLog(LogLevel level)
        {
            return level >= MinimumLogLevel;
        }

        public static bool ShouldLog(LogLevel level, LoggedFeature feature)
        {
            return level >= MinimumLogLevel && IsFeatureEnabled(feature);
        }

        public static bool IsFeatureEnabled(LoggedFeature feature)
        {
            // Default to true if not found, to avoid suppressing logs unexpectedly.
            return !_featureStates.TryGetValue(feature, out var isEnabled) || isEnabled;
        }

        public static string GetFeatureColorHex(LoggedFeature feature)
        {
            if (_featureColors.TryGetValue(feature, out Color color))
            {
                return ColorUtility.ToHtmlStringRGB(color);
            }
            return "FFFFFF"; // Default to white
        }

#if UNITY_EDITOR
        internal static void SetFeatureEnabled(LoggedFeature feature, bool isEnabled)
        {
            _featureStates[feature] = isEnabled;
            EditorPrefs.SetBool($"LogFeature_{feature}", isEnabled);
        }
        
        internal static void LoadFeatureSettings()
        {
            foreach (LoggedFeature feature in Enum.GetValues(typeof(LoggedFeature)))
            {
                // Default all to true
                _featureStates[feature] = EditorPrefs.GetBool($"LogFeature_{feature}", true);
                
                if (!_featureColors.ContainsKey(feature))
                {
                    int colorIndex = (int)feature % _predefinedColors.Length;
                    _featureColors[feature] = _predefinedColors[colorIndex];
                }
            }
        }
#else
        // In builds, we can't use EditorPrefs. Let's just enable all features.
        // A more robust solution might use a config file.
        static LogSettings()
        {
            foreach (LoggedFeature feature in Enum.GetValues(typeof(LoggedFeature)))
            {
                _featureStates[feature] = true;
                int colorIndex = (int)feature % _predefinedColors.Length;
                _featureColors[feature] = _predefinedColors[colorIndex];
            }
        }
#endif
    }
}