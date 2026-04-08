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
    /// Requires SignalR NuGet package. Define HAS_SIGNALR scripting symbol after installing.
    /// </summary>
    public class SignalRClient : MonoBehaviour
    {
        [SerializeField] private ApiConfig apiConfig;

#if HAS_SIGNALR
        private HubConnection _connection;
#endif

        /// <summary>Fired when a new item is added during scan processing.</summary>
        public event Action<ItemAddedEvent> OnItemAdded;

        /// <summary>Fired on scan progress updates.</summary>
        public event Action<string, string, int, string> OnScanProgress;

        /// <summary>Fired when scan completes.</summary>
        public event Action<string, string, int, int, int> OnScanCompleted;

        /// <summary>Fired when an item is removed.</summary>
        public event Action<string, string> OnItemRemoved;

        /// <summary>Fired when a scan fails.</summary>
        public event Action<string, string> OnScanFailed;

        /// <summary>Whether the connection is active.</summary>
        public bool IsConnected =>
#if HAS_SIGNALR
            _connection?.State == HubConnectionState.Connected;
#else
            false;
#endif

        private async void Start()
        {
            if (apiConfig == null)
                apiConfig = ApiConfig.Instance;

            await ConnectAsync();
        }

        /// <summary>
        /// Establishes the SignalR connection and registers event handlers.
        /// </summary>
        public async Task ConnectAsync()
        {
#if HAS_SIGNALR
            if (_connection != null)
                return;

            _connection = new HubConnectionBuilder()
                .WithUrl(apiConfig.HubUrl)
                .WithAutomaticReconnect()
                .Build();

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

            try
            {
                await _connection.StartAsync();
                Debug.Log("SignalR connected successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SignalR connection failed: {ex.Message}");
            }
#else
            Debug.LogWarning("SignalR not available. Install Microsoft.AspNetCore.SignalR.Client via NuGet and add HAS_SIGNALR to Scripting Define Symbols.");
            await Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Joins a container's real-time update group.
        /// </summary>
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

        /// <summary>
        /// Leaves a container's real-time update group.
        /// </summary>
        public async Task LeaveContainerAsync(Guid containerId)
        {
#if HAS_SIGNALR
            if (!IsConnected) return;

            await _connection.InvokeAsync("LeaveContainer", containerId);
            Debug.Log($"Left container group: {containerId}");
#else
            await Task.CompletedTask;
#endif
        }

        private async void OnDestroy()
        {
#if HAS_SIGNALR
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
#else
            await Task.CompletedTask;
#endif
        }
    }
}
