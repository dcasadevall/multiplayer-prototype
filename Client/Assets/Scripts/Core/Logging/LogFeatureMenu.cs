#if UNITY_EDITOR
using System;
using Shared.Logging;
using UnityEditor;

namespace Core.Logging
{
    [InitializeOnLoad]
    public static class LogFeatureMenu
    {
        private const string MenuPath = "Debug/Log Features/";

        static LogFeatureMenu()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            LogSettings.LoadFeatureSettings();
        }

        [MenuItem(MenuPath + "Enable All", false, 200)]
        private static void EnableAllFeatures() => SetAllFeatures(true);

        [MenuItem(MenuPath + "Disable All", false, 201)]
        private static void DisableAllFeatures() => SetAllFeatures(false);

        [MenuItem(MenuPath + "---", false, 202)]
        private static void Separator() { }

        // Automatically generated menu items for each feature
        [MenuItem(MenuPath + "General", false, 203)] private static void ToggleGeneral() => ToggleFeature(LoggedFeature.General);
        [MenuItem(MenuPath + "General", true)] private static bool ValidateGeneral() => ValidateFeature(LoggedFeature.General);

        [MenuItem(MenuPath + "Networking", false, 204)] private static void ToggleNetworking() => ToggleFeature(LoggedFeature.Networking);
        [MenuItem(MenuPath + "Networking", true)] private static bool ValidateNetworking() => ValidateFeature(LoggedFeature.Networking);
        
        [MenuItem(MenuPath + "ECS", false, 205)] private static void ToggleECS() => ToggleFeature(LoggedFeature.Ecs);
        [MenuItem(MenuPath + "ECS", true)] private static bool ValidateECS() => ValidateFeature(LoggedFeature.Ecs);

        [MenuItem(MenuPath + "Game", false, 206)] private static void ToggleGame() => ToggleFeature(LoggedFeature.Game);
        [MenuItem(MenuPath + "Game", true)] private static bool ValidateGame() => ValidateFeature(LoggedFeature.Game);
        
        [MenuItem(MenuPath + "Input", false, 207)] private static void ToggleInput() => ToggleFeature(LoggedFeature.Input);
        [MenuItem(MenuPath + "Input", true)] private static bool ValidateInput() => ValidateFeature(LoggedFeature.Input);

        [MenuItem(MenuPath + "Replication", false, 208)] private static void ToggleReplication() => ToggleFeature(LoggedFeature.Replication);
        [MenuItem(MenuPath + "Replication", true)] private static bool ValidateReplication() => ValidateFeature(LoggedFeature.Replication);
        
        [MenuItem(MenuPath + "Simulation", false, 208)] private static void ToggleSimulation() => ToggleFeature(LoggedFeature.Simulation);
        [MenuItem(MenuPath + "Simulation", true)] private static bool ValidateSimulation() => ValidateFeature(LoggedFeature.Simulation);

        [MenuItem(MenuPath + "Prediction", false, 209)] private static void TogglePrediction() => ToggleFeature(LoggedFeature.Prediction);
        [MenuItem(MenuPath + "Prediction", true)] private static bool ValidatePrediction() => ValidateFeature(LoggedFeature.Prediction);
        
        [MenuItem(MenuPath + "UI", false, 210)] private static void ToggleUI() => ToggleFeature(LoggedFeature.Ui);
        [MenuItem(MenuPath + "UI", true)] private static bool ValidateUI() => ValidateFeature(LoggedFeature.Ui);

        [MenuItem(MenuPath + "Initialization", false, 211)] private static void ToggleInitialization() => ToggleFeature(LoggedFeature.Initialization);
        [MenuItem(MenuPath + "Initialization", true)] private static bool ValidateInitialization() => ValidateFeature(LoggedFeature.Initialization);
        
        [MenuItem(MenuPath + "Scheduling", false, 212)] private static void ToggleScheduling() => ToggleFeature(LoggedFeature.Scheduling);
        [MenuItem(MenuPath + "Scheduling", true)] private static bool ValidateScheduling() => ValidateFeature(LoggedFeature.Scheduling);


        private static void ToggleFeature(LoggedFeature feature)
        {
            LogSettings.SetFeatureEnabled(feature, !LogSettings.IsFeatureEnabled(feature));
        }
        
        private static bool ValidateFeature(LoggedFeature feature)
        {
            Menu.SetChecked(MenuPath + feature.ToString(), LogSettings.IsFeatureEnabled(feature));
            return true;
        }

        private static void SetAllFeatures(bool enabled)
        {
            foreach (LoggedFeature feature in Enum.GetValues(typeof(LoggedFeature)))
            {
                LogSettings.SetFeatureEnabled(feature, enabled);
            }
        }
    }
}
#endif