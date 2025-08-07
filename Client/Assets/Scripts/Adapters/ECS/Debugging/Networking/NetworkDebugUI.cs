using System;
using System.Collections.Generic;
using System.Linq;
using Adapters;
using Core.ECS.Replication;
using Microsoft.Extensions.DependencyInjection;
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
        [SerializeField] private bool _showOnStart = false;
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
        private readonly Queue<TimeSpan> _replicationIntervals = new();
        
        private IDisposable _snapshotHandler;
        private bool _showDebugUI;
        private float _lastStatsUpdate;
        private const float StatsUpdateInterval = 0.5f;
        
        // Current stats
        private int _currentPing;
        private int _totalPacketsReceived;
        private DateTime _connectionStartTime;

        private void Start()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var serviceProvider = FindObjectOfType<RootServiceProvider>()?.ServiceProvider;
#pragma warning restore CS0618 // Type or member is obsolete
            _clientConnection = serviceProvider?.GetRequiredService<IClientConnection>();
            _messageReceiver = serviceProvider?.GetRequiredService<IMessageReceiver>();
            _logger = serviceProvider?.GetRequiredService<ILogger>();
            _replicationStats = serviceProvider?.GetRequiredService<IReplicationStats>();
            
            _showDebugUI = _showOnStart;
            _connectionStartTime = DateTime.Now;
            
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
        
        private void UpdateStats()
        {
            if (Time.time - _lastStatsUpdate < StatsUpdateInterval)
                return;
                
            _lastStatsUpdate = Time.time;
            
            // Update current stats
            _currentPing = _clientConnection?.PingMs ?? -1;
            
            // Add to history
            _pingHistory.Enqueue(_currentPing);
            _replicationIntervals.Enqueue(_replicationStats.TimeBetweenSnapshots);
            
            // Trim history
            while (_pingHistory.Count > _historySize)
                _pingHistory.Dequeue();
            while (_replicationIntervals.Count > _historySize)
                _replicationIntervals.Dequeue();
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
            var peerId = _clientConnection?.AssignedPeerId ?? -1;
            GUILayout.Label($"Peer ID: {peerId}");
        }
        
        private void DrawCurrentStats()
        {
            var pingText = _currentPing >= 0 ? $"{_currentPing}ms" : "N/A";
            var pingColor = GetPingColor(_currentPing);
            
            GUI.color = pingColor;
            GUILayout.Label($"Ping: {pingText}");
            GUI.color = Color.white;
            
            if (_replicationStats != null)
            {
                var intervalColor = GetReplicationIntervalColor(_replicationStats.TimeBetweenSnapshots);
                GUI.color = intervalColor;
                GUILayout.Label($"Replication Interval: {_replicationStats.TimeBetweenSnapshots.TotalMilliseconds} ms");
                GUI.color = Color.white;
            }
            
            GUILayout.Label($"Total Packets: {_totalPacketsReceived}");
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