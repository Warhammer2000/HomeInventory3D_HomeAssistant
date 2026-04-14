using System;
using System.Threading.Tasks;
using HomeInventory3D.Utils;
using UnityEngine;
#if HAS_SIGNALR
using Microsoft.AspNetCore.SignalR.Client;
#endif

namespace HomeInventory3D.Networking
{
    /// <summary>
    /// SignalR client for real-time inventory updates.
    /// </summary>
    public class SignalRClient : MonoBehaviour
    {
        [SerializeField] private ApiConfig apiConfig;

#if HAS_SIGNALR
        private HubConnection _connection;
        private TaskCompletionSource<bool> _connectTcs;
        private bool _isSetup;
#endif

        public event Action<ItemAddedEvent> OnItemAdded;
        public event Action<string, string, int, string> OnScanProgress;
        public event Action<string, string, int, int, int> OnScanCompleted;
        public event Action<string, string> OnItemRemoved;
        public event Action<string, string> OnScanFailed;
        public event Action<VoiceSearchResultEvent> OnVoiceSearchResult;

        public bool IsConnected =>
#if HAS_SIGNALR
            _connection?.State == HubConnectionState.Connected;
#else
            false;
#endif

        /// <summary>
        /// Connects to SignalR hub. If already connecting, waits for the result.
        /// Safe to call multiple times — will await the same connection attempt.
        /// </summary>
        public async Task ConnectAsync()
        {
#if HAS_SIGNALR
            if (apiConfig == null)
                apiConfig = ApiConfig.Instance;

            // Already connected
            if (_connection?.State == HubConnectionState.Connected)
                return;

            // Already connecting — wait for it
            if (_connectTcs != null && !_connectTcs.Task.IsCompleted)
            {
                await _connectTcs.Task;
                return;
            }

            _connectTcs = new TaskCompletionSource<bool>();

            if (!_isSetup)
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(apiConfig.HubUrl)
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                    .Build();

                // Keep connection alive indefinitely
                _connection.KeepAliveInterval = TimeSpan.FromSeconds(15);
                _connection.ServerTimeout = TimeSpan.FromSeconds(60);

                _connection.On<ItemAddedEvent>("ItemAdded", item =>
                {
                    UnityMainThread.Enqueue(() => OnItemAdded?.Invoke(item));
                });

                _connection.On<string, string, int, string>("ScanProgress", (scanId, containerId, progress, stage) =>
                {
                    UnityMainThread.Enqueue(() => OnScanProgress?.Invoke(scanId, containerId, progress, stage));
                });

                _connection.On<string, string, int, int, int>("ScanCompleted", (scanId, containerId, detected, added, removed) =>
                {
                    UnityMainThread.Enqueue(() => OnScanCompleted?.Invoke(scanId, containerId, detected, added, removed));
                });

                _connection.On<string, string>("ItemRemoved", (itemId, containerId) =>
                {
                    UnityMainThread.Enqueue(() => OnItemRemoved?.Invoke(itemId, containerId));
                });

                _connection.On<string, string>("ScanFailed", (scanId, errorMessage) =>
                {
                    UnityMainThread.Enqueue(() => OnScanFailed?.Invoke(scanId, errorMessage));
                });

                _connection.On<VoiceSearchResultEvent>("VoiceSearchResult", evt =>
                {
                    UnityMainThread.Enqueue(() => OnVoiceSearchResult?.Invoke(evt));
                });

                _connection.Reconnecting += error =>
                {
                    Debug.LogWarning($"SignalR reconnecting: {error?.Message}");
                    return Task.CompletedTask;
                };

                _connection.Reconnected += connectionId =>
                {
                    Debug.Log($"SignalR reconnected: {connectionId}");
                    return Task.CompletedTask;
                };

                _connection.Closed += error =>
                {
                    Debug.LogWarning($"SignalR connection closed: {error?.Message}");
                    return Task.CompletedTask;
                };

                _isSetup = true;
            }

            try
            {
                await _connection.StartAsync();
                Debug.Log("SignalR connected successfully");
                _connectTcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SignalR connection failed: {ex.Message}");
                _connectTcs.TrySetResult(false);
            }
#else
            Debug.LogWarning("SignalR not available.");
            await Task.CompletedTask;
#endif
        }

        public async Task JoinContainerAsync(Guid containerId)
        {
#if HAS_SIGNALR
            if (!IsConnected)
            {
                Debug.LogWarning("Cannot join container: SignalR not connected");
                return;
            }

            await _connection.InvokeAsync("JoinContainer", containerId);
            Debug.Log($"Joined container group: {containerId}");
#else
            await Task.CompletedTask;
#endif
        }

        public async Task LeaveContainerAsync(Guid containerId)
        {
#if HAS_SIGNALR
            try
            {
                if (_connection?.State == HubConnectionState.Connected)
                    await _connection.InvokeAsync("LeaveContainer", containerId);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { Debug.LogWarning($"LeaveContainer error: {ex.Message}"); }
#else
            await Task.CompletedTask;
#endif
        }

        private async void OnDestroy()
        {
#if HAS_SIGNALR
            try
            {
                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }
            }
            catch (Exception) { }
#else
            await Task.CompletedTask;
#endif
        }
    }
}
