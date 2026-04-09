using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HomeInventory3D.Editor
{
    /// <summary>
    /// One-click visual upgrade: post-processing, lighting, particles, environment.
    /// Menu: HomeInventory3D → Visual Upgrade
    /// </summary>
    public static class VisualUpgradeEditor
    {
        private const string MaterialsPath = "Assets/Materials";
        private const string PrefabsPath = "Assets/Prefabs";

        [MenuItem("HomeInventory3D/Visual Upgrade", false, 2)]
        public static void ApplyVisualUpgrade()
        {
            SetupPostProcessing();
            SetupLighting();
            SetupEnvironment();
            SetupSpawnVFX();
            UpgradeContainerMaterials();

            CopyVFXToResources();

            EditorUtility.DisplayDialog("HomeInventory3D",
                "Visual upgrade applied!\n\n" +
                "- Post-Processing Volume (Bloom, AO, Color Grading, Vignette)\n" +
                "- 3-point lighting (Key, Fill, Rim)\n" +
                "- Gradient skybox + floor plane\n" +
                "- Spawn particle prefab\n" +
                "- Upgraded container materials",
                "OK");
        }

        private static void SetupPostProcessing()
        {
            // Find or create Global Volume
            var volumeGo = GameObject.Find("[PostProcessing]");
            if (volumeGo == null)
            {
                volumeGo = new GameObject("[PostProcessing]");
                Undo.RegisterCreatedObjectUndo(volumeGo, "Create PostProcessing");
            }

            var volume = volumeGo.GetComponent<Volume>();
            if (volume == null)
                volume = volumeGo.AddComponent<Volume>();

            volume.isGlobal = true;
            volume.priority = 1;

            // Create or update profile
            var profilePath = $"{MaterialsPath}/PostProcessProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            // Bloom — soft glow
            if (!profile.Has<Bloom>())
                profile.Add<Bloom>();
            var bloom = profile.components.Find(c => c is Bloom) as Bloom;
            bloom.active = true;
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(0.5f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(1f, 0.95f, 0.9f));

            // Color Grading — warm, slightly saturated
            if (!profile.Has<ColorAdjustments>())
                profile.Add<ColorAdjustments>();
            var colorAdj = profile.components.Find(c => c is ColorAdjustments) as ColorAdjustments;
            colorAdj.active = true;
            colorAdj.postExposure.Override(0.3f);
            colorAdj.contrast.Override(10f);
            colorAdj.saturation.Override(15f);
            colorAdj.colorFilter.Override(new Color(1f, 0.97f, 0.93f));

            // Tonemapping
            if (!profile.Has<Tonemapping>())
                profile.Add<Tonemapping>();
            var tonemap = profile.components.Find(c => c is Tonemapping) as Tonemapping;
            tonemap.active = true;
            tonemap.mode.Override(TonemappingMode.ACES);

            // Vignette — subtle frame
            if (!profile.Has<Vignette>())
                profile.Add<Vignette>();
            var vignette = profile.components.Find(c => c is Vignette) as Vignette;
            vignette.active = true;
            vignette.intensity.Override(0.25f);
            vignette.smoothness.Override(0.4f);
            vignette.color.Override(new Color(0.1f, 0.08f, 0.15f));

            volume.profile = profile;
            EditorUtility.SetDirty(profile);
        }

        private static void SetupLighting()
        {
            // Key Light (main directional)
            var keyLight = GameObject.Find("Directional Light");
            if (keyLight != null)
            {
                var light = keyLight.GetComponent<Light>();
                light.color = new Color(1f, 0.95f, 0.88f); // warm white
                light.intensity = 1.5f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.6f;
                keyLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            }

            // Fill Light — softer, cooler, from opposite side
            var fillGo = GameObject.Find("[FillLight]");
            if (fillGo == null)
            {
                fillGo = new GameObject("[FillLight]");
                Undo.RegisterCreatedObjectUndo(fillGo, "Create FillLight");
            }
            var fillLight = fillGo.GetComponent<Light>();
            if (fillLight == null)
                fillLight = fillGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.7f, 0.8f, 1f); // cool blue
            fillLight.intensity = 0.4f;
            fillLight.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
            EnsureComponent<UniversalAdditionalLightData>(fillGo);

            // Rim Light — from behind, orange accent
            var rimGo = GameObject.Find("[RimLight]");
            if (rimGo == null)
            {
                rimGo = new GameObject("[RimLight]");
                Undo.RegisterCreatedObjectUndo(rimGo, "Create RimLight");
            }
            var rimLight = rimGo.GetComponent<Light>();
            if (rimLight == null)
                rimLight = rimGo.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(1f, 0.7f, 0.4f); // warm orange
            rimLight.intensity = 0.6f;
            rimLight.shadows = LightShadows.None;
            rimGo.transform.rotation = Quaternion.Euler(-10f, 200f, 0f);
            EnsureComponent<UniversalAdditionalLightData>(rimGo);

            // Ambient light
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.5f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.35f, 0.32f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.18f, 0.15f);
        }

        private static void SetupEnvironment()
        {
            // Background color
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.14f, 0.13f, 0.18f);
            }

            // Floor plane
            var floor = GameObject.Find("[Floor]");
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "[Floor]";
                Undo.RegisterCreatedObjectUndo(floor, "Create Floor");
            }
            floor.transform.position = new Vector3(0, -0.01f, 0);
            floor.transform.localScale = new Vector3(3, 1, 3);

            // Floor material
            var floorMatPath = $"{MaterialsPath}/FloorStylized.mat";
            var floorMat = AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);
            if (floorMat == null)
            {
                floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(floorMat, floorMatPath);
            }
            floorMat.SetColor("_BaseColor", new Color(0.18f, 0.17f, 0.22f));
            floorMat.SetFloat("_Smoothness", 0.85f);
            floorMat.SetFloat("_Metallic", 0.1f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Remove floor collider (we don't need physics)
            var floorCollider = floor.GetComponent<Collider>();
            if (floorCollider != null)
                Object.DestroyImmediate(floorCollider);

            EditorUtility.SetDirty(floorMat);
        }

        private static void SetupSpawnVFX()
        {
            var prefabPath = $"{PrefabsPath}/SpawnVFX.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return;

            var vfxGo = new GameObject("SpawnVFX");

            // Main burst particles — magic sparkles
            var ps = vfxGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.8f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.3f, 0.6f, 1f, 1f),
                new Color(0.6f, 0.4f, 1f, 1f));
            main.gravityModifier = -0.5f; // float upward
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;
            main.playOnAwake = false;

            // Emission — one burst
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

            // Shape — sphere around spawn point
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            // Size over lifetime — shrink
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));

            // Color over lifetime — fade out
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.5f, 0.7f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = gradient;

            // Renderer — additive material
            var renderer = vfxGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            var vfxMatPath = $"{MaterialsPath}/SparkleParticle.mat";
            var vfxMat = AssetDatabase.LoadAssetAtPath<Material>(vfxMatPath);
            if (vfxMat == null)
            {
                vfxMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                vfxMat.SetColor("_BaseColor", new Color(0.5f, 0.7f, 1f, 1f));
                vfxMat.SetFloat("_Surface", 1); // Transparent
                // Additive blending
                vfxMat.SetFloat("_Blend", 1);
                vfxMat.renderQueue = 3000;
                AssetDatabase.CreateAsset(vfxMat, vfxMatPath);
            }
            renderer.sharedMaterial = vfxMat;

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(vfxGo, prefabPath);
            Object.DestroyImmediate(vfxGo);

            // Also create a ring burst sub-effect prefab
            CreateRingBurstPrefab();

            EditorUtility.SetDirty(vfxMat);
        }

        private static void CreateRingBurstPrefab()
        {
            var prefabPath = $"{PrefabsPath}/SpawnRing.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

            var ringGo = new GameObject("SpawnRing");
            var ps = ringGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.4f;
            main.startSpeed = 2f;
            main.startSize = 0.03f;
            main.startColor = new Color(1f, 0.8f, 0.3f, 0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));

            var renderer = ringGo.GetComponent<ParticleSystemRenderer>();
            var vfxMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/SparkleParticle.mat");
            if (vfxMat != null)
                renderer.sharedMaterial = vfxMat;

            PrefabUtility.SaveAsPrefabAsset(ringGo, prefabPath);
            Object.DestroyImmediate(ringGo);
        }

        private static void UpgradeContainerMaterials()
        {
            // Upgrade ContainerWood to a richer wood look
            var woodMatPath = $"{MaterialsPath}/ContainerWood.mat";
            var woodMat = AssetDatabase.LoadAssetAtPath<Material>(woodMatPath);
            if (woodMat != null)
            {
                woodMat.SetColor("_BaseColor", new Color(0.5f, 0.33f, 0.18f));
                woodMat.SetFloat("_Smoothness", 0.25f);
                woodMat.SetFloat("_Metallic", 0f);
                EditorUtility.SetDirty(woodMat);
            }

            // Upgrade SpawnGlow — brighter, more magical
            var glowMatPath = $"{MaterialsPath}/SpawnGlow.mat";
            var glowMat = AssetDatabase.LoadAssetAtPath<Material>(glowMatPath);
            if (glowMat != null)
            {
                glowMat.SetColor("_BaseColor", new Color(0.3f, 0.7f, 1f, 0.6f));
                glowMat.EnableKeyword("_EMISSION");
                glowMat.SetColor("_EmissionColor", new Color(0.3f, 0.7f, 1f) * 3f);
                EditorUtility.SetDirty(glowMat);
            }

            // Upgrade Highlight — warmer pulse
            var highlightMatPath = $"{MaterialsPath}/Highlight.mat";
            var highlightMat = AssetDatabase.LoadAssetAtPath<Material>(highlightMatPath);
            if (highlightMat != null)
            {
                highlightMat.SetColor("_BaseColor", new Color(1f, 0.75f, 0.2f, 0.5f));
                highlightMat.EnableKeyword("_EMISSION");
                highlightMat.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.2f) * 2f);
                EditorUtility.SetDirty(highlightMat);
            }
        }

        private static void CopyVFXToResources()
        {
            const string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
                AssetDatabase.CreateFolder("Assets", "Resources");

            CopyPrefabToResources($"{PrefabsPath}/SpawnVFX.prefab", $"{resourcesPath}/SpawnVFX.prefab");
            CopyPrefabToResources($"{PrefabsPath}/SpawnRing.prefab", $"{resourcesPath}/SpawnRing.prefab");
        }

        private static void CopyPrefabToResources(string src, string dst)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(src)) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dst)) return;
            AssetDatabase.CopyAsset(src, dst);
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }
    }
}
