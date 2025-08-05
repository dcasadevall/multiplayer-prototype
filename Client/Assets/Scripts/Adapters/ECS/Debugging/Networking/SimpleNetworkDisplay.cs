using System;
using System.Collections.Generic;
using System.Linq;
using Shared.ECS.Replication;
using Shared.Networking;
using UnityEngine;
using UnityEngine.UI;
using ILogger = Shared.Logging.ILogger;

namespace Core.Networking
{
    /// <summary>
    /// Simple network stats display using Unity UI.
    /// Shows ping and updates per second in a clean, minimal interface.
    /// </summary>
    public class SimpleNetworkDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text _pingText;
        [SerializeField] private Text _upsText;
        [SerializeField] private GameObject _networkPanel;
        
        [Header("Settings")]
        [SerializeField] private bool _showOnStart = true;
        [SerializeField] private float _updateInterval = 1f;
        [SerializeField] private int _sampleSize = 30;
        [SerializeField] private string _pingFormat = "Ping: {0}ms";
        [SerializeField] private string _upsFormat = "UPS: {0:F1}";
        
        [Header("Color Coding")]
        [SerializeField] private Color _goodPingColor = Color.green;
        [SerializeField] private Color _mediumPingColor = Color.yellow;
        [SerializeField] private Color _badPingColor = Color.red;
        [SerializeField] private int _goodPingThreshold = 50;
        [SerializeField] private int _badPingThreshold = 150;
        
        private IClientConnection _clientConnection;
        private IMessageReceiver _messageReceiver;
        private ILogger _logger;
        
        private readonly Queue<float> _updateTimestamps = new();
        private IDisposable _snapshotHandler;
        private float _lastUpdate;
        private bool _isInitialized;

        public void Initialize(IClientConnection clientConnection, IMessageReceiver messageReceiver, ILogger logger)
        {
            _clientConnection = clientConnection;
            _messageReceiver = messageReceiver;
            _logger = logger;
            
            RegisterMessageHandlers();
            _isInitialized = true;
            
            if (_networkPanel != null)
                _networkPanel.SetActive(_showOnStart);
                
            _logger.Info("Simple Network Display initialized");
        }
        
        private void Update()
        {
            if (!_isInitialized || !IsDisplayActive())
                return;
                
            if (Time.time - _lastUpdate >= _updateInterval)
            {
                UpdateDisplay();
                _lastUpdate = Time.time;
            }
        }
        
        private void RegisterMessageHandlers()
        {
            _snapshotHandler = _messageReceiver.RegisterMessageHandler<WorldSnapshotMessage>(
                "SimpleNetworkDisplay",
                OnWorldSnapshotReceived);
        }
        
        private void OnWorldSnapshotReceived(int peerId, WorldSnapshotMessage message)
        {
            _updateTimestamps.Enqueue(Time.time);
            
            // Keep only recent samples
            while (_updateTimestamps.Count > _sampleSize)
            {
                _updateTimestamps.Dequeue();
            }
        }
        
        private void UpdateDisplay()
        {
            UpdatePingDisplay();
            UpdateUPSDisplay();
        }
        
        private void UpdatePingDisplay()
        {
            if (_pingText == null)
                return;
                
            var ping = _clientConnection?.PingMs ?? -1;
            
            if (ping >= 0)
            {
                _pingText.text = string.Format(_pingFormat, ping);
                _pingText.color = GetPingColor(ping);
            }
            else
            {
                _pingText.text = string.Format(_pingFormat, "N/A");
                _pingText.color = Color.gray;
            }
        }
        
        private void UpdateUPSDisplay()
        {
            if (_upsText == null)
                return;
                
            var ups = CalculateUpdatesPerSecond();
            _upsText.text = string.Format(_upsFormat, ups);
            _upsText.color = GetUPSColor(ups);
        }
        
        private float CalculateUpdatesPerSecond()
        {
            if (_updateTimestamps.Count < 2)
                return 0f;
                
            var timestamps = _updateTimestamps.ToArray();
            var timeSpan = timestamps.Last() - timestamps.First();
            
            return timeSpan > 0 ? (timestamps.Length - 1) / timeSpan : 0f;
        }
        
        private Color GetPingColor(int ping)
        {
            if (ping <= _goodPingThreshold)
                return _goodPingColor;
            else if (ping <= _badPingThreshold)
                return _mediumPingColor;
            else
                return _badPingColor;
        }
        
        private Color GetUPSColor(float ups)
        {
            // Good UPS is typically 20-30+ for smooth gameplay
            if (ups >= 20f)
                return _goodPingColor;
            else if (ups >= 10f)
                return _mediumPingColor;
            else
                return _badPingColor;
        }
        
        private bool IsDisplayActive()
        {
            return _networkPanel == null || _networkPanel.activeInHierarchy;
        }
        
        #region Public API
        
        /// <summary>
        /// Shows or hides the network display.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_networkPanel != null)
            {
                _networkPanel.SetActive(visible);
                _logger.Info("Network display {0}", visible ? "shown" : "hidden");
            }
        }
        
        /// <summary>
        /// Toggles the visibility of the network display.
        /// </summary>
        public void ToggleVisibility()
        {
            if (_networkPanel != null)
            {
                SetVisible(!_networkPanel.activeInHierarchy);
            }
        }
        
        /// <summary>
        /// Gets the current ping in milliseconds.
        /// </summary>
        public int CurrentPing => _clientConnection?.PingMs ?? -1;
        
        /// <summary>
        /// Gets the current updates per second.
        /// </summary>
        public float CurrentUPS => CalculateUpdatesPerSecond();
        
        /// <summary>
        /// Resets the UPS calculation by clearing the sample history.
        /// </summary>
        public void ResetUPSCalculation()
        {
            _updateTimestamps.Clear();
            _logger.Info("UPS calculation reset");
        }
        
        #endregion
        
        #region Unity Inspector Methods
        
        [ContextMenu("Toggle Display")]
        private void ToggleDisplayMenu()
        {
            ToggleVisibility();
        }
        
        [ContextMenu("Reset UPS")]
        private void ResetUPSMenu()
        {
            ResetUPSCalculation();
        }
        
        [ContextMenu("Log Current Stats")]
        private void LogCurrentStatsMenu()
        {
            _logger.Info("Current Network Stats - Ping: {0}ms, UPS: {1:F1}", CurrentPing, CurrentUPS);
        }
        
        #endregion
        
        private void OnDestroy()
        {
            _snapshotHandler?.Dispose();
        }
    }
}