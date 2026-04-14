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
        [SerializeField] private float spawnHeight = 0.18f;
        [SerializeField] private float itemScale = 0.09f;

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
            var obj = Instantiate(genericItemPrefab, containerManager.ItemsParent);
            obj.transform.localPosition = new Vector3(0, spawnHeight, 0);
            obj.transform.localRotation = Quaternion.identity;

            var controller = obj.GetComponent<ItemController>();
            if (controller == null)
                controller = obj.AddComponent<ItemController>();

            controller.Initialize(dto);
            containerManager.RegisterItem(controller);

            if (!string.IsNullOrEmpty(dto.meshFilePath))
            {
                await TryLoadGltfMeshAsync(obj, dto.meshFilePath);
            }

            // AddPhysics normalizes size + adds MeshCollider + Rigidbody
            AddPhysics(obj, null);

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
            var obj = Instantiate(genericItemPrefab, containerManager.ItemsParent);
            obj.transform.localPosition = new Vector3(0, spawnHeight, 0);
            obj.transform.localRotation = Quaternion.identity;
            var controller = obj.GetComponent<ItemController>();
            if (controller == null)
                controller = obj.AddComponent<ItemController>();

            controller.Initialize(evt);
            containerManager.RegisterItem(controller);

            if (!string.IsNullOrEmpty(evt.meshUrl))
            {
                await TryLoadGltfMeshAsync(obj, evt.meshUrl);
            }

            // AddPhysics normalizes size + adds MeshCollider + Rigidbody
            AddPhysics(obj, evt);
            SpawnAnimation.Play(obj, spawnGlowMaterial);

            return controller;
        }

        /// <summary>
        /// Normalizes model size then adds MeshCollider + Rigidbody with Claude-predicted physics.
        /// </summary>
        private void AddPhysics(GameObject obj, ItemAddedEvent evt)
        {
            var mass = evt?.massKg ?? 0.15f;
            var realSizeCm = evt?.realSizeCm ?? 10f;
            var bounciness = evt?.bounciness ?? 0.2f;
            var friction = evt?.friction ?? 0.5f;
            var materialType = evt?.materialType ?? "plastic";
            var isFragile = evt?.isFragile ?? false;

            // Step 1: Normalize model to real-world size (cm → meters)
            NormalizeModelSize(obj, realSizeCm / 100f);

            // Step 1.5: Remove any existing colliders from prefab (e.g. BoxCollider from GenericItem)
            foreach (var existingCol in obj.GetComponentsInChildren<Collider>())
                UnityEngine.Object.Destroy(existingCol);

            // Step 2: Add MeshCollider (convex) for accurate shape — on every child mesh
            var meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length > 0)
            {
                foreach (var mf in meshFilters)
                {
                    if (mf.sharedMesh == null) continue;
                    if (mf.GetComponent<Collider>() != null) continue;

                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.convex = true; // required for Rigidbody
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
            else if (obj.GetComponentInChildren<Collider>() == null)
            {
                // Fallback — no mesh, add box
                obj.AddComponent<BoxCollider>();
            }

            // Step 3: Physics Material
            var physicMat = new PhysicsMaterial($"PhysMat_{obj.name}")
            {
                bounciness = bounciness,
                dynamicFriction = friction,
                staticFriction = friction * 1.2f,
                bounceCombine = PhysicsMaterialCombine.Average,
                frictionCombine = PhysicsMaterialCombine.Average
            };

            foreach (var col in obj.GetComponentsInChildren<Collider>())
                col.material = physicMat;

            // Step 4: Rigidbody
            var rb = obj.GetComponent<Rigidbody>();
            if (rb == null) rb = obj.AddComponent<Rigidbody>();

            rb.mass = mass;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Material-based drag
            var (drag, angDrag) = materialType?.ToLowerInvariant() switch
            {
                "metal" => (1.5f, 2f),
                "wood" => (2f, 3f),
                "glass" or "ceramic" => (1f, 1.5f),
                "rubber" => (3f, 4f),
                "fabric" or "paper" => (4f, 5f),
                "electronics" => (2f, 3f),
                _ => (2f, 3f)
            };
            rb.linearDamping = drag;
            rb.angularDamping = angDrag;

            rb.collisionDetectionMode = isFragile
                ? CollisionDetectionMode.ContinuousDynamic
                : CollisionDetectionMode.Discrete;

            Debug.Log($"Physics: {obj.name} — {mass}kg, {materialType}, bounce={bounciness}, friction={friction}");
        }

        /// <summary>
        /// Scales the model so its largest dimension matches the real-world size in meters.
        /// Claude provides real_size_cm (e.g. mug=10cm, keyboard=45cm), we convert to meters.
        /// </summary>
        private static void NormalizeModelSize(GameObject target, float targetSizeMeters)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest < 0.001f) return;

            var scaleFactor = targetSizeMeters / largest;
            target.transform.localScale *= scaleFactor;

            Debug.Log($"Normalized: {target.name} {largest:F3}m → {targetSizeMeters:F3}m ({targetSizeMeters*100:F0}cm) scale x{scaleFactor:F4}");
        }

        private async Task TryLoadGltfMeshAsync(GameObject target, string meshUrl)
        {
            if (_apiClient == null) return;

            try
            {
                var url = ResolveFileUrl(meshUrl);
                Debug.Log($"Loading GLB from: {url}");

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

                    // Apply fixed scale — keep parent's itemScale
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


        /// <summary>
        /// Resolves a mesh/file URL to a full HTTP URL pointing at the backend /files/ endpoint.
        /// Handles: full URLs (rewrite to backend), relative paths (prepend backend + /files/).
        /// </summary>
        private string ResolveFileUrl(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // If it's already a full URL, rewrite host to our backend
            if (path.StartsWith("http"))
            {
                // Extract the path part after /files/
                var filesIndex = path.IndexOf("/files/");
                if (filesIndex >= 0)
                {
                    var relativePart = path[(filesIndex + 7)..]; // after "/files/"
                    return $"{_apiClient!.BaseUrl}/files/{relativePart}";
                }
                return path;
            }

            // Relative path — prepend backend base URL + /files/
            return $"{_apiClient!.BaseUrl}/files/{path.TrimStart('/')}";
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
