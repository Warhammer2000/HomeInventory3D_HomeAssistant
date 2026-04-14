using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Exploded view: items fly apart using LOCAL coordinates (no reparenting).
    /// Toggle with E key.
    /// </summary>
    public class ExplodedView : MonoBehaviour
    {
        [SerializeField] private ContainerManager containerManager;
        [SerializeField] private float explodeRadius = 0.2f;
        [SerializeField] private float animDuration = 0.7f;
        [SerializeField] private float rotateSpeed = 15f;

        private bool _exploded;
        private readonly Dictionary<string, Vector3> _savedLocalPos = new();
        private readonly Dictionary<string, Quaternion> _savedLocalRot = new();
        private readonly List<GameObject> _labels = new();
        private readonly List<LineRenderer> _lines = new();

        public bool IsExploded => _exploded;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                Toggle();

            if (!_exploded) return;

            // Rotate items slowly
            if (containerManager != null)
            {
                foreach (var item in containerManager.SpawnedItems.Values)
                    if (item != null) item.transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.Self);
            }

            // Billboard labels
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var l in _labels)
            {
                if (l == null) continue;
                l.transform.LookAt(cam.transform);
                l.transform.Rotate(0, 180, 0);
            }

            // Update lines from center to items (in world space)
            UpdateLines();
        }

        public void Toggle()
        {
            if (_exploded) Collapse();
            else Explode();
        }

        private void Explode()
        {
            if (containerManager == null) return;
            var items = containerManager.SpawnedItems;
            if (items.Count == 0) return;

            _exploded = true;
            _savedLocalPos.Clear();
            _savedLocalRot.Clear();
            ClearVisuals();

            var index = 0;
            var total = items.Count;

            foreach (var (id, item) in items)
            {
                // Save LOCAL transform
                _savedLocalPos[id] = item.transform.localPosition;
                _savedLocalRot[id] = item.transform.localRotation;

                // Freeze physics
                var rb = item.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                // Calculate exploded LOCAL position — radial around local origin
                var angle = (float)index / total * Mathf.PI * 2f;
                var ring = index % 2 == 0 ? 1f : 0.7f;
                var y = 0.25f + (index % 3) * 0.12f; // stagger height
                var targetLocal = new Vector3(
                    Mathf.Cos(angle) * explodeRadius * ring,
                    y,
                    Mathf.Sin(angle) * explodeRadius * ring);

                StartCoroutine(AnimateLocalPos(item.transform, targetLocal, animDuration));

                // Label (world space, positioned after animation — we'll update in UpdateLines)
                var confStr = item.Confidence > 0 ? $" ({item.Confidence:P0})" : "";
                var labelGo = new GameObject("Label");
                var tm = labelGo.AddComponent<TextMesh>();
                tm.text = item.ItemName + confStr;
                tm.characterSize = 0.03f;
                tm.fontSize = 200;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = Color.white;
                tm.fontStyle = FontStyle.Bold;
                labelGo.GetComponent<MeshRenderer>().sortingOrder = 300;
                labelGo.transform.localScale = Vector3.one * 0.06f;
                // Position relative to item in world
                labelGo.transform.position = item.transform.position + Vector3.up * 0.1f;
                _labels.Add(labelGo);

                // Connecting line
                var lineGo = new GameObject("Line");
                var lr = lineGo.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.startWidth = 0.002f;
                lr.endWidth = 0.001f;
                var c = GetColor(index);
                lr.startColor = c;
                lr.endColor = new Color(c.r, c.g, c.b, 0.15f);
                lr.useWorldSpace = true;
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = c;
                lr.material = mat;
                _lines.Add(lr);

                index++;
            }

            Debug.Log($"Exploded view: ON ({items.Count} items)");
        }

        private void Collapse()
        {
            _exploded = false;

            if (containerManager != null)
            {
                foreach (var (id, item) in containerManager.SpawnedItems)
                {
                    if (!_savedLocalPos.TryGetValue(id, out var origLocal)) continue;

                    StartCoroutine(AnimateLocalPos(item.transform, origLocal, animDuration));

                    if (_savedLocalRot.TryGetValue(id, out var origRot))
                        StartCoroutine(AnimateLocalRot(item.transform, origRot, animDuration));

                    // Re-enable physics after animation
                    StartCoroutine(DelayedAction(animDuration + 0.1f, () =>
                    {
                        var rb = item.GetComponent<Rigidbody>();
                        if (rb != null) rb.isKinematic = false;
                    }));
                }
            }

            // Delayed cleanup of visuals
            StartCoroutine(DelayedAction(animDuration + 0.2f, ClearVisuals));

            Debug.Log("Exploded view: OFF");
        }

        private void UpdateLines()
        {
            if (containerManager == null) return;

            // Center = Items parent world position
            var center = containerManager.ItemsParent.position;

            var i = 0;
            foreach (var item in containerManager.SpawnedItems.Values)
            {
                if (i >= _lines.Count || i >= _labels.Count) break;

                var worldPos = item.transform.position;

                // Update line
                if (_lines[i] != null)
                {
                    _lines[i].SetPosition(0, center);
                    _lines[i].SetPosition(1, worldPos);
                }

                // Update label position
                if (_labels[i] != null)
                    _labels[i].transform.position = worldPos + Vector3.up * 0.1f;

                i++;
            }
        }

        private static IEnumerator AnimateLocalPos(Transform t, Vector3 targetLocal, float dur)
        {
            var start = t.localPosition;
            var el = 0f;
            while (el < dur)
            {
                var p = el / dur;
                var ease = 1f + 2.70158f * Mathf.Pow(p - 1f, 3f) + 1.70158f * Mathf.Pow(p - 1f, 2f);
                t.localPosition = Vector3.Lerp(start, targetLocal, ease);
                el += Time.deltaTime;
                yield return null;
            }
            t.localPosition = targetLocal;
        }

        private static IEnumerator AnimateLocalRot(Transform t, Quaternion targetLocal, float dur)
        {
            var start = t.localRotation;
            var el = 0f;
            while (el < dur)
            {
                t.localRotation = Quaternion.Slerp(start, targetLocal, el / dur);
                el += Time.deltaTime;
                yield return null;
            }
            t.localRotation = targetLocal;
        }

        private static IEnumerator DelayedAction(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        private void ClearVisuals()
        {
            foreach (var l in _labels) if (l != null) Destroy(l);
            _labels.Clear();
            foreach (var lr in _lines) if (lr != null) Destroy(lr.gameObject);
            _lines.Clear();
        }

        private static Color GetColor(int i)
        {
            Color[] c = { new(0.3f, 0.7f, 1f, 0.6f), new(1f, 0.5f, 0.3f, 0.6f), new(0.4f, 1f, 0.5f, 0.6f), new(1f, 0.4f, 0.7f, 0.6f), new(0.7f, 0.5f, 1f, 0.6f) };
            return c[i % c.Length];
        }
    }
}
