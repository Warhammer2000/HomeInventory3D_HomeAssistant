using System.Collections;
using UnityEngine;

namespace HomeInventory3D.Animation
{
    /// <summary>
    /// Scale-up + glow effect for newly spawned items.
    /// </summary>
    public static class SpawnAnimation
    {
        private const float Duration = 0.6f;

        /// <summary>
        /// Plays spawn animation on a GameObject: scale 0 → 1 with emission glow fade.
        /// </summary>
        public static void Play(GameObject target, Material glowMaterial = null)
        {
            var runner = target.GetComponent<SpawnAnimationRunner>();
            if (runner == null)
                runner = target.AddComponent<SpawnAnimationRunner>();

            runner.StartAnimation(glowMaterial, Duration);
        }
    }

    /// <summary>
    /// MonoBehaviour runner for the spawn coroutine.
    /// Destroys itself after animation completes.
    /// </summary>
    public class SpawnAnimationRunner : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public void StartAnimation(Material glowMaterial, float duration)
        {
            StartCoroutine(AnimateSpawn(glowMaterial, duration));
        }

        private IEnumerator AnimateSpawn(Material glowMaterial, float duration)
        {
            var targetScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            transform.localScale = Vector3.zero;

            var renderer = GetComponentInChildren<Renderer>();
            Material originalMaterial = null;
            Material glowInstance = null;

            if (renderer != null && glowMaterial != null)
            {
                originalMaterial = renderer.material;
                glowInstance = new Material(glowMaterial);
                renderer.material = glowInstance;
            }

            var elapsed = 0f;

            while (elapsed < duration)
            {
                var t = elapsed / duration;
                var scale = Mathf.SmoothStep(0f, 1f, t);
                transform.localScale = targetScale * scale;

                if (glowInstance != null)
                {
                    var glowIntensity = 1f - t;
                    var baseColor = glowMaterial.GetColor(EmissionColorId);
                    glowInstance.SetColor(EmissionColorId, baseColor * glowIntensity);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = targetScale;

            if (renderer != null && originalMaterial != null)
            {
                renderer.material = originalMaterial;
            }

            if (glowInstance != null)
            {
                Destroy(glowInstance);
            }

            Destroy(this);
        }
    }
}
