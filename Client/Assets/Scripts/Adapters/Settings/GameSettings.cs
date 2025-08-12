using UnityEngine;

namespace Adapters.Settings
{
    /// <summary>
    /// Game settings contains settings that are not sent by the server.
    /// Namely network settings.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Settings/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [SerializeField]
        private NetworkClientSettings _networkSettings;
        public NetworkClientSettings NetworkSettings => _networkSettings;
    }
}
