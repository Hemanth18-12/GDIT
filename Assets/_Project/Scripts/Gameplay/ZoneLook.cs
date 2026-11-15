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
                case Zone.Bazaar:    return new Look { exposure = 0f,    temperature = 15f,  contrast = 12f, saturation = 10f,  bloom = 0.5f, bloomThreshold = 1.0f, vignette = 0.22f, grain = 0.12f, shadows = new Color(0.92f, 0.95f, 1.05f), fog = new Color(0.78f, 0.62f, 0.45f), fogDensity = 0.004f, sunIntensity = 1.3f, sun = new Color(1f, 0.78f, 0.55f) };
                case Zone.Courtyard: return new Look { exposure = 0.3f,  temperature = 25f,  contrast = 10f, saturation = 5f,   bloom = 0.5f, bloomThreshold = 1.0f, vignette = 0.2f,  grain = 0.1f,  shadows = new Color(0.85f, 0.92f, 1.12f), fog = new Color(0.9f, 0.75f, 0.55f),  fogDensity = 0.006f, sunIntensity = 1.5f, sun = new Color(1f, 0.82f, 0.56f) };
                case Zone.Vaults:    return new Look { exposure = -1.5f, temperature = -10f, contrast = 28f, saturation = -15f, bloom = 0.25f, bloomThreshold = 1.1f, vignette = 0.42f, grain = 0.25f, shadows = new Color(0.9f, 1.0f, 1.08f),  fog = new Color(0.05f, 0.06f, 0.07f), fogDensity = 0.045f, sunIntensity = 0f,   sun = Color.black };
                case Zone.Stepwell:  return new Look { exposure = -1.0f, temperature = -25f, contrast = 26f, saturation = -20f, bloom = 0.8f, bloomThreshold = 0.9f, vignette = 0.35f, grain = 0.2f,  shadows = new Color(0.82f, 0.92f, 1.15f), fog = new Color(0.1f, 0.13f, 0.18f),  fogDensity = 0.02f,  sunIntensity = 0.25f, sun = new Color(0.6f, 0.72f, 1f) };
                case Zone.SunWall:   return new Look { exposure = 0.7f,  temperature = 0f,   contrast = 35f, saturation = -5f,  bloom = 0.25f, bloomThreshold = 1.2f, vignette = 0.15f, grain = 0.08f, shadows = new Color(0.95f, 0.97f, 1.05f), fog = new Color(0.9f, 0.88f, 0.8f),   fogDensity = 0f,     sunIntensity = 2.2f, sun = new Color(1f, 0.97f, 0.9f) };
                default:             return new Look { exposure = -0.3f, temperature = 10f,  contrast = 14f, saturation = 5f,   bloom = 0.5f, bloomThreshold = 1.0f, vignette = 0.3f,  grain = 0.15f, shadows = new Color(1.02f, 0.9f, 1.12f),  fog = new Color(0.32f, 0.27f, 0.36f), fogDensity = 0.018f, sunIntensity = 0.9f, sun = new Color(1f, 0.6f, 0.4f) };
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
            Volume.isGlobal = false; Volume.blendDistance = blendDistance; Volume.priority = 10f; Volume.sharedProfile = Profile;
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
