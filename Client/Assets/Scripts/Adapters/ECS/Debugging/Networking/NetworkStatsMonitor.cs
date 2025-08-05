using System;
using System.Collections.Generic;
using System.Linq;
using Shared.ECS.Replication;
using Shared.Networking;
using UnityEngine;
using ILogger = Shared.Logging.ILogger;

namespace Core.Networking
{
    /// <summary>
    /// Monitors and displays network statistics including ping and updates per second.
    /// Tracks world replication messages to calculate server update rate.
    /// </summary>
    public class NetworkStatsMonitor : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool _showNetworkStats = true;
        [SerializeField] private Vector2 _displayPosition = new Vector2(10, 10);
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private int _fontSize = 16;
        
        [Header("Calculation Settings")]
        [SerializeField] private float _updateInterval = 1f; // How often to update the display
        [SerializeField] private int _sampleWindow = 60; // Number of samples to keep for averaging
        
        private IClientConnection _clientConnection;
        private IMessageReceiver _messageReceiver;
        private ILogger _logger;
        
        // Stats tracking
        private readonly Queue<float> _updateTimestamps = new();
        private float _lastDisplayUpdate;
        private float _currentUpdatesPerSecond;
        private int _currentPingMs;
        private IDisposable _snapshotHandler;
        
        // GUI Style
        private GUIStyle _guiStyle;
        private bool _isInitialized;

        public void Initialize(IClientConnection clientConnection, 
            IMessageReceiver messageReceiver, 
            Shared.Logging.ILogger logger)
        {
            _clientConnection = clientConnection;
            _messageReceiver = messageReceiver;
            _logger = logger;
            
            RegisterMessageHandlers();
            _logger.Info("Network Stats Monitor initialized");
        }
        
        private void Awake()
        {
            SetupGUIStyle();
        }
        
        private void Start()
        {
            _lastDisplayUpdate = Time.time;
        }
        
        private void Update()
        {
            if (!_showNetworkStats || !_isInitialized)
                return;
                
            // Update display at regular intervals
            if (Time.time - _lastDisplayUpdate >= _updateInterval)
            {
                UpdateNetworkStats();
                _lastDisplayUpdate = Time.time;
            }
        }
        
        private void OnGUI()
        {
            if (!_showNetworkStats || !_isInitialized)
                return;
                
            DrawNetworkStats();
        }
        
        private void RegisterMessageHandlers()
        {
            _snapshotHandler = _messageReceiver.RegisterMessageHandler<WorldSnapshotMessage>(
                "NetworkStatsMonitor",
                OnWorldSnapshotReceived);
            _isInitialized = true;
        }
        
        private void OnWorldSnapshotReceived(int peerId, WorldSnapshotMessage message)
        {
            // Record timestamp for updates per second calculation
            _updateTimestamps.Enqueue(Time.time);
            
            // Remove old samples outside the window
            while (_updateTimestamps.Count > _sampleWindow)
            {
                _updateTimestamps.Dequeue();
            }
        }
        
        private void UpdateNetworkStats()
        {
            // Update ping from client connection
            _currentPingMs = _clientConnection?.PingMs ?? -1;
            
            // Calculate updates per second
            _currentUpdatesPerSecond = CalculateUpdatesPerSecond();
        }
        
        private float CalculateUpdatesPerSecond()
        {
            if (_updateTimestamps.Count < 2)
                return 0f;
                
            var timestamps = _updateTimestamps.ToArray();
            var timeSpan = timestamps.Last() - timestamps.First();
            
            if (timeSpan <= 0f)
                return 0f;
                
            // Calculate updates per second over the sample window
            return (timestamps.Length - 1) / timeSpan;
        }
        
        private void SetupGUIStyle()
        {
            _guiStyle = new GUIStyle();
            _guiStyle.fontSize = _fontSize;
            _guiStyle.normal.textColor = _textColor;
            _guiStyle.fontStyle = FontStyle.Bold;
            _guiStyle.alignment = TextAnchor.UpperLeft;
        }
        
        private void DrawNetworkStats()
        {
            var rect = new Rect(_displayPosition.x, _displayPosition.y, 300, 100);
            
            var statsText = BuildStatsText();
            GUI.Label(rect, statsText, _guiStyle);
        }
        
        private string BuildStatsText()
        {
            var pingText = _currentPingMs >= 0 ? $"{_currentPingMs}ms" : "N/A";
            var upsText = $"{_currentUpdatesPerSecond:F1}";
            
            return $"Network Stats:\n" +
                   $"Ping: {pingText}\n" +
                   $"Updates/sec: {upsText}\n" +
                   $"Samples: {_updateTimestamps.Count}";
        }
        
        #region Public API
        
        /// <summary>
        /// Toggles the network stats display on/off.
        /// </summary>
        public void ToggleDisplay()
        {
            _showNetworkStats = !_showNetworkStats;
            _logger.Info("Network stats display {0}", _showNetworkStats ? "enabled" : "disabled");
        }
        
        /// <summary>
        /// Gets the current ping in milliseconds.
        /// </summary>
        public int CurrentPingMs => _currentPingMs;
        
        /// <summary>
        /// Gets the current updates per second from the server.
        /// </summary>
        public float CurrentUpdatesPerSecond => _currentUpdatesPerSecond;
        
        /// <summary>
        /// Resets the statistics tracking.
        /// </summary>
        public void ResetStats()
        {
            _updateTimestamps.Clear();
            _currentUpdatesPerSecond = 0f;
            _logger.Info("Network stats reset");
        }
        
        #endregion
        
        #region Unity Inspector Controls
        
        [ContextMenu("Toggle Network Stats")]
        private void ToggleNetworkStatsMenu()
        {
            ToggleDisplay();
        }
        
        [ContextMenu("Reset Network Stats")]
        private void ResetNetworkStatsMenu()
        {
            ResetStats();
        }
        
        [ContextMenu("Log Current Stats")]
        private void LogCurrentStats()
        {
            _logger.Info("Current Network Stats - Ping: {0}ms, Updates/sec: {1:F1}", 
                _currentPingMs, _currentUpdatesPerSecond);
        }
        
        #endregion
        
        private void OnDestroy()
        {
            _snapshotHandler?.Dispose();
        }
    }
}