using System.Collections;
using UnityEngine;

namespace HomeInventory3D.Animation
{
    /// <summary>
    /// Scale-up + glow + particle burst for newly spawned items.
    /// </summary>
    public static class SpawnAnimation
    {
        private const float Duration = 0.8f;
        private const float BounceOvershoot = 1.15f;

        /// <summary>
        /// Plays spawn animation: scale 0 → overshoot → 1 with glow fade + optional particle burst.
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
    /// MonoBehaviour runner for the spawn coroutine with bounce easing and particle VFX.
    /// </summary>
    public class SpawnAnimationRunner : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static GameObject _vfxPrefab;

        public void StartAnimation(Material glowMaterial, float duration)
        {
            if (_vfxPrefab == null)
                _vfxPrefab = Resources.Load<GameObject>("SpawnVFX");

            StartCoroutine(AnimateSpawn(glowMaterial, duration));
        }

        private IEnumerator AnimateSpawn(Material glowMaterial, float duration)
        {
            var targetScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            transform.localScale = Vector3.zero;

            // Spawn particle burst at position
            SpawnVFX(transform.position);

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

                // Elastic/bounce easing: overshoot then settle
                var scale = EaseOutBack(t);
                transform.localScale = targetScale * scale;

                // Glow fade
                if (glowInstance != null)
                {
                    var glowIntensity = 1f - t;
                    var baseColor = glowMaterial.GetColor(EmissionColorId);
                    glowInstance.SetColor(EmissionColorId, baseColor * (glowIntensity * glowIntensity));
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

        private void SpawnVFX(Vector3 position)
        {
            if (_vfxPrefab != null)
            {
                var vfx = Instantiate(_vfxPrefab, position, Quaternion.identity);
                foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>())
                    ps.Play();
                Destroy(vfx, 3f);
            }
        }

        /// <summary>
        /// Ease-out-back: overshoots then settles. Gives a "pop" feel.
        /// </summary>
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
