using System.Collections;
using UnityEngine;

namespace HomeInventory3D.Animation
{
    /// <summary>
    /// Pulsing highlight effect for search results.
    /// </summary>
    public class HighlightAnimation : MonoBehaviour
    {
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float minIntensity = 0.3f;
        [SerializeField] private float maxIntensity = 1f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Renderer _renderer;
        private Material _originalMaterial;
        private Material _highlightInstance;
        private bool _isHighlighted;

        /// <summary>
        /// Starts the pulsing highlight effect.
        /// </summary>
        public void StartHighlight(Material overrideMaterial = null)
        {
            if (_isHighlighted) return;

            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null) return;

            _isHighlighted = true;
            _originalMaterial = _renderer.material;
            _highlightInstance = new Material(overrideMaterial != null ? overrideMaterial : highlightMaterial);
            _renderer.material = _highlightInstance;

            StartCoroutine(PulseCoroutine());
        }

        /// <summary>
        /// Stops the highlight and restores the original material.
        /// </summary>
        public void StopHighlight()
        {
            if (!_isHighlighted) return;

            _isHighlighted = false;
            StopAllCoroutines();

            if (_renderer != null && _originalMaterial != null)
            {
                _renderer.material = _originalMaterial;
            }

            if (_highlightInstance != null)
            {
                Destroy(_highlightInstance);
                _highlightInstance = null;
            }
        }

        private IEnumerator PulseCoroutine()
        {
            var baseColor = _highlightInstance.GetColor(EmissionColorId);

            while (_isHighlighted)
            {
                var t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                var intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
                _highlightInstance.SetColor(EmissionColorId, baseColor * intensity);
                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (_highlightInstance != null)
                Destroy(_highlightInstance);
        }
    }
}
