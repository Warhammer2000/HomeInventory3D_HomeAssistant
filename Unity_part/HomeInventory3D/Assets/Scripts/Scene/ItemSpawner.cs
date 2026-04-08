using System;
using System.Threading;
using System.Threading.Tasks;
using GLTFast;
using HomeInventory3D.Animation;
using HomeInventory3D.Networking;
using UnityEngine;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Spawns 3D objects from backend data with GLTFast mesh loading and spawn animation.
    /// </summary>
    public class ItemSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject genericItemPrefab;
        [SerializeField] private Material spawnGlowMaterial;
        [SerializeField] private ContainerManager containerManager;

        private ApiClient _apiClient;

        private void Start()
        {
            if (ApiConfig.Instance != null)
                _apiClient = new ApiClient(ApiConfig.Instance.BackendUrl);
        }

        /// <summary>
        /// Spawns an item from a backend ItemDto (scene loading).
        /// </summary>
        public async Task<ItemController> SpawnItemAsync(ItemDto dto, bool animate = false)
        {
            var position = containerManager.RelativeToWorldPosition(
                dto.positionX, dto.positionY, dto.positionZ);

            var rotation = Quaternion.Euler(
                dto.rotationX ?? 0f,
                dto.rotationY ?? 0f,
                dto.rotationZ ?? 0f);

            var obj = Instantiate(genericItemPrefab, position, rotation, containerManager.ItemsParent);

            var controller = obj.GetComponent<ItemController>();
            if (controller == null)
                controller = obj.AddComponent<ItemController>();

            controller.Initialize(dto);
            containerManager.RegisterItem(controller);

            if (!string.IsNullOrEmpty(dto.meshFilePath))
            {
                await TryLoadGltfMeshAsync(obj, dto.meshFilePath);
            }

            SetBoundingBoxScale(obj, dto);

            if (animate)
            {
                SpawnAnimation.Play(obj, spawnGlowMaterial);
            }

            return controller;
        }

        /// <summary>
        /// Spawns an item from a SignalR event (real-time, always animated).
        /// </summary>
        public async Task<ItemController> SpawnItemAsync(ItemAddedEvent evt)
        {
            var position = containerManager.RelativeToWorldPosition(
                evt.positionX, evt.positionY, evt.positionZ);

            var rotation = Quaternion.Euler(
                evt.rotationX ?? 0f,
                evt.rotationY ?? 0f,
                evt.rotationZ ?? 0f);

            var obj = Instantiate(genericItemPrefab, position, rotation, containerManager.ItemsParent);
            obj.transform.localScale = Vector3.zero;

            var controller = obj.GetComponent<ItemController>();
            if (controller == null)
                controller = obj.AddComponent<ItemController>();

            controller.Initialize(evt);
            containerManager.RegisterItem(controller);

            if (!string.IsNullOrEmpty(evt.meshUrl))
            {
                await TryLoadGltfMeshAsync(obj, evt.meshUrl);
            }

            SetBoundingBoxScale(obj, evt);

            SpawnAnimation.Play(obj, spawnGlowMaterial);

            return controller;
        }

        private async Task TryLoadGltfMeshAsync(GameObject target, string meshUrl)
        {
            if (_apiClient == null) return;

            try
            {
                var url = meshUrl;
                if (!url.StartsWith("http"))
                    url = $"{_apiClient.BaseUrl}/{url.TrimStart('/')}";

                var gltf = new GltfImport();
                var success = await gltf.Load(url);

                if (success)
                {
                    // Remove placeholder visuals
                    var existingRenderer = target.GetComponentInChildren<MeshRenderer>();
                    if (existingRenderer != null)
                        Destroy(existingRenderer.gameObject != target
                            ? existingRenderer.gameObject
                            : existingRenderer);

                    var existingFilter = target.GetComponentInChildren<MeshFilter>();
                    if (existingFilter != null && existingFilter.gameObject == target)
                        Destroy(existingFilter);

                    await gltf.InstantiateMainSceneAsync(target.transform);
                    Debug.Log($"GLB loaded: {meshUrl}");
                }
                else
                {
                    Debug.LogWarning($"Failed to load GLB: {meshUrl}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GLB load error for {meshUrl}: {ex.Message}");
            }
        }

        private static void SetBoundingBoxScale(GameObject obj, ItemDto dto)
        {
            if (!dto.bboxMaxX.HasValue || !dto.bboxMinX.HasValue) return;

            var size = new Vector3(
                (dto.bboxMaxX.Value - dto.bboxMinX.Value),
                (dto.bboxMaxY ?? 0f) - (dto.bboxMinY ?? 0f),
                (dto.bboxMaxZ ?? 0f) - (dto.bboxMinZ ?? 0f));

            if (size.magnitude > 0.01f)
                obj.transform.localScale = size;
        }

        private static void SetBoundingBoxScale(GameObject obj, ItemAddedEvent evt)
        {
            if (!evt.bboxMaxX.HasValue || !evt.bboxMinX.HasValue) return;

            var size = new Vector3(
                (evt.bboxMaxX.Value - evt.bboxMinX.Value),
                (evt.bboxMaxY ?? 0f) - (evt.bboxMinY ?? 0f),
                (evt.bboxMaxZ ?? 0f) - (evt.bboxMinZ ?? 0f));

            if (size.magnitude > 0.01f)
                obj.transform.localScale = size;
        }
    }
}
