using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LegacyOfTemples.World
{
    /// <summary>Keep local-light shadows within the Mac's atlas; sunlight is separate.</summary>
    public sealed class PunctualShadowBudget : MonoBehaviour
    {
        readonly Dictionary<Light, LightShadows> candidates = new Dictionary<Light, LightShadows>();
        readonly List<Light> nearest = new List<Light>();
        readonly List<Light> removed = new List<Light>();
        float refreshAt;
        int lastFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<PunctualShadowBudget>() != null) return;
            var go = new GameObject("Local light shadow budget");
            DontDestroyOnLoad(go);
            go.AddComponent<PunctualShadowBudget>();
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            SceneManager.sceneLoaded -= SceneLoaded;
            foreach (var pair in candidates) if (pair.Key != null) pair.Key.shadows = pair.Value;
        }

        void SceneLoaded(Scene scene, LoadSceneMode mode) { refreshAt = 0; lastFrame = -1; }

        void BeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType != CameraType.Game || lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime + 0.5f;
                removed.Clear();
                foreach (var pair in candidates) if (pair.Key == null) removed.Add(pair.Key);
                foreach (var light in removed) candidates.Remove(light);
                foreach (var light in FindObjectsByType<Light>())
                {
                    if ((light.type != LightType.Point && light.type != LightType.Spot) ||
                        light.shadows == LightShadows.None || candidates.ContainsKey(light)) continue;
                    candidates.Add(light, light.shadows);
                    // URP's Low tier is 256px in this project's PC renderer.
                    light.GetUniversalAdditionalLightData().additionalLightsShadowResolutionTier = 0;
                }
            }

            nearest.Clear();
            foreach (var pair in candidates)
                if (pair.Key != null && pair.Key.isActiveAndEnabled && pair.Key.intensity > 0)
                    nearest.Add(pair.Key);
            Vector3 eye = camera.transform.position;
            nearest.Sort((a, b) => (a.transform.position - eye).sqrMagnitude.CompareTo((b.transform.position - eye).sqrMagnitude));
            for (int i = 0; i < nearest.Count; i++)
                nearest[i].shadows = i < 2 ? candidates[nearest[i]] : LightShadows.None;
        }
    }
}
