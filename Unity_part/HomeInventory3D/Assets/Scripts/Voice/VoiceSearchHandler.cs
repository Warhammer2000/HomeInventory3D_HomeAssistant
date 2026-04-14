using System;
using System.Collections;
using HomeInventory3D.Animation;
using HomeInventory3D.Networking;
using HomeInventory3D.Scene;
using HomeInventory3D.UI;
using UnityEngine;

namespace HomeInventory3D.Voice
{
    /// <summary>
    /// Handles voice search results from SignalR: navigates camera to item and highlights it.
    /// </summary>
    public class VoiceSearchHandler : MonoBehaviour
    {
        [SerializeField] private SignalRClient signalRClient;
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private ContainerManager containerManager;
        [SerializeField] private OrbitCamera orbitCamera;
        [SerializeField] private ToastNotification toast;
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private float highlightDuration = 8f;

        private HighlightAnimation _currentHighlight;

        private void Start()
        {
            if (signalRClient != null)
            {
                signalRClient.OnVoiceSearchResult += HandleVoiceSearchResult;
            }
        }

        private void HandleVoiceSearchResult(VoiceSearchResultEvent evt)
        {
            Debug.Log($"Voice search: \"{evt.itemName}\" in \"{evt.containerName}\" — {evt.answer}");

            // Show toast with answer
            toast?.Show(evt.answer);

            // Stop previous highlight
            StopCurrentHighlight();

            // Navigate to item
            StartCoroutine(NavigateToItemCoroutine(evt));
        }

        private IEnumerator NavigateToItemCoroutine(VoiceSearchResultEvent evt)
        {
            var containerId = evt.containerId;

            // Check if we need to load a different container
            if (containerManager.ContainerId != containerId)
            {
                Debug.Log($"Voice: loading container {containerId}...");
                var loadTask = sceneLoader.LoadContainerSceneAsync(Guid.Parse(containerId));

                // Wait for scene to load
                while (!loadTask.IsCompleted)
                    yield return null;

                // Wait a frame for items to spawn
                yield return new WaitForSeconds(0.5f);
            }

            // Find the item in scene
            if (containerManager.SpawnedItems.TryGetValue(evt.itemId, out var itemController))
            {
                // Fly camera to item
                var itemPos = itemController.transform.position;
                orbitCamera.FlyTo(itemPos, 0.5f);

                Debug.Log($"Voice: camera flying to {evt.itemName} at {itemPos}");

                // Wait for camera to arrive
                yield return new WaitForSeconds(1.5f);

                // Highlight item
                var highlight = itemController.GetComponent<HighlightAnimation>();
                if (highlight == null)
                    highlight = itemController.gameObject.AddComponent<HighlightAnimation>();

                highlight.StartHighlight(highlightMaterial);
                _currentHighlight = highlight;

                // Auto-stop highlight after duration
                yield return new WaitForSeconds(highlightDuration);
                StopCurrentHighlight();
            }
            else
            {
                Debug.LogWarning($"Voice: item {evt.itemId} not found in spawned items");
                toast?.Show($"Предмет «{evt.itemName}» не найден в текущей сцене");
            }
        }

        private void StopCurrentHighlight()
        {
            if (_currentHighlight != null)
            {
                _currentHighlight.StopHighlight();
                _currentHighlight = null;
            }
        }

        private void OnDestroy()
        {
            if (signalRClient != null)
            {
                signalRClient.OnVoiceSearchResult -= HandleVoiceSearchResult;
            }
        }
    }
}
