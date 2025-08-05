using System;
using System.Collections.Generic;
using System.Linq;
using Adapters;
using Core.ECS.Replication;
using Microsoft.Extensions.DependencyInjection;
using Shared.ECS.Replication;
using Shared.Networking;
using UnityEngine;
using ILogger = Shared.Logging.ILogger;

namespace Core.Networking
{
    /// <summary>
    /// Advanced network debugging UI that shows detailed network statistics,
    /// ping history, and packet information.
    /// </summary>
    public class NetworkDebugUI : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.F3;
        [SerializeField] private bool _showOnStart = true;
        [SerializeField] private Vector2 _windowPosition = new Vector2(10, 10);
        [SerializeField] private Vector2 _windowSize = new Vector2(350, 250);
        
        [Header("Graph Settings")]
        [SerializeField] private int _historySize = 100;
        [SerializeField] private Color _pingGraphColor = Color.green;
        [SerializeField] private Color _upsGraphColor = Color.blue;
        
        private IClientConnection _clientConnection;
        private IMessageReceiver _messageReceiver;
        private ILogger _logger;
        private IReplicationStats _replicationStats;
        
        // Network stats
        private readonly Queue<float> _pingHistory = new();
        private readonly Queue<float> _upsHistory = new();
        private readonly Queue<float> _updateTimestamps = new();
        private readonly Queue<TimeSpan> _replicationIntervals = new();
        
        private IDisposable _snapshotHandler;
        private bool _showDebugUI;
        private float _lastStatsUpdate;
        private const float STATS_UPDATE_INTERVAL = 0.5f;
        
        // Current stats
        private int _currentPing;
        private float _currentUPS;
        private int _totalPacketsReceived;
        private DateTime _connectionStartTime;

        private void Start()
        {
            var serviceProvider = FindObjectOfType<RootServiceProvider>()?.ServiceProvider;
            _clientConnection = serviceProvider?.GetRequiredService<IClientConnection>();
            _messageReceiver = serviceProvider?.GetRequiredService<IMessageReceiver>();
            _logger = serviceProvider?.GetRequiredService<ILogger>();
            _replicationStats = serviceProvider?.GetRequiredService<IReplicationStats>();
            
            _showDebugUI = _showOnStart;
            _connectionStartTime = DateTime.Now;
            
            RegisterMessageHandlers();
            _logger.Info("Network Debug UI initialized");
        }
        
        private void Update()
        {
            HandleInput();
            UpdateStats();
        }
        
        private void OnGUI()
        {
            if (!_showDebugUI)
                return;
                
            DrawDebugWindow();
        }
        
        private void HandleInput()
        {
            if (UnityEngine.Input.GetKeyDown(_toggleKey))
            {
                ToggleDebugUI();
            }
        }
        
        private void RegisterMessageHandlers()
        {
            _snapshotHandler = _messageReceiver.RegisterMessageHandler<WorldSnapshotMessage>(
                "NetworkDebugUI",
                OnWorldSnapshotReceived);
        }
        
        private void OnWorldSnapshotReceived(int peerId, WorldSnapshotMessage message)
        {
            _totalPacketsReceived++;
            _updateTimestamps.Enqueue(Time.time);
            
            // Trim old timestamps
            while (_updateTimestamps.Count > _historySize)
            {
                _updateTimestamps.Dequeue();
            }
        }
        
        private void UpdateStats()
        {
            if (Time.time - _lastStatsUpdate < STATS_UPDATE_INTERVAL)
                return;
                
            _lastStatsUpdate = Time.time;
            
            // Update current stats
            _currentPing = _clientConnection?.PingMs ?? -1;
            _currentUPS = CalculateUpdatesPerSecond();
            
            // Add to history
            _pingHistory.Enqueue(_currentPing);
            _upsHistory.Enqueue(_currentUPS);
            _replicationIntervals.Enqueue(_replicationStats.TimeBetweenSnapshots);
            
            // Trim history
            while (_pingHistory.Count > _historySize)
                _pingHistory.Dequeue();
            while (_upsHistory.Count > _historySize)
                _upsHistory.Dequeue();
            while (_replicationIntervals.Count > _historySize)
                _replicationIntervals.Dequeue();
        }
        
        private float CalculateUpdatesPerSecond()
        {
            if (_updateTimestamps.Count < 2)
                return 0f;
                
            var timestamps = _updateTimestamps.ToArray();
            var timeSpan = timestamps.Last() - timestamps.First();
            
            return timeSpan > 0 ? (timestamps.Length - 1) / timeSpan : 0f;
        }
        
        private void DrawDebugWindow()
        {
            var windowRect = new Rect(_windowPosition, _windowSize);
            GUI.Window(0, windowRect, DrawWindowContent, "Network Debug");
        }
        
        private void DrawWindowContent(int windowID)
        {
            GUILayout.BeginVertical();
            
            // Connection info
            DrawConnectionInfo();
            
            GUILayout.Space(10);
            
            // Current stats
            DrawCurrentStats();
            
            GUILayout.Space(10);
            
            // Mini graphs
            DrawMiniGraphs();
            
            GUILayout.Space(10);
            
            // Controls
            DrawControls();
            
            GUILayout.EndVertical();
            
            // Make window draggable
            GUI.DragWindow();
        }
        
