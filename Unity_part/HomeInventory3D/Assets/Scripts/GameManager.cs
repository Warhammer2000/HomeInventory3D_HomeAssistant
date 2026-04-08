using System;
using HomeInventory3D.Networking;
using HomeInventory3D.Scene;
using HomeInventory3D.UI;
using UnityEngine;

namespace HomeInventory3D
{
    /// <summary>
    /// Main game manager. Wires together networking, scene, and UI components.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Networking")]
        [SerializeField] private SignalRClient signalRClient;

        [Header("Scene")]
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private ContainerManager containerManager;
        [SerializeField] private ItemSpawner itemSpawner;

        [Header("UI")]
        [SerializeField] private ToastNotification toast;
        [SerializeField] private ProgressOverlay progressOverlay;
        [SerializeField] private ItemInfoCard itemInfoCard;

        [Header("Config")]
        [SerializeField] private string defaultContainerId;

        private void Start()
        {
            // Wire up SignalR events to UI
            if (signalRClient != null)
            {
                signalRClient.OnItemAdded += HandleItemAdded;
                signalRClient.OnScanProgress += HandleScanProgress;
                signalRClient.OnScanCompleted += HandleScanCompleted;
                signalRClient.OnScanFailed += HandleScanFailed;
            }

            // Wire up scene events
            if (sceneLoader != null)
            {
                sceneLoader.OnLoadStarted += () => progressOverlay?.Show("Loading scene...");
                sceneLoader.OnLoadCompleted += count =>
                {
                    progressOverlay?.Hide();
                    toast?.Show($"Scene loaded: {count} items");
                };
                sceneLoader.OnLoadError += error =>
                {
                    progressOverlay?.Hide();
                    toast?.Show($"Error: {error}");
                };
            }

            // Load default container if specified
            if (!string.IsNullOrEmpty(defaultContainerId) &&
                Guid.TryParse(defaultContainerId, out var containerId))
            {
                LoadContainer(containerId);
            }
        }

        /// <summary>
        /// Loads a container scene by ID.
        /// </summary>
        public async void LoadContainer(Guid containerId)
        {
            if (sceneLoader != null)
                await sceneLoader.LoadContainerSceneAsync(containerId);
        }

        private void HandleItemAdded(ItemAddedEvent evt)
        {
            toast?.Show($"{evt.name} added!");
        }

        private void HandleScanProgress(string scanId, string containerId, int progress, string stage)
        {
            progressOverlay?.Show(stage);
            progressOverlay?.UpdateProgress(progress, stage);
        }

        private void HandleScanCompleted(string scanId, string containerId, int detected, int added, int removed)
        {
            progressOverlay?.Hide();
            toast?.Show($"Scan complete: +{added} items, -{removed} removed");
        }

        private void HandleScanFailed(string scanId, string errorMessage)
        {
            progressOverlay?.Hide();
            toast?.Show($"Scan failed: {errorMessage}");
        }

        private void OnDestroy()
        {
            if (signalRClient != null)
            {
                signalRClient.OnItemAdded -= HandleItemAdded;
                signalRClient.OnScanProgress -= HandleScanProgress;
                signalRClient.OnScanCompleted -= HandleScanCompleted;
                signalRClient.OnScanFailed -= HandleScanFailed;
            }
        }
    }
}
