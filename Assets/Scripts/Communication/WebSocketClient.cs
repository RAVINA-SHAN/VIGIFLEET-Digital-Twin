using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VigiFleet.Telemetry;

namespace VigiFleet.Communication
{
    /// <summary>
    /// High-performance WebSocket client connecting to the VIGIFLEET FastAPI backend
    /// at ws://localhost:8000/ws/telemetry. Supports automatic reconnect and main-thread dispatching.
    /// </summary>
    public class WebSocketClient : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string serverUrl = "ws://localhost:8000/ws/telemetry";
        [SerializeField] private bool connectOnStart = true;
        [SerializeField] private float reconnectIntervalS = 3.0f;
        [SerializeField] private float pingIntervalS = 10.0f;

        [Header("Status (Read Only)")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private long totalPacketsReceived = 0;
        [SerializeField] private string lastError = null;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<string> _incomingMessageQueue = new ConcurrentQueue<string>();
        private float _lastPingTime = 0f;
        private float _lastReconnectAttempt = 0f;

        public bool IsConnected => isConnected;
        public long TotalPacketsReceived => totalPacketsReceived;
        public string ServerUrl { get => serverUrl; set => serverUrl = value; }

        public event Action<bool> OnConnectionStatusChanged;
        public event Action<string> OnRawMessageReceived;
        public event Action<ESP32TelemetryPacket> OnTelemetryPacketReceived;

        private void Start()
        {
            if (connectOnStart)
            {
                Connect();
            }
        }

        private void Update()
        {
            // Process queued messages on Unity's main thread
            while (_incomingMessageQueue.TryDequeue(out string message))
            {
                ProcessMessageOnMainThread(message);
            }

            // Periodic keep-alive ping
            if (isConnected && (Time.time - _lastPingTime) > pingIntervalS)
            {
                _lastPingTime = Time.time;
                _ = SendPingAsync();
            }

            // Auto-reconnect if disconnected
            if (!isConnected && (Time.time - _lastReconnectAttempt) > reconnectIntervalS)
            {
                _lastReconnectAttempt = Time.time;
                Connect();
            }
        }

        public void Connect()
        {
            if (isConnected) return;
            _ = ConnectAsync();
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            if (_webSocket != null)
            {
                try
                {
                    _webSocket.Abort();
                    _webSocket.Dispose();
                }
                catch { }
                _webSocket = null;
            }
            SetConnectedState(false);
        }

        private async Task ConnectAsync()
        {
            try
            {
                Disconnect();

                _cts = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();

                Uri uri = new Uri(serverUrl);
                await _webSocket.ConnectAsync(uri, _cts.Token);

                if (_webSocket.State == WebSocketState.Open)
                {
                    SetConnectedState(true);
                    _ = ReceiveLoopAsync(_cts.Token);
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                SetConnectedState(false);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            StringBuilder sb = new StringBuilder();

            try
            {
                while (!token.IsCancellationRequested && _webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        string fullMsg = sb.ToString();
                        sb.Clear();
                        _incomingMessageQueue.Enqueue(fullMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
            finally
            {
                SetConnectedState(false);
            }
        }

        private async Task SendPingAsync()
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                try
                {
                    byte[] bytes = Encoding.UTF8.GetBytes("ping");
                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch { }
            }
        }

        private void SetConnectedState(bool state)
        {
            if (isConnected != state)
            {
                isConnected = state;
                OnConnectionStatusChanged?.Invoke(state);
            }
        }

        private void ProcessMessageOnMainThread(string json)
        {
            totalPacketsReceived++;
            OnRawMessageReceived?.Invoke(json);

            // Attempt to parse vehicle packet
            try
            {
                if (json.Contains("\"vehicle_id\""))
                {
                    ESP32TelemetryPacket pkt = ESP32TelemetryPacket.FromJson(json);
                    if (pkt != null && !string.IsNullOrEmpty(pkt.vehicle_id))
                    {
                        OnTelemetryPacketReceived?.Invoke(pkt);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VigiFleet WS] Message parse error: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }
}
