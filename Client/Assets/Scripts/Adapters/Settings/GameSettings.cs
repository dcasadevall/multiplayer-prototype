using Shared.Settings;
using UnityEngine;

namespace Settings
{
    /// <summary>
    /// Game settings contains settings that are not sent by the server.
    /// Namely network settings.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Network Settings")]
        public NetworkSettings NetworkSettings;
    }
}
