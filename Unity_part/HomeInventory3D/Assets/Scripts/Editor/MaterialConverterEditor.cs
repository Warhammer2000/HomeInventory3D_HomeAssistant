using UnityEditor;
using UnityEngine;

namespace HomeInventory3D.Editor
{
    /// <summary>
    /// Converts all Built-in RP materials to URP Lit shader.
    /// Menu: HomeInventory3D → Convert Materials to URP
    /// </summary>
    public static class MaterialConverterEditor
    {
        [MenuItem("HomeInventory3D/Convert Materials to URP", false, 10)]
        public static void ConvertAllMaterials()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");

            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("Error", "URP Lit shader not found!", "OK");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            var converted = 0;
            var skipped = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                var shaderName = mat.shader.name;

                // Skip already URP materials
                if (shaderName.StartsWith("Universal Render Pipeline") ||
                    shaderName.StartsWith("Shader Graphs") ||
                    shaderName.StartsWith("Hidden"))
                {
                    skipped++;
                    continue;
                }

                // Get properties before switching shader
                var mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                var metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                var glossiness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
                var bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                var emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
                var emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;

                // Check if it's an unlit/particle shader
                var isUnlit = shaderName.Contains("Unlit") || shaderName.Contains("Particle");

                // Switch shader
                mat.shader = isUnlit && urpUnlit != null ? urpUnlit : urpLit;

                // Re-apply properties with URP names
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", mainColor);

                if (mat.HasProperty("_BaseMap") && mainTex != null)
                    mat.SetTexture("_BaseMap", mainTex);

                if (mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", metallic);

                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", glossiness);

                if (mat.HasProperty("_BumpMap") && bumpMap != null)
                    mat.SetTexture("_BumpMap", bumpMap);

                if (emissionColor != Color.black || emissionMap != null)
                {
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", emissionColor);
                    if (mat.HasProperty("_EmissionMap") && emissionMap != null)
                        mat.SetTexture("_EmissionMap", emissionMap);
                }

                EditorUtility.SetDirty(mat);
                converted++;
                Debug.Log($"Converted: {path} ({shaderName} → {mat.shader.name})");
            }

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Material Converter",
                $"Done!\n\nConverted: {converted}\nSkipped (already URP): {skipped}",
                "OK");
        }
    }
}
