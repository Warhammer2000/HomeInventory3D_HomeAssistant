using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Enhanced X-Ray mode: container becomes ghost wireframe, items glow by tag color,
    /// pulsing emission, edge fresnel effect.
    /// Toggle with X key.
    /// </summary>
    public class XRayMode : MonoBehaviour
    {
        [SerializeField] private ContainerManager containerManager;

        private bool _active;
        private float _pulseTime;
        private readonly List<RendererState> _savedContainerStates = new();
        private readonly List<Material> _tempMaterials = new();
        private readonly Dictionary<Renderer, Color> _itemGlowColors = new();

        private static readonly Color[] TagColors =
        {
            new(0.3f, 0.7f, 1f),   // blue
            new(1f, 0.5f, 0.3f),   // orange
            new(0.4f, 1f, 0.5f),   // green
            new(1f, 0.4f, 0.7f),   // pink
            new(0.7f, 0.5f, 1f),   // purple
            new(1f, 0.9f, 0.3f),   // yellow
        };

        private struct RendererState
        {
            public Renderer Renderer;
            public Material[] OriginalMaterials;
        }

        public bool IsActive => _active;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.xKey.wasPressedThisFrame)
                Toggle();

            if (_active)
                UpdatePulse();
        }

        public void Toggle()
        {
            if (_active) Deactivate();
            else Activate();
        }

        private void Activate()
        {
            if (containerManager == null) return;
            _active = true;
            _pulseTime = 0;

            // Ghost container — very transparent with edge highlight
            var containerMesh = containerManager.transform.Find("ContainerMesh");
            if (containerMesh != null)
            {
                foreach (var r in containerMesh.GetComponentsInChildren<Renderer>())
                {
                    _savedContainerStates.Add(new RendererState
                    {
                        Renderer = r,
                        OriginalMaterials = r.materials
                    });

                    var ghostMats = new Material[r.materials.Length];
                    for (var i = 0; i < r.materials.Length; i++)
                    {
                        var ghost = new Material(Shader.Find("Universal Render Pipeline/Lit"));

                        // Very transparent base
                        ghost.SetColor("_BaseColor", new Color(0.3f, 0.5f, 0.8f, 0.08f));
                        ghost.SetFloat("_Surface", 1);
                        ghost.SetFloat("_Blend", 0);
                        ghost.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        ghost.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        ghost.SetInt("_ZWrite", 0);
                        ghost.renderQueue = 3000;
                        ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                        // Edge glow emission
                        ghost.EnableKeyword("_EMISSION");
                        ghost.SetColor("_EmissionColor", new Color(0.15f, 0.3f, 0.6f) * 0.3f);

                        // Metallic + smooth for reflection
                        ghost.SetFloat("_Metallic", 0.8f);
                        ghost.SetFloat("_Smoothness", 0.9f);

                        ghostMats[i] = ghost;
                        _tempMaterials.Add(ghost);
                    }
                    r.materials = ghostMats;
                }
            }

            // Glow items — each gets a color based on first tag
            _itemGlowColors.Clear();
            foreach (var item in containerManager.SpawnedItems.Values)
            {
                var glowColor = GetColorForItem(item);
                foreach (var r in item.GetComponentsInChildren<Renderer>())
                {
                    foreach (var m in r.materials)
                    {
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", glowColor * 1.5f);
                    }
                    _itemGlowColors[r] = glowColor;
                }
            }

            Debug.Log("X-Ray mode: ON (pulse + tag colors)");
        }

        private void UpdatePulse()
        {
            _pulseTime += Time.deltaTime;
            var pulse = (Mathf.Sin(_pulseTime * 3f) + 1f) * 0.5f; // 0-1

            // Pulse item glow
            foreach (var (renderer, baseColor) in _itemGlowColors)
            {
                if (renderer == null) continue;
                var intensity = 0.8f + pulse * 1.2f; // 0.8 — 2.0
                foreach (var m in renderer.materials)
                    m.SetColor("_EmissionColor", baseColor * intensity);
            }

            // Pulse container ghost
            foreach (var state in _savedContainerStates)
            {
                if (state.Renderer == null) continue;
                var alpha = 0.05f + pulse * 0.1f; // 0.05 — 0.15
                foreach (var m in state.Renderer.materials)
                    m.SetColor("_BaseColor", new Color(0.3f, 0.5f, 0.8f, alpha));
            }
        }

        private void Deactivate()
        {
            _active = false;

            // Restore container
            foreach (var state in _savedContainerStates)
            {
                if (state.Renderer != null)
                    state.Renderer.materials = state.OriginalMaterials;
            }
            _savedContainerStates.Clear();

            foreach (var m in _tempMaterials)
                Destroy(m);
            _tempMaterials.Clear();

            // Remove item glow
            if (containerManager != null)
            {
                foreach (var item in containerManager.SpawnedItems.Values)
                {
                    foreach (var r in item.GetComponentsInChildren<Renderer>())
                    {
                        foreach (var m in r.materials)
                        {
                            m.DisableKeyword("_EMISSION");
                            m.SetColor("_EmissionColor", Color.black);
                        }
                    }
                }
            }
            _itemGlowColors.Clear();

            Debug.Log("X-Ray mode: OFF");
        }

        private static Color GetColorForItem(ItemController item)
        {
            if (item.Tags != null && item.Tags.Length > 0)
            {
                var hash = Mathf.Abs(item.Tags[0].GetHashCode());
                return TagColors[hash % TagColors.Length];
            }
            return new Color(0.3f, 0.7f, 1f); // default blue
        }
    }
}
