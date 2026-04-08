using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HomeInventory3D.Editor
{
    /// <summary>
    /// Editor menu tool to set up the HomeInventory3D scene from scratch.
    /// Creates all required GameObjects, components, materials, prefabs, and camera.
    /// </summary>
    public static class SceneSetupEditor
    {
        private const string MaterialsPath = "Assets/Materials";
        private const string PrefabsPath = "Assets/Prefabs";

        [MenuItem("HomeInventory3D/Setup Scene", false, 1)]
        public static void SetupScene()
        {
            EnsureDirectories();

            var spawnGlow = CreateSpawnGlowMaterial();
            var highlightMat = CreateHighlightMaterial();
            var containerMat = CreateContainerMaterial();
            var genericItemPrefab = CreateGenericItemPrefab();

            CreateOrFindRoot(spawnGlow, highlightMat, containerMat, genericItemPrefab);

            EditorUtility.DisplayDialog("HomeInventory3D",
                "Scene setup complete!\n\n" +
                "Created:\n" +
                "- Materials (SpawnGlow, Highlight, ContainerWood)\n" +
                "- GenericItem prefab\n" +
                "- [GameManager] with all components\n" +
                "- [Container] with ContainerManager\n" +
                "- Main Camera with OrbitCamera\n" +
                "- Directional Light\n" +
                "- UI Document",
                "OK");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(PrefabsPath))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder($"{PrefabsPath}/UI"))
                AssetDatabase.CreateFolder(PrefabsPath, "UI");
        }

        private static Material CreateSpawnGlowMaterial()
        {
            var path = $"{MaterialsPath}/SpawnGlow.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.2f, 0.8f, 1f, 0.8f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 1f, 1f) * 2f);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.renderQueue = 3000;

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material CreateHighlightMaterial()
        {
            var path = $"{MaterialsPath}/Highlight.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f, 0.6f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f, 1f) * 1.5f);
            mat.SetFloat("_Surface", 1);
            mat.renderQueue = 3000;

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material CreateContainerMaterial()
        {
            var path = $"{MaterialsPath}/ContainerWood.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.55f, 0.35f, 0.18f, 1f));
            mat.SetFloat("_Smoothness", 0.2f);

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static GameObject CreateGenericItemPrefab()
        {
            var path = $"{PrefabsPath}/GenericItem.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "GenericItem";
            obj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            // Add ItemController
            obj.AddComponent<Scene.ItemController>();

            // Add collider for raycasting (already has BoxCollider from primitive)
            var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);

            return prefab;
        }

        private static void CreateOrFindRoot(
            Material spawnGlow, Material highlight, Material containerMat, GameObject genericItemPrefab)
        {
            // --- GameManager ---
            var gmGo = FindOrCreate("[GameManager]");
            var gm = EnsureComponent<GameManager>(gmGo);

            // UnityMainThread
            var threadGo = FindOrCreate("[UnityMainThread]");
            EnsureComponent<Utils.UnityMainThread>(threadGo);

            // ApiConfig
            var apiConfig = EnsureComponent<Networking.ApiConfig>(gmGo);

            // SignalRClient
            var signalR = EnsureComponent<Networking.SignalRClient>(gmGo);

            // --- Container ---
            var containerGo = FindOrCreate("[Container]");
            var containerMgr = EnsureComponent<Scene.ContainerManager>(containerGo);

            // Container visual — open box shape
            var containerVisual = FindOrCreateChild(containerGo, "ContainerMesh");
            if (containerVisual.GetComponent<MeshFilter>() == null)
            {
                // Bottom
                var bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bottom.name = "Bottom";
                bottom.transform.SetParent(containerVisual.transform);
                bottom.transform.localPosition = new Vector3(0, 0, 0);
                bottom.transform.localScale = new Vector3(0.5f, 0.02f, 0.3f);
                SetMaterial(bottom, containerMat);

                // Walls
                CreateWall("WallFront", containerVisual.transform, new Vector3(0, 0.1f, 0.15f), new Vector3(0.5f, 0.2f, 0.02f), containerMat);
                CreateWall("WallBack", containerVisual.transform, new Vector3(0, 0.1f, -0.15f), new Vector3(0.5f, 0.2f, 0.02f), containerMat);
                CreateWall("WallLeft", containerVisual.transform, new Vector3(-0.25f, 0.1f, 0), new Vector3(0.02f, 0.2f, 0.3f), containerMat);
                CreateWall("WallRight", containerVisual.transform, new Vector3(0.25f, 0.1f, 0), new Vector3(0.02f, 0.2f, 0.3f), containerMat);
            }

            // Items parent
            var itemsParent = FindOrCreateChild(containerGo, "Items");

            // --- ItemSpawner ---
            var spawner = EnsureComponent<Scene.ItemSpawner>(gmGo);

            // --- SceneLoader ---
            var sceneLoader = EnsureComponent<Scene.SceneLoader>(gmGo);

            // --- UI Document ---
            var uiGo = FindOrCreate("[UI]");
            var uiDoc = EnsureComponent<UnityEngine.UIElements.UIDocument>(uiGo);
            var toast = EnsureComponent<UI.ToastNotification>(uiGo);
            var progress = EnsureComponent<UI.ProgressOverlay>(uiGo);
            var infoCard = EnsureComponent<UI.ItemInfoCard>(uiGo);
            var searchPanel = EnsureComponent<UI.SearchPanel>(uiGo);

            // --- Camera ---
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            cam.transform.position = new Vector3(0, 0.8f, -1.5f);
            cam.transform.LookAt(containerGo.transform);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 1f);

            var orbit = EnsureComponent<Scene.OrbitCamera>(cam.gameObject);

            // Ensure URP camera data
            EnsureComponent<UniversalAdditionalCameraData>(cam.gameObject);

            // --- Light ---
            var lightGo = FindOrCreate("Directional Light");
            var light = EnsureComponent<Light>(lightGo);
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            EnsureComponent<UniversalAdditionalLightData>(lightGo);

            // --- Wire SerializeField references via SerializedObject ---
            WireReferences(gm, signalR, sceneLoader, containerMgr, spawner,
                toast, progress, infoCard, searchPanel, orbit,
                uiDoc, spawnGlow, highlight, genericItemPrefab, containerGo, itemsParent);

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(gmGo);
        }

        private static void WireReferences(
            GameManager gm,
            Networking.SignalRClient signalR,
            Scene.SceneLoader sceneLoader,
            Scene.ContainerManager containerMgr,
            Scene.ItemSpawner spawner,
            UI.ToastNotification toast,
            UI.ProgressOverlay progress,
            UI.ItemInfoCard infoCard,
            UI.SearchPanel searchPanel,
            Scene.OrbitCamera orbit,
            UnityEngine.UIElements.UIDocument uiDoc,
            Material spawnGlow,
            Material highlight,
            GameObject genericItemPrefab,
            GameObject containerGo,
            GameObject itemsParent)
        {
            // GameManager
            var gmSo = new SerializedObject(gm);
            SetField(gmSo, "signalRClient", signalR);
            SetField(gmSo, "sceneLoader", sceneLoader);
            SetField(gmSo, "containerManager", containerMgr);
            SetField(gmSo, "itemSpawner", spawner);
            SetField(gmSo, "toast", toast);
            SetField(gmSo, "progressOverlay", progress);
            SetField(gmSo, "itemInfoCard", infoCard);
            gmSo.ApplyModifiedPropertiesWithoutUndo();

            // SignalRClient
            var srSo = new SerializedObject(signalR);
            SetField(srSo, "apiConfig", gm.GetComponent<Networking.ApiConfig>());
            srSo.ApplyModifiedPropertiesWithoutUndo();

            // SceneLoader
            var slSo = new SerializedObject(sceneLoader);
            SetField(slSo, "containerManager", containerMgr);
            SetField(slSo, "itemSpawner", spawner);
            SetField(slSo, "signalRClient", signalR);
            slSo.ApplyModifiedPropertiesWithoutUndo();

            // ItemSpawner
            var isSo = new SerializedObject(spawner);
            SetField(isSo, "genericItemPrefab", genericItemPrefab);
            SetField(isSo, "spawnGlowMaterial", spawnGlow);
            SetField(isSo, "containerManager", containerMgr);
            isSo.ApplyModifiedPropertiesWithoutUndo();

            // ContainerManager
            var cmSo = new SerializedObject(containerMgr);
            SetField(cmSo, "itemsParent", itemsParent.transform);
            cmSo.ApplyModifiedPropertiesWithoutUndo();

            // OrbitCamera
            var ocSo = new SerializedObject(orbit);
            SetField(ocSo, "target", containerGo.transform);
            ocSo.ApplyModifiedPropertiesWithoutUndo();

            // UI components
            foreach (var uiComp in new MonoBehaviour[] { toast, progress, infoCard, searchPanel })
            {
                var so = new SerializedObject(uiComp);
                SetField(so, "uiDocument", uiDoc);
                if (uiComp is UI.SearchPanel)
                {
                    SetField(so, "containerManager", containerMgr);
                    SetField(so, "highlightMaterial", highlight);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetField(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
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
            if (comp == null)
                comp = go.AddComponent<T>();
            return comp;
        }

        private static void CreateWall(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = pos;
            wall.transform.localScale = scale;
            SetMaterial(wall, mat);
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = mat;
        }
    }
}
