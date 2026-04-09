using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HomeInventory3D.Editor
{
    /// <summary>
    /// Builds a cozy low-poly living room using LowPolyInterior + Furniture_FREE assets.
    /// Uses mesh bounds for precise placement — nothing floats.
    /// Menu: HomeInventory3D → Beautify Scene
    /// </summary>
    public static class SceneBeautifyEditor
    {
        private const string LPI = "Assets/LowPolyInterior/Prefabs";
        private const string FUR = "Assets/ithappy/Furniture_FREE/Prefabs";

        [MenuItem("HomeInventory3D/Beautify Scene", false, 3)]
        public static void BeautifyScene()
        {
            DestroyIfExists("[CozyRoom]");
            DestroyIfExists("[Floor]");
            DestroyIfExists("[Decoration]");
            DestroyIfExists("[FillLight]");
            DestroyIfExists("[RimLight]");
            DestroyIfExists("[SpotLight]");

            var root = new GameObject("[CozyRoom]");
            Undo.RegisterCreatedObjectUndo(root, "Create CozyRoom");

            BuildFloor(root.transform);
            BuildWalls(root.transform);

            // Place table first, get its surface height
            var table = PlaceLPI("RoomTable_01", "Room", root.transform, V3(0, 0, 0), Q(0, 0, 0));
            var tableTop = GetTopY(table);
            Debug.Log($"Table top Y = {tableTop}");

            SetupContainerOnTable(tableTop);
            PlaceTableItems(root.transform, tableTop);

            // Bookshelf
            BuildLibraryCorner(root.transform);

            // PC desk — place desk, get its height, put items on it
            BuildDeskZone(root.transform);

            // Sofa + coffee table
            BuildSofaCorner(root.transform);

            // Fireplace
            BuildFireplace(root.transform);

            // Wall decorations — hung at fixed wall heights
            BuildWallDecoration(root.transform);

            // Floor details
            BuildLifeDetails(root.transform);

            // TV on shelf
            BuildTVCorner(root.transform);

            SetupLighting(root.transform);
            SetupCamera();
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("HomeInventory3D",
                "Cozy room built with precise placement!\n\n" +
                "All objects snapped to surfaces.\n" +
                "Run 'Visual Upgrade' for post-processing!",
                "OK");
        }

        // ===== Container on table =====
        private static void SetupContainerOnTable(float tableTop)
        {
            var containerGo = GameObject.Find("[Container]")
                ?? GameObject.Find("Container_Test Box");
            if (containerGo == null) return;

            var oldMesh = containerGo.transform.Find("ContainerMesh");
            if (oldMesh != null) Object.DestroyImmediate(oldMesh.gameObject);

            var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{FUR}/box_001.prefab");
            if (boxPrefab != null)
            {
                var box = (GameObject)PrefabUtility.InstantiatePrefab(boxPrefab);
                box.name = "ContainerMesh";
                box.transform.SetParent(containerGo.transform);
                box.transform.localScale = Vector3.one * 0.7f;
                // Sit box ON the table surface
                var boxBottom = GetBottomYLocal(box);
                box.transform.localPosition = new Vector3(0, tableTop - boxBottom, 0);
            }

            var items = containerGo.transform.Find("Items");
            if (items != null)
                items.localPosition = new Vector3(0, tableTop + 0.05f, 0);
        }

        private static void PlaceTableItems(Transform parent, float tableTop)
        {
            // Candle on table
            var candle = PlaceLPI("Candle_01", "Room", parent, V3(-0.3f, 0, 0.12f), Q(0, 15, 0));
            SnapToY(candle, tableTop);

            // Notebook on table
            var notebook = PlaceLPI("NoteBook_01", "Room", parent, V3(0.3f, 0, -0.1f), Q(0, 5, 0));
            SnapToY(notebook, tableTop);
        }

        // ===== Floor =====
        private static void BuildFloor(Transform parent)
        {
            var floor = new GameObject("Floor");
            floor.transform.SetParent(parent);

            for (var x = -2; x <= 2; x += 2)
            for (var z = -2; z <= 2; z += 2)
                PlaceLPI("Floor_05", "Floor", floor.transform, V3(x, 0, z), Q(0, 0, 0));

            // Area rug
            PlaceLPI("Catpet_03", "Room", floor.transform, V3(0, 0.005f, -0.3f), Q(0, 0, 0), V3(1.2f, 1, 1.2f));
        }

        // ===== Walls =====
        private static void BuildWalls(Transform parent)
        {
            var walls = new GameObject("Walls");
            walls.transform.SetParent(parent);

            // Back wall
            PlaceLPI("WallFloor2_01", "Walls", walls.transform, V3(-2, 0, 2), Q(0, 0, 0));
            PlaceLPI("WallFloor2_01", "Walls", walls.transform, V3(0, 0, 2), Q(0, 0, 0));
            PlaceLPI("WallFloor2_01", "Walls", walls.transform, V3(2, 0, 2), Q(0, 0, 0));

            // Left wall
            PlaceLPI("WallFloor2_01", "Walls", walls.transform, V3(-2, 0, 0), Q(0, 90, 0));
            PlaceLPI("WallFloor2_01", "Walls", walls.transform, V3(-2, 0, -2), Q(0, 90, 0));
        }

        // ===== Library =====
        private static void BuildLibraryCorner(Transform parent)
        {
            var zone = new GameObject("LibraryCorner");
            zone.transform.SetParent(parent);

            // Bookshelf against back-left wall
            var shelf = PlaceLPI("Library", "Room", zone.transform, V3(-1.6f, 0, 1.8f), Q(0, 180, 0));

            // Plant on top of shelf
            if (shelf != null)
            {
                var shelfTop = GetTopY(shelf);
                var flower = PlaceLPI("Flower_02", "Flowers", zone.transform, V3(-1.6f, 0, 1.8f), Q(0, 30, 0), V3(0.8f, 0.8f, 0.8f));
                SnapToY(flower, shelfTop);
            }

            // Rubik's cube on a shelf (estimate mid-shelf)
            PlaceLPI("KubikRubik", "Room", zone.transform, V3(-1.3f, 0.5f, 1.82f), Q(0, 25, 0));
        }

        // ===== PC Desk =====
        private static void BuildDeskZone(Transform parent)
        {
            var zone = new GameObject("DeskZone");
            zone.transform.SetParent(parent);

            var desk = PlaceLPI("PC_Table", "Room", zone.transform, V3(1.8f, 0, 1.8f), Q(0, 180, 0));
            var deskTop = GetTopY(desk);
            Debug.Log($"Desk top Y = {deskTop}");

            // Items ON the desk
            var pc = PlaceLPI("PC", "Room", zone.transform, V3(2.1f, 0, 1.8f), Q(0, 180, 0));
            SnapToY(pc, deskTop);

            var screen = PlaceLPI("PC_Screen", "Room", zone.transform, V3(1.8f, 0, 1.8f), Q(0, 180, 0));
            SnapToY(screen, deskTop);

            var keyboard = PlaceLPI("PC_Keyboard", "Room", zone.transform, V3(1.8f, 0, 1.5f), Q(0, 180, 0));
            SnapToY(keyboard, deskTop);

            var mouse = PlaceLPI("PC_Mouse", "Room", zone.transform, V3(2.05f, 0, 1.5f), Q(0, 180, 0));
            SnapToY(mouse, deskTop);

            var smart = PlaceLPI("SmartHome", "Room", zone.transform, V3(2.3f, 0, 1.8f), Q(0, 180, 0));
            SnapToY(smart, deskTop);

            // Chair in front of desk (on floor)
            PlaceLPI("OfficeChair_01", "Room", zone.transform, V3(1.8f, 0, 1.0f), Q(0, 0, 0));
        }

        // ===== Sofa Corner =====
        private static void BuildSofaCorner(Transform parent)
        {
            var zone = new GameObject("SofaCorner");
            zone.transform.SetParent(parent);

            // Sofa against left wall
            PlaceLPI("Sofa_03", "Room", zone.transform, V3(-1.7f, 0, -0.5f), Q(0, 90, 0));

            // Pillows ON sofa
            var sofaTop = 0.42f; // typical sofa seat height
            var p1 = PlaceLPI("Pillow_01", "Room", zone.transform, V3(-1.55f, 0, -0.2f), Q(0, 15, 10));
            SnapToY(p1, sofaTop);
            var p2 = PlaceLPI("Pillow_02", "Room", zone.transform, V3(-1.55f, 0, -0.8f), Q(0, -20, -8));
            SnapToY(p2, sofaTop);

            // Coffee table in front of sofa
            var coffeeTable = PlaceFUR("coffee_table_001", zone.transform, V3(-0.7f, 0, -0.5f), Q(0, 0, 0), V3(0.5f, 0.5f, 0.5f));
            var ctTop = GetTopY(coffeeTable);

            // Items ON coffee table
            var book = PlaceLPI("Book_02", "Room", zone.transform, V3(-0.7f, 0, -0.45f), Q(0, 35, 0));
            SnapToY(book, ctTop);

            var candle = PlaceLPI("Candle_02", "Room", zone.transform, V3(-0.6f, 0, -0.6f), Q(0, 0, 0));
            SnapToY(candle, ctTop);

            var remote = PlaceLPI("TV_Remote", "Room", zone.transform, V3(-0.8f, 0, -0.4f), Q(0, -45, 0));
            SnapToY(remote, ctTop);
        }

        // ===== Fireplace =====
        private static void BuildFireplace(Transform parent)
        {
            var zone = new GameObject("FireplaceArea");
            zone.transform.SetParent(parent);

            PlaceLPI("Fireplace_01", "Room", zone.transform, V3(-1.85f, 0, 0.7f), Q(0, 90, 0));

            // Floor lamp next to fireplace
            PlaceLPI("Lamp_01", "Light", zone.transform, V3(-0.6f, 0, 0.5f), Q(0, 0, 0));
        }

        // ===== Wall Decorations =====
        private static void BuildWallDecoration(Transform parent)
        {
            var zone = new GameObject("WallDecor");
            zone.transform.SetParent(parent);

            // Back wall — pictures hung at eye level
            PlaceLPI("Picture_01", "Room", zone.transform, V3(0, 1.5f, 1.95f), Q(0, 180, 0));
            PlaceLPI("Picture_05", "Room", zone.transform, V3(0.9f, 1.7f, 1.95f), Q(0, 180, 0), V3(0.8f, 0.8f, 0.8f));
            PlaceLPI("Clock_01", "Room", zone.transform, V3(-0.5f, 1.8f, 1.95f), Q(0, 180, 0));

            // Curtain on back wall (implied window)
            PlaceLPI("Curtains_03", "Curtains", zone.transform, V3(0.5f, 0, 1.95f), Q(0, 180, 0));

            // Left wall
            PlaceLPI("Picture_03", "Room", zone.transform, V3(-1.95f, 1.5f, 0.7f), Q(0, 90, 0));
            PlaceLPI("PhotoFrame", "Room", zone.transform, V3(-1.95f, 1.4f, -0.3f), Q(0, 90, 0));
            PlaceLPI("Mirror_01", "Room", zone.transform, V3(-1.95f, 1.4f, 1.4f), Q(0, 90, 0));
        }

        // ===== TV Corner =====
        private static void BuildTVCorner(Transform parent)
        {
            var zone = new GameObject("TVCorner");
            zone.transform.SetParent(parent);

            var shelf = PlaceLPI("TV_Shelf", "Room", zone.transform, V3(2.3f, 0, -0.2f), Q(0, -90, 0));
            var shelfTop = GetTopY(shelf);

            var tv = PlaceLPI("TV", "Room", zone.transform, V3(2.3f, 0, -0.2f), Q(0, -90, 0));
            SnapToY(tv, shelfTop);
        }

        // ===== Life Details =====
        private static void BuildLifeDetails(Transform parent)
        {
            var zone = new GameObject("LifeDetails");
            zone.transform.SetParent(parent);

            PlaceLPI("Flower_01", "Flowers", zone.transform, V3(0.6f, 0, -1f), Q(0, 0, 0));
            PlaceLPI("Flower_05", "Flowers", zone.transform, V3(-1.5f, 0, -1.3f), Q(0, 45, 0), V3(0.9f, 0.9f, 0.9f));
            PlaceFUR("toy_001", zone.transform, V3(-0.4f, 0, -1.1f), Q(0, 60, 0), V3(0.4f, 0.4f, 0.4f));
        }

        // ===== Lighting =====
        private static void SetupLighting(Transform parent)
        {
            var lights = new GameObject("Lights");
            lights.transform.SetParent(parent);

            var keyLight = GameObject.Find("Directional Light");
            if (keyLight != null)
            {
                var light = keyLight.GetComponent<Light>();
                light.color = new Color(1f, 0.92f, 0.82f);
                light.intensity = 1.0f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.4f;
                keyLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            MakePointLight("ContainerLight", lights.transform, V3(0, 1.8f, 0.2f), new Color(1, 0.9f, 0.7f), 2.5f, 3f, true);
            MakePointLight("FireplaceGlow", lights.transform, V3(-1.5f, 0.5f, 0.7f), new Color(1, 0.6f, 0.25f), 1.5f, 2.5f, false);
            MakePointLight("DeskLamp", lights.transform, V3(1.8f, 1.2f, 1.6f), new Color(0.9f, 0.95f, 1f), 1.0f, 2f, false);
            MakePointLight("SofaWarm", lights.transform, V3(-1.2f, 1.5f, -0.5f), new Color(1, 0.85f, 0.7f), 0.6f, 2f, false);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.4f, 0.38f, 0.5f);
            RenderSettings.ambientEquatorColor = new Color(0.35f, 0.3f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.17f, 0.14f);
        }

        // ===== Camera =====
        private static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.transform.position = new Vector3(0.5f, 1.2f, -1.8f);
            cam.transform.LookAt(new Vector3(0, 0.4f, 0));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.09f, 0.13f);
            cam.fieldOfView = 45f;

            var orbit = cam.GetComponent<Scene.OrbitCamera>();
            if (orbit != null)
            {
                var so = new SerializedObject(orbit);
                SetFloat(so, "distance", 2f);
                SetFloat(so, "minDistance", 0.8f);
                SetFloat(so, "maxDistance", 5f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ===== PLACEMENT HELPERS =====

        /// <summary>
        /// Gets the world-space top Y of a GameObject's combined bounds.
        /// </summary>
        private static float GetTopY(GameObject go)
        {
            if (go == null) return 0;
            var bounds = GetCombinedBounds(go);
            return bounds.max.y;
        }

        /// <summary>
        /// Gets the local-space bottom Y offset of a prefab instance.
        /// </summary>
        private static float GetBottomYLocal(GameObject go)
        {
            if (go == null) return 0;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            return bounds.min.y - go.transform.position.y;
        }

        /// <summary>
        /// Snaps a GameObject so its bottom sits exactly at the given Y height.
        /// </summary>
        private static void SnapToY(GameObject go, float surfaceY)
        {
            if (go == null) return;
            var bounds = GetCombinedBounds(go);
            var bottomOffset = bounds.min.y - go.transform.position.y;
            var pos = go.transform.position;
            pos.y = surfaceY - bottomOffset;
            go.transform.position = pos;
        }

        private static Bounds GetCombinedBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        // ===== FACTORY HELPERS =====

        private static GameObject PlaceLPI(string prefabName, string folder, Transform parent, Vector3 pos, Quaternion rot, Vector3? scale = null)
        {
            return PlacePrefab($"{LPI}/{folder}/{prefabName}.prefab", prefabName, parent, pos, rot, scale ?? Vector3.one);
        }

        private static GameObject PlaceFUR(string prefabName, Transform parent, Vector3 pos, Quaternion rot, Vector3? scale = null)
        {
            return PlacePrefab($"{FUR}/{prefabName}.prefab", prefabName, parent, pos, rot, scale ?? Vector3.one);
        }

        private static GameObject PlacePrefab(string path, string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found: {path}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            instance.transform.localScale = scale;
            return instance;
        }

        private static void MakePointLight(string name, Transform parent, Vector3 pos, Color color, float intensity, float range, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            go.AddComponent<UniversalAdditionalLightData>();
        }

        private static void DestroyIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        private static void SetFloat(SerializedObject so, string field, float value)
        {
            var prop = so.FindProperty(field);
            if (prop != null) prop.floatValue = value;
        }

        private static Vector3 V3(float x, float y, float z) => new(x, y, z);
        private static Quaternion Q(float x, float y, float z) => Quaternion.Euler(x, y, z);
    }
}
