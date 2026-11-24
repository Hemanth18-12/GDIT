using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.World;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// The main menu's live background: the real temple at golden hour with a slow camera drift.
    /// (The old menu camera circled a blank wall.) Built at runtime, away from the old menu set, with no colliders.
    /// </summary>
    public sealed class MenuTempleBackdrop : MonoBehaviour
    {
        static readonly Vector3 Site = new Vector3(3000f, 0f, 3000f);
        Bounds b; Camera cam; float t;
        public static bool Ready { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded += (s, m) => { if (s.name == "MainMenu") Make(); };
            if (SceneManager.GetActiveScene().name == "MainMenu") Make();
        }
        static void Make() { Ready = false; new GameObject("MenuTempleBackdrop").AddComponent<MenuTempleBackdrop>(); }

        void Start()
        {
            cam = Camera.main;
            if (cam == null) { enabled = false; return; }
            var res = KalingaTempleBuilder.Build(transform, "MenuTemple", Site, 2.1f);
            foreach (var c in res.root.GetComponentsInChildren<Collider>()) Destroy(c);
            b = res.footprint;
            var flag = new GameObject("MenuTempleFlag").AddComponent<WavingFlag>();
            flag.transform.SetParent(res.root, false);
            flag.transform.position = res.towerTop;
            var ground = Village.Box(transform, "MenuGround", Vector3.zero, new Vector3(900f, 0.2f, 900f), "red_sandstone_pavement", 3.2f, false); ground.transform.position = new Vector3(Site.x, -0.1f, Site.z);
            // golden hour: one low warm sun from the front-left, warm haze
            Light sun = RenderSettings.sun; if (sun == null) foreach (var l in FindObjectsByType<Light>()) if (l.type == LightType.Directional) sun = l;
            if (sun == null) { sun = new GameObject("MenuSun").AddComponent<Light>(); sun.type = LightType.Directional; }
            sun.transform.rotation = Quaternion.Euler(14f, 205f, 0f); sun.color = new Color(1f, 0.74f, 0.48f); sun.intensity = 1.9f; sun.shadows = LightShadows.Soft;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = new Color(0.86f, 0.62f, 0.42f); RenderSettings.fogDensity = 0.0028f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(0.62f, 0.56f, 0.6f); RenderSettings.ambientEquatorColor = new Color(0.6f, 0.44f, 0.34f); RenderSettings.ambientGroundColor = new Color(0.22f, 0.15f, 0.12f);
            var old = cam.GetComponent<MenuCameraController>(); if (old != null) old.enabled = false;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 900f);
            Ready = true; Drift();
        }

        void LateUpdate() { if (Ready) Drift(); }
        void Drift()
        {
            t += Time.unscaledDeltaTime;
            float sway = Mathf.Sin(t * 0.045f) * 34f, rise = 9f + 5f * (0.5f + 0.5f * Mathf.Sin(t * 0.03f));
            cam.transform.position = new Vector3(b.center.x - 55f + sway, rise, b.max.z + 105f);   // the menu panel sits on the left, so the temple is framed to the right
            cam.transform.LookAt(new Vector3(b.center.x + 18f, b.size.y * 0.42f, b.center.z));
        }
    }
}
