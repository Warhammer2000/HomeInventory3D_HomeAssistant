using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HomeInventory3D.Editor
{
    /// <summary>
    /// Injects HomeInventory3D components into ANY existing scene without touching its layout/lights/camera.
    /// Places a container box on the first table-like surface found, or at a configurable position.
    /// Menu: HomeInventory3D → Inject Into Current Scene
    /// </summary>
    public static class InjectIntoSceneEditor
    {
        private const string PrefabsPath = "Assets/Prefabs";
        private const string MaterialsPath = "Assets/Materials";
        private const string FUR = "Assets/ithappy/Furniture_FREE/Prefabs";

        [MenuItem("HomeInventory3D/Inject Into Current Scene", false, 4)]
        public static void InjectIntoScene()
        {
            // Ensure materials and prefab exist
            EnsureMaterials();
            EnsureGenericItemPrefab();

            // Create core GameObjects
            var gmGo = FindOrCreate("[GameManager]");
            EnsureComponent<GameManager>(gmGo);
            var apiConfig = EnsureComponent<Networking.ApiConfig>(gmGo);
            var signalR = EnsureComponent<Networking.SignalRClient>(gmGo);
            var spawner = EnsureComponent<Scene.ItemSpawner>(gmGo);
            var sceneLoader = EnsureComponent<Scene.SceneLoader>(gmGo);
            var tagConnections = EnsureComponent<Scene.TagConnectionManager>(gmGo);
            var xray = EnsureComponent<Scene.XRayMode>(gmGo);
            var exploded = EnsureComponent<Scene.ExplodedView>(gmGo);
            var dayNight = EnsureComponent<Scene.DayNightCycle>(gmGo);

            // UnityMainThread
            var threadGo = FindOrCreate("[UnityMainThread]");
            EnsureComponent<Utils.UnityMainThread>(threadGo);

            // Container — place at a reasonable position in the scene
            var containerGo = FindOrCreate("[Container]");
            var containerMgr = EnsureComponent<Scene.ContainerManager>(containerGo);

            // Try to find a table in the scene to place on
            var tablePos = FindTablePosition();
            containerGo.transform.position = tablePos;

            // Container box visual
            var existingMesh = containerGo.transform.Find("ContainerMesh");
            if (existingMesh == null)
            {
                var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FUR}/box_001.prefab");
                if (boxPrefab != null)
                {
                    var box = (GameObject)PrefabUtility.InstantiatePrefab(boxPrefab);
                    box.name = "ContainerMesh";
                    box.transform.SetParent(containerGo.transform);
                    box.transform.localPosition = new Vector3(0, 0.05f, 0);
                    box.transform.localRotation = Quaternion.identity;
                    box.transform.localScale = Vector3.one * 0.6f;
                }
            }

            // Items parent
            var itemsParent = FindOrCreateChild(containerGo, "Items");
            itemsParent.transform.localPosition = new Vector3(0, 0.1f, 0);

            // UI
            var uiGo = FindOrCreate("[UI]");
            var hoverHUD = EnsureComponent<UI.ItemHoverHUD>(uiGo);

            // Add OrbitCamera to existing main camera
            var cam = Camera.main;
            if (cam != null)
            {
                var orbit = EnsureComponent<Scene.OrbitCamera>(cam.gameObject);
                EnsureComponent<UniversalAdditionalCameraData>(cam.gameObject);

                // Wire orbit target
                var orbitSo = new SerializedObject(orbit);
                SetField(orbitSo, "target", containerGo.transform);
                SetFloat(orbitSo, "distance", 1.5f);
                SetFloat(orbitSo, "minDistance", 0.5f);
                SetFloat(orbitSo, "maxDistance", 5f);
                orbitSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Wire all SerializeField references
            var spawnGlow = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/SpawnGlow.mat");
            var highlight = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/Highlight.mat");
            var genericItem = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/GenericItem.prefab");

            WireReferences(gmGo, signalR, sceneLoader, containerMgr, spawner,
                hoverHUD, spawnGlow, highlight, genericItem, containerGo, itemsParent.gameObject);

            EditorUtility.SetDirty(gmGo);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("HomeInventory3D",
                $"Injected into current scene!\n\n" +
                $"Container placed at: {tablePos}\n" +
                $"Components: GameManager, SignalR, Container,\n" +
                $"ItemSpawner, SceneLoader, UI, OrbitCamera\n\n" +
                $"Set Backend URL and Container ID in [GameManager] inspector,\n" +
                $"then press Play!",
                "OK");
        }

        private static Vector3 FindTablePosition()
        {
            // Search for objects that look like tables
            var tableNames = new[] { "Table", "table", "Desk", "desk", "RoomTable", "KitchenTable", "OfficeTable", "Bar" };

            foreach (var name in tableNames)
            {
                var candidates = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
                foreach (var t in candidates)
                {
                    if (!t.name.Contains(name, System.StringComparison.OrdinalIgnoreCase)) continue;

                    // Get top of table
                    var renderers = t.GetComponentsInChildren<Renderer>();
                    if (renderers.Length == 0) continue;

                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers)
                        bounds.Encapsulate(r.bounds);

                    var pos = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                    Debug.Log($"Found table '{t.name}' — placing container at {pos}");
                    return pos;
                }
            }

            // Fallback: center of scene, slightly elevated
            Debug.Log("No table found — placing container at default position");
            return new Vector3(0, 1f, 0);
        }

        private static void WireReferences(
            GameObject gmGo,
            Networking.SignalRClient signalR,
            Scene.SceneLoader sceneLoader,
            Scene.ContainerManager containerMgr,
            Scene.ItemSpawner spawner,
            UI.ItemHoverHUD hoverHUD,
            Material spawnGlow,
            Material highlight,
            GameObject genericItem,
            GameObject containerGo,
            GameObject itemsParent)
        {
            var gm = gmGo.GetComponent<GameManager>();

            // GameManager
            var gmSo = new SerializedObject(gm);
            SetField(gmSo, "signalRClient", signalR);
            SetField(gmSo, "sceneLoader", sceneLoader);
            SetField(gmSo, "containerManager", containerMgr);
            SetField(gmSo, "itemSpawner", spawner);
            SetField(gmSo, "tagConnections", gmGo.GetComponent<Scene.TagConnectionManager>());
            gmSo.ApplyModifiedPropertiesWithoutUndo();

            // SignalRClient
            var srSo = new SerializedObject(signalR);
            SetField(srSo, "apiConfig", gmGo.GetComponent<Networking.ApiConfig>());
            srSo.ApplyModifiedPropertiesWithoutUndo();

            // SceneLoader
            var slSo = new SerializedObject(sceneLoader);
            SetField(slSo, "containerManager", containerMgr);
            SetField(slSo, "itemSpawner", spawner);
            SetField(slSo, "signalRClient", signalR);
            slSo.ApplyModifiedPropertiesWithoutUndo();

            // TagConnectionManager
            var tcSo = new SerializedObject(gmGo.GetComponent<Scene.TagConnectionManager>());
            SetField(tcSo, "containerManager", containerMgr);
            tcSo.ApplyModifiedPropertiesWithoutUndo();

            // ItemSpawner
            var isSo = new SerializedObject(spawner);
            SetField(isSo, "genericItemPrefab", genericItem);
            SetField(isSo, "spawnGlowMaterial", spawnGlow);
            SetField(isSo, "containerManager", containerMgr);
            isSo.ApplyModifiedPropertiesWithoutUndo();

            // ContainerManager
            var cmSo = new SerializedObject(containerMgr);
            SetField(cmSo, "itemsParent", itemsParent.transform);
            cmSo.ApplyModifiedPropertiesWithoutUndo();

            // XRayMode, ExplodedView — wire containerManager
            var xraySo = new SerializedObject(gmGo.GetComponent<Scene.XRayMode>());
            SetField(xraySo, "containerManager", containerMgr);
            xraySo.ApplyModifiedPropertiesWithoutUndo();

            var explodeSo = new SerializedObject(gmGo.GetComponent<Scene.ExplodedView>());
            SetField(explodeSo, "containerManager", containerMgr);
            explodeSo.ApplyModifiedPropertiesWithoutUndo();

            // ItemHoverHUD
            var hudSo = new SerializedObject(hoverHUD);
            SetField(hudSo, "containerManager", containerMgr);
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
                AssetDatabase.CreateFolder("Assets", "Materials");

            CreateMaterialIfMissing("SpawnGlow", new Color(0.3f, 0.7f, 1f, 0.6f), 3f);
            CreateMaterialIfMissing("Highlight", new Color(1f, 0.75f, 0.2f, 0.5f), 2f);
        }

        private static void CreateMaterialIfMissing(string name, Color color, float emissionMultiplier)
        {
            var path = $"{MaterialsPath}/{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionMultiplier);
            mat.SetFloat("_Surface", 1);
            mat.renderQueue = 3000;
            AssetDatabase.CreateAsset(mat, path);
        }

        private static void EnsureGenericItemPrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabsPath))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var path = $"{PrefabsPath}/GenericItem.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "GenericItem";
            obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            obj.AddComponent<Scene.ItemController>();
            PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
        }

        private static GameObject FindOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            }
            return go;
        }

        private static GameObject FindOrCreateChild(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = Vector3.zero;
            return go;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void SetField(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static void SetFloat(SerializedObject so, string field, float value)
        {
            var prop = so.FindProperty(field);
            if (prop != null) prop.floatValue = value;
        }
    }
}
