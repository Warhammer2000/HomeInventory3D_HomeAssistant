using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Draws glowing LineRenderer connections between items that share the same tags.
    /// Activate by calling ShowConnections("инструмент") or ShowAllConnections().
    /// </summary>
    public class TagConnectionManager : MonoBehaviour
    {
        [SerializeField] private ContainerManager containerManager;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private float lineWidth = 0.003f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float connectionDuration = 8f;

        private readonly List<GameObject> _activeLines = new();
        private float _hideTimer;
        private bool _showing;

        private static readonly Color[] TagColors =
        {
            new(0.3f, 0.7f, 1f, 0.8f),    // blue
            new(1f, 0.5f, 0.3f, 0.8f),     // orange
            new(0.4f, 1f, 0.5f, 0.8f),     // green
            new(1f, 0.4f, 0.7f, 0.8f),     // pink
            new(0.7f, 0.5f, 1f, 0.8f),     // purple
            new(1f, 0.9f, 0.3f, 0.8f),     // yellow
        };

        private void Update()
        {
            if (!_showing) return;

            // Pulse alpha on all lines
            var alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f * 0.6f + 0.2f;
            foreach (var lineGo in _activeLines)
            {
                if (lineGo == null) continue;
                var lr = lineGo.GetComponent<LineRenderer>();
                if (lr == null) continue;

                var c = lr.startColor;
                c.a = alpha;
                lr.startColor = c;
                lr.endColor = c;
            }

            // Auto-hide after duration
            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0)
                HideConnections();
        }

        /// <summary>
        /// Shows connections between all items that share the given tag.
        /// </summary>
        public void ShowConnections(string tag)
        {
            HideConnections();

            if (containerManager == null) return;

            var matchingItems = containerManager.SpawnedItems.Values
                .Where(ic => ic.Tags != null && ic.Tags.Any(t =>
                    t.Equals(tag, System.StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (matchingItems.Count < 2)
            {
                Debug.Log($"TagConnections: only {matchingItems.Count} items with tag '{tag}' — need at least 2");
                return;
            }

            var color = GetColorForTag(tag);

            // Connect each pair
            for (var i = 0; i < matchingItems.Count; i++)
            {
                for (var j = i + 1; j < matchingItems.Count; j++)
                {
                    CreateConnection(matchingItems[i].transform, matchingItems[j].transform, color);
                }
            }

            _showing = true;
            _hideTimer = connectionDuration;

            Debug.Log($"TagConnections: showing {_activeLines.Count} connections for tag '{tag}' ({matchingItems.Count} items)");
        }

        /// <summary>
        /// Shows connections for ALL shared tags between items.
        /// </summary>
        public void ShowAllConnections()
        {
            HideConnections();

            if (containerManager == null) return;

            var items = containerManager.SpawnedItems.Values.ToList();
            var tagGroups = new Dictionary<string, List<ItemController>>();

            // Group items by tag
            foreach (var item in items)
            {
                if (item.Tags == null) continue;
                foreach (var tag in item.Tags)
                {
                    var key = tag.ToLowerInvariant();
                    if (!tagGroups.ContainsKey(key))
                        tagGroups[key] = new List<ItemController>();
                    tagGroups[key].Add(item);
                }
            }

            // Draw connections for groups with 2+ items
            foreach (var (tag, group) in tagGroups)
            {
                if (group.Count < 2) continue;
                var color = GetColorForTag(tag);

                for (var i = 0; i < group.Count; i++)
                {
                    for (var j = i + 1; j < group.Count; j++)
                    {
                        CreateConnection(group[i].transform, group[j].transform, color);
                    }
                }
            }

            _showing = true;
            _hideTimer = connectionDuration;

            Debug.Log($"TagConnections: showing {_activeLines.Count} connections across all tags");
        }

        /// <summary>
        /// Removes all connections.
        /// </summary>
        public void HideConnections()
        {
            foreach (var line in _activeLines)
            {
                if (line != null) Destroy(line);
            }
            _activeLines.Clear();
            _showing = false;
        }

        private void CreateConnection(Transform a, Transform b, Color color)
        {
            var lineGo = new GameObject($"TagLine_{a.name}_{b.name}");
            lineGo.transform.SetParent(transform);

            var lr = lineGo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, a.position);
            lr.SetPosition(1, b.position);

            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.startColor = color;
            lr.endColor = color;
            lr.useWorldSpace = true;

            // Create glowing material
            if (lineMaterial != null)
            {
                var mat = new Material(lineMaterial);
                mat.SetColor("_BaseColor", color);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2f);
                lr.material = mat;
            }
            else
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = color;
                lr.material = mat;
            }

            _activeLines.Add(lineGo);
        }

        private static void AddParticleTrail(GameObject parent, Vector3 from, Vector3 to, Color color)
        {
            var ps = parent.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2f;
            main.loop = true;
            main.startLifetime = 1.5f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.003f, 0.008f);
            main.startColor = new Color(color.r, color.g, color.b, 0.6f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 15;

            // Shape: edge between two points
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
            shape.radius = Vector3.Distance(from, to);
            shape.position = (from + to) * 0.5f - parent.transform.position;
            shape.rotation = Quaternion.LookRotation(to - from).eulerAngles;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0, 0.5f, 1, 0));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) },
                new[] { new GradientAlphaKey(0.6f, 0), new GradientAlphaKey(0, 1) });
            colorOverLifetime.color = gradient;

            var renderer = parent.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static Color GetColorForTag(string tag)
        {
            var hash = Mathf.Abs(tag.GetHashCode());
            return TagColors[hash % TagColors.Length];
        }
    }
}
