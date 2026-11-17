using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Graphics presets (Low, Medium, High, Ultra) from Settings. High is the default. Each preset sets render scale,
    /// shadow distance and detail, anti-aliasing, level-of-detail bias and how heavy the zone effects may be.
    /// </summary>
    public static class GraphicsPresets
    {
        public struct Preset { public string name; public float renderScale, shadowDistance, lodBias; public int cascades, msaa, pixelLights; public bool softShadows, ambientOcclusion, depthOfField, motionBlur; public float effectScale; }

        public static readonly Preset[] All =
        {
            new Preset { name = "Low",    renderScale = 0.75f, shadowDistance = 35f,  lodBias = 0.7f, cascades = 1, msaa = 1, pixelLights = 2, softShadows = false, ambientOcclusion = false, depthOfField = false, motionBlur = false, effectScale = 0.4f },
            new Preset { name = "Medium", renderScale = 0.9f,  shadowDistance = 60f,  lodBias = 1.0f, cascades = 2, msaa = 2, pixelLights = 4, softShadows = true,  ambientOcclusion = false, depthOfField = false, motionBlur = false, effectScale = 0.7f },
            new Preset { name = "High",   renderScale = 1.0f,  shadowDistance = 70f,  lodBias = 1.2f, cascades = 2, msaa = 4, pixelLights = 6, softShadows = true,  ambientOcclusion = true,  depthOfField = true,  motionBlur = false, effectScale = 1.0f },
            new Preset { name = "Ultra",  renderScale = 1.0f,  shadowDistance = 140f, lodBias = 2.0f, cascades = 4, msaa = 8, pixelLights = 8, softShadows = true,  ambientOcclusion = true,  depthOfField = true,  motionBlur = true,  effectScale = 1.0f },
        };

        public static Preset Current => All[Mathf.Clamp(GameOptions.Graphics, 0, All.Length - 1)];
        public static event System.Action<Preset> OnApplied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { GameOptions.OnChanged -= Apply; GameOptions.OnChanged += Apply; Apply(); }

        public static void Apply()
        {
            var p = Current;
            Application.targetFrameRate = 60; QualitySettings.vSyncCount = 1;
            QualitySettings.shadowDistance = p.shadowDistance; QualitySettings.lodBias = p.lodBias; QualitySettings.pixelLightCount = p.pixelLights;
            // In the Editor the pipeline asset is a project file; changing it there would rewrite the file, so only the built game does it.
            if (!Application.isEditor && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = p.renderScale; urp.shadowDistance = p.shadowDistance; urp.shadowCascadeCount = p.cascades; urp.msaaSampleCount = p.msaa;
            }
            if (!Application.isEditor && Screen.fullScreen != GameOptions.Fullscreen) Screen.fullScreen = GameOptions.Fullscreen;
            OnApplied?.Invoke(p);
        }
    }
}
