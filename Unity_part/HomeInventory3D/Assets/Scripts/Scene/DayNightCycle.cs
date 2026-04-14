using UnityEngine;
using UnityEngine.InputSystem;

namespace HomeInventory3D.Scene
{
    /// <summary>
    /// Day/night cycle: smoothly transitions lighting between warm day and cozy night.
    /// Toggle with N key, or auto-cycle.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private Light mainLight;
        [SerializeField] private float cycleSpeed = 0.05f;
        [SerializeField] private bool autoCycle;

        private float _timeOfDay = 0.3f; // 0=midnight, 0.25=sunrise, 0.5=noon, 0.75=sunset

        // Day preset
        private static readonly Color DayLightColor = new(1f, 0.95f, 0.88f);
        private const float DayIntensity = 1.3f;
        private static readonly Color DaySky = new(0.5f, 0.55f, 0.7f);
        private static readonly Color DayEquator = new(0.4f, 0.38f, 0.35f);
        private static readonly Color DayGround = new(0.25f, 0.22f, 0.18f);
        private static readonly Color DayBg = new(0.75f, 0.82f, 0.9f);

        // Night preset
        private static readonly Color NightLightColor = new(0.3f, 0.35f, 0.6f);
        private const float NightIntensity = 0.3f;
        private static readonly Color NightSky = new(0.05f, 0.05f, 0.15f);
        private static readonly Color NightEquator = new(0.08f, 0.06f, 0.1f);
        private static readonly Color NightGround = new(0.03f, 0.02f, 0.05f);
        private static readonly Color NightBg = new(0.06f, 0.05f, 0.1f);

        // Sunset preset
        private static readonly Color SunsetLightColor = new(1f, 0.6f, 0.3f);
        private const float SunsetIntensity = 0.8f;
        private static readonly Color SunsetSky = new(0.6f, 0.3f, 0.2f);

        private void Start()
        {
            if (mainLight == null)
            {
                var lightGo = GameObject.Find("Directional Light");
                if (lightGo != null)
                    mainLight = lightGo.GetComponent<Light>();
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                // N = toggle day/night
                if (kb.nKey.wasPressedThisFrame)
                {
                    _timeOfDay = _timeOfDay < 0.5f ? 0.8f : 0.3f;
                }
                // [ and ] = manual adjust
                if (kb.leftBracketKey.isPressed)
                    _timeOfDay -= cycleSpeed * Time.deltaTime;
                if (kb.rightBracketKey.isPressed)
                    _timeOfDay += cycleSpeed * Time.deltaTime;
            }

            if (autoCycle)
                _timeOfDay += cycleSpeed * 0.02f * Time.deltaTime;

            _timeOfDay %= 1f;
            if (_timeOfDay < 0) _timeOfDay += 1f;

            ApplyLighting();
        }

        private void ApplyLighting()
        {
            if (mainLight == null) return;

            // Determine blend factors
            // 0.0-0.2 = night, 0.2-0.35 = sunrise, 0.35-0.65 = day, 0.65-0.8 = sunset, 0.8-1.0 = night
            float dayFactor;
            Color lightColor;
            float intensity;

            if (_timeOfDay < 0.2f || _timeOfDay > 0.8f)
            {
                // Night
                dayFactor = 0f;
                lightColor = NightLightColor;
                intensity = NightIntensity;
            }
            else if (_timeOfDay < 0.35f)
            {
                // Sunrise
                var t = (_timeOfDay - 0.2f) / 0.15f;
                dayFactor = t;
                lightColor = Color.Lerp(NightLightColor, SunsetLightColor, t);
                intensity = Mathf.Lerp(NightIntensity, SunsetIntensity, t);
            }
            else if (_timeOfDay < 0.65f)
            {
                // Day
                dayFactor = 1f;
                lightColor = DayLightColor;
                intensity = DayIntensity;
            }
            else
            {
                // Sunset
                var t = (_timeOfDay - 0.65f) / 0.15f;
                dayFactor = 1f - t;
                lightColor = Color.Lerp(DayLightColor, SunsetLightColor, t);
                intensity = Mathf.Lerp(DayIntensity, SunsetIntensity, t);
            }

            // Apply
            mainLight.color = lightColor;
            mainLight.intensity = intensity;
            mainLight.transform.rotation = Quaternion.Euler(
                Mathf.Lerp(10f, 60f, dayFactor),
                -30f + _timeOfDay * 360f,
                0);

            // Ambient
            RenderSettings.ambientSkyColor = Color.Lerp(NightSky, DaySky, dayFactor);
            RenderSettings.ambientEquatorColor = Color.Lerp(NightEquator, DayEquator, dayFactor);
            RenderSettings.ambientGroundColor = Color.Lerp(NightGround, DayGround, dayFactor);

            // Camera bg
            var cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = Color.Lerp(NightBg, DayBg, dayFactor);
        }
    }
}
