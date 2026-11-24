using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// The look of one zone: exposure, white balance, contrast, saturation, bloom, vignette, grain, fog and sun.
    /// Put on a box at a zone; it builds a URP Volume from the numbers in Docs/LEVEL_DESIGN_V3.md and blends in as the camera enters.
    /// </summary>
    public class ZoneLook : MonoBehaviour
    {
        public enum Zone { Bazaar, Courtyard, Vaults, Stepwell, SunWall, Jungle }

        [System.Serializable]
        public struct Look
        {
            public float exposure, temperature, tint, contrast, saturation, bloom, bloomThreshold, vignette, grain;
            public Color shadows, fog; public float fogDensity; public float sunIntensity; public Color sun;
        }

        public Zone zone;
        public Vector3 size = new Vector3(120f, 60f, 120f);
        public float blendDistance = 12f;
        public Volume Volume { get; private set; }
        public VolumeProfile Profile { get; private set; }

        public static Look Preset(Zone z)
        {
            switch (z)
            {
                case Zone.Bazaar:    return new Look { exposure = 0.7f, temperature = 6f, contrast = 6f, saturation = 2f, bloom = 0.18f, bloomThreshold = 1.2f, vignette = 0.12f, grain = 0.025f, shadows = new Color(0.99f, 1f, 1.01f), fog = new Color(0.68f, 0.62f, 0.54f), fogDensity = 0.003f, sunIntensity = 1.3f, sun = new Color(1f, 0.88f, 0.72f) };
                case Zone.Courtyard: return new Look { exposure = 0.65f, temperature = 3f, contrast = 5f, saturation = 0f, bloom = 0.18f, bloomThreshold = 1.2f, vignette = 0.12f, grain = 0.025f, shadows = new Color(0.99f, 1f, 1.02f), fog = new Color(0.72f, 0.69f, 0.62f), fogDensity = 0.003f, sunIntensity = 1.5f, sun = new Color(1f, 0.9f, 0.76f) };
                case Zone.Vaults:    return new Look { exposure = 0.1f, temperature = 0f, contrast = 8f, saturation = -4f, bloom = 0.15f, bloomThreshold = 1.2f, vignette = 0.18f, grain = 0.03f, shadows = Color.white, fog = new Color(0.12f, 0.11f, 0.10f), fogDensity = 0.008f, sunIntensity = 0f, sun = Color.black };
                case Zone.Stepwell:  return new Look { exposure = 0.35f, temperature = -5f, contrast = 7f, saturation = -3f, bloom = 0.2f, bloomThreshold = 1.2f, vignette = 0.16f, grain = 0.03f, shadows = new Color(0.99f, 1f, 1.02f), fog = new Color(0.17f, 0.19f, 0.22f), fogDensity = 0.008f, sunIntensity = 0.65f, sun = new Color(0.88f, 0.93f, 1f) };
                case Zone.SunWall:   return new Look { exposure = 0.5f, temperature = 0f, contrast = 8f, saturation = 0f, bloom = 0.15f, bloomThreshold = 1.3f, vignette = 0.1f, grain = 0.02f, shadows = Color.white, fog = new Color(0.9f, 0.88f, 0.8f), fogDensity = 0f, sunIntensity = 1.6f, sun = new Color(1f, 0.97f, 0.9f) };
                default:             return new Look { exposure = 0.5f, temperature = 4f, contrast = 6f, saturation = 2f, bloom = 0.18f, bloomThreshold = 1.2f, vignette = 0.14f, grain = 0.025f, shadows = new Color(1f, 1f, 1.01f), fog = new Color(0.30f, 0.32f, 0.33f), fogDensity = 0.01f, sunIntensity = 1.0f, sun = new Color(1f, 0.84f, 0.68f) };
            }
        }

        void Awake() { Build(); }

        public void Build()
        {
            var look = Preset(zone);
            if (Profile == null) Profile = ScriptableObject.CreateInstance<VolumeProfile>();
            Profile.name = "ZoneLook_" + zone;
            var tone = Get<Tonemapping>(); tone.mode.Override(TonemappingMode.ACES);
            var ca = Get<ColorAdjustments>(); ca.postExposure.Override(look.exposure); ca.contrast.Override(look.contrast); ca.saturation.Override(look.saturation);
            var wb = Get<WhiteBalance>(); wb.temperature.Override(look.temperature); wb.tint.Override(look.tint);
            var smh = Get<ShadowsMidtonesHighlights>(); smh.shadows.Override(new Vector4(look.shadows.r, look.shadows.g, look.shadows.b, 0f));
            var p = GraphicsPresets.Current;
            var bloom = Get<Bloom>(); bloom.intensity.Override(look.bloom * Mathf.Max(0.5f, p.effectScale)); bloom.threshold.Override(look.bloomThreshold); bloom.scatter.Override(0.65f);
            var vig = Get<Vignette>(); vig.intensity.Override(look.vignette); vig.smoothness.Override(0.45f);
            var grain = Get<FilmGrain>(); grain.intensity.Override(look.grain * p.effectScale); grain.type.Override(FilmGrainLookup.Thin1);
            var mb = Get<MotionBlur>(); mb.intensity.Override(p.motionBlur ? 0.15f : 0f);
            if (Volume == null)
            {
                Volume = gameObject.GetComponent<Volume>(); if (Volume == null) Volume = gameObject.AddComponent<Volume>();
                var box = gameObject.GetComponent<BoxCollider>(); if (box == null) box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true; box.size = size;
            }
            Volume.isGlobal = false; Volume.blendDistance = blendDistance; Volume.priority = 110f; Volume.sharedProfile = Profile;
        }

        T Get<T>() where T : VolumeComponent { if (!Profile.TryGet(out T c)) c = Profile.Add<T>(true); c.active = true; return c; }

        /// <summary>Fog and sun for this zone (the world's time-of-day controller blends these as the player moves between zones).</summary>
        public void ApplyAtmosphere(Light sun)
        {
            var look = Preset(zone);
            RenderSettings.fog = look.fogDensity > 0f; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = look.fog; RenderSettings.fogDensity = look.fogDensity;
            if (sun != null) { sun.intensity = look.sunIntensity; sun.color = look.sun; }
        }
    }
}
