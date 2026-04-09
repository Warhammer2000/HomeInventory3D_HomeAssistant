using System;
using System.Threading.Tasks;
using HomeInventory3D.Networking;
using UnityEngine;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Loads a full container scene from the backend and sets up SignalR subscriptions.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private ContainerManager containerManager;
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private SignalRClient signalRClient;

        private ApiClient _apiClient;
        private Guid _currentContainerId;

        /// <summary>Fired when scene loading starts.</summary>
        public event Action OnLoadStarted;

        /// <summary>Fired when scene loading completes.</summary>
        public event Action<int> OnLoadCompleted;

        /// <summary>Fired on loading error.</summary>
        public event Action<string> OnLoadError;

        private void Start()
        {
            if (ApiConfig.Instance != null)
                _apiClient = new ApiClient(ApiConfig.Instance.BackendUrl);

            if (signalRClient != null)
            {
                signalRClient.OnItemAdded += HandleItemAdded;
                signalRClient.OnItemRemoved += HandleItemRemoved;
            }
        }

        /// <summary>
        /// Loads the full scene for a container from the backend.
        /// </summary>
        public async Task LoadContainerSceneAsync(Guid containerId)
        {
            if (_apiClient == null)
            {
                OnLoadError?.Invoke("ApiClient not initialized");
                return;
            }

            OnLoadStarted?.Invoke();

            try
            {
                // Leave previous container group
                if (_currentContainerId != Guid.Empty && signalRClient != null)
                {
                    await signalRClient.LeaveContainerAsync(_currentContainerId);
                }

                containerManager.ClearItems();
                _currentContainerId = containerId;

                var scene = await _apiClient.GetSceneAsync(containerId);
                if (scene == null)
                {
                    OnLoadError?.Invoke("Scene data not found");
                    return;
                }

                containerManager.Initialize(scene.container);

                if (scene.items != null)
                {
                    foreach (var itemDto in scene.items)
                    {
                        await itemSpawner.SpawnItemAsync(itemDto, animate: true);
                    }
                }

                // Join SignalR group for real-time updates
                if (signalRClient != null)
                {
                    if (!signalRClient.IsConnected)
                    {
                        Debug.Log("Waiting for SignalR connection...");
                        await signalRClient.ConnectAsync();
                    }

                    if (signalRClient.IsConnected)
                    {
                        await signalRClient.JoinContainerAsync(containerId);
                        Debug.Log($"Joined container group: {containerId}");
                    }
                    else
                    {
                        Debug.LogWarning("SignalR still not connected — real-time updates unavailable");
                    }
                }

                var itemCount = scene.items?.Length ?? 0;
                OnLoadCompleted?.Invoke(itemCount);
                Debug.Log($"Scene loaded: {scene.container.name} with {itemCount} items");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load scene: {ex.Message}");
                OnLoadError?.Invoke(ex.Message);
            }
        }

        private async void HandleItemAdded(ItemAddedEvent evt)
        {
            Debug.Log($"SignalR ItemAdded received: {evt.name} for container {evt.containerId} (watching: {_currentContainerId})");

            if (evt.containerId != _currentContainerId.ToString())
            {
                Debug.LogWarning($"Ignoring ItemAdded — container mismatch: {evt.containerId} != {_currentContainerId}");
                return;
            }

            await itemSpawner.SpawnItemAsync(evt);
            Debug.Log($"Item spawned via SignalR: {evt.name}");
        }

        private void HandleItemRemoved(string itemId, string containerId)
        {
            if (containerId != _currentContainerId.ToString())
                return;

            containerManager.RemoveItem(itemId);
        }

        private async void OnDestroy()
        {
            if (signalRClient != null)
            {
                signalRClient.OnItemAdded -= HandleItemAdded;
                signalRClient.OnItemRemoved -= HandleItemRemoved;

                if (_currentContainerId != Guid.Empty)
                {
                    await signalRClient.LeaveContainerAsync(_currentContainerId);
                }
            }
        }
    }
}