        private void DrawConnectionInfo()
        {
            GUILayout.Label("Connection Info:");
            
            var peerId = _clientConnection?.AssignedPeerId ?? -1;
            var uptime = DateTime.Now - _connectionStartTime;
            
            GUILayout.Label($"Peer ID: {peerId}");
            GUILayout.Label($"Uptime: {uptime:hh\\:mm\\:ss}");
            GUILayout.Label($"Ping: {_currentPing} ms");
            GUILayout.Label($"Updates/sec: {_currentUPS:F1}");
            GUILayout.Label($"Replication Interval: {_replicationStats?.TimeBetweenSnapshots.TotalMilliseconds ?? 0} ms");
            GUILayout.Label($"Total Packets: {_totalPacketsReceived}");
        }
        
        private void DrawCurrentStats()
        {
            GUILayout.Label("Current Stats:");
            
            var pingText = _currentPing >= 0 ? $"{_currentPing}ms" : "N/A";
            var pingColor = GetPingColor(_currentPing);
            
            GUI.color = pingColor;
            GUILayout.Label($"Ping: {pingText}");
            GUI.color = Color.white;
            
            var upsColor = GetUPSColor(_currentUPS);
            GUI.color = upsColor;
            GUILayout.Label($"Updates/sec: {_currentUPS:F1}");
            GUI.color = Color.white;
            
            if (_replicationStats != null)
            {
                var intervalColor = GetReplicationIntervalColor(_replicationStats.TimeBetweenSnapshots);
                GUI.color = intervalColor;
                GUILayout.Label($"Replication Interval: {_replicationStats.TimeBetweenSnapshots.TotalMilliseconds} ms");
                GUI.color = Color.white;
            }
        }
        
        private void DrawMiniGraphs()
        {
            GUILayout.Label("History:");
            
            // Ping graph
            if (_pingHistory.Count > 1)
            {
                GUILayout.Label("Ping (ms):");
                DrawMiniGraph(_pingHistory.ToArray(), _pingGraphColor, 0, 200);
            }
            
            // UPS graph
            if (_upsHistory.Count > 1)
            {
                GUILayout.Label("Updates/sec:");
                DrawMiniGraph(_upsHistory.ToArray(), _upsGraphColor, 0, 60);
            }
            
            // Replication interval graph
            if (_replicationIntervals.Count > 1)
            {
                GUILayout.Label("Replication Interval (ms):");
                var intervals = _replicationIntervals.Select(i => (float)i.TotalMilliseconds).ToArray();
                var minInterval = intervals.Min();
                var maxInterval = intervals.Max();
                DrawMiniGraph(intervals, GetReplicationIntervalColor(_replicationStats.TimeBetweenSnapshots), minInterval, maxInterval);
            }
        }
        
        private void DrawMiniGraph(float[] values, Color color, float minY, float maxY)
        {
            var rect = GUILayoutUtility.GetRect(300, 40);
            
            GUI.color = Color.black;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            
            if (values.Length < 2)
                return;
                
            var stepX = rect.width / (values.Length - 1);
            var rangeY = maxY - minY;
            
            for (int i = 0; i < values.Length - 1; i++)
            {
                var x1 = rect.x + i * stepX;
                var y1 = rect.y + rect.height - ((values[i] - minY) / rangeY) * rect.height;
                var x2 = rect.x + (i + 1) * stepX;
                var y2 = rect.y + rect.height - ((values[i + 1] - minY) / rangeY) * rect.height;
                
                DrawLine(new Vector2(x1, y1), new Vector2(x2, y2));
            }
            
            GUI.color = Color.white;
        }
        
        private void DrawLine(Vector2 start, Vector2 end)
        {
            var thickness = 2f;
            var angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            var length = Vector2.Distance(start, end);
            
            var rect = new Rect(start.x, start.y - thickness / 2, length, thickness);
            
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUIUtility.RotateAroundPivot(-angle, start);
        }
        
        private void DrawControls()
        {
            GUILayout.Label("Controls:");
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Stats"))
            {
                ResetStats();
            }
            if (GUILayout.Button("Close"))
            {
                _showDebugUI = false;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Label($"Press {_toggleKey} to toggle");
        }
        
        private Color GetPingColor(int ping)
        {
            if (ping < 0) return Color.gray;
            if (ping < 50) return Color.green;
            if (ping < 100) return Color.yellow;
            if (ping < 200) return Color.orange;
            return Color.red;
        }
        
        private Color GetUPSColor(float ups)
        {
            if (ups >= 20) return Color.green;
            if (ups >= 10) return Color.yellow;
            if (ups >= 5) return Color.orange;
            return Color.red;
        }
        
        private Color GetReplicationIntervalColor(TimeSpan interval)
        {
            if (interval.TotalMilliseconds < 50) return Color.green;
            if (interval.TotalMilliseconds < 100) return Color.yellow;
            if (interval.TotalMilliseconds < 200) return Color.orange;
            return Color.red;
        }
        
        #region Public API
        
        public void ToggleDebugUI()
        {
            _showDebugUI = !_showDebugUI;
            _logger.Info("Network debug UI {0}", _showDebugUI ? "enabled" : "disabled");
        }
        
        public void ResetStats()
        {
            _pingHistory.Clear();
            _upsHistory.Clear();
            _updateTimestamps.Clear();
            _totalPacketsReceived = 0;
            _connectionStartTime = DateTime.Now;
            _logger.Info("Network debug stats reset");
        }
        
        public bool IsVisible => _showDebugUI;
        
        #endregion
        
        private void OnDestroy()
        {
            _snapshotHandler?.Dispose();
        }
    }
}