using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LegacyOfTemples.World
{
    /// <summary>
    /// Runtime "look" pass for every map (no scene files are edited): ACES tonemapping, exposure, mild bloom,
    /// vignette, trilight ambient fill, soft blue sky fill light, rim light on the hero, light fog,
    /// warmer/lighter weathered sandstone, and simple low-cost scenery where the world looks empty.
    /// </summary>
    public sealed class LookTuner : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("LookTuner");
            DontDestroyOnLoad(go);
            go.AddComponent<LookTuner>();
        }

        private struct Look { public float exposure, ambient, fog; public Color sky, equator, ground, fogColor; }

        private void Awake() { SceneManager.sceneLoaded += (s, m) => Apply(s.name); Apply(SceneManager.GetActiveScene().name); }

        private static Look For(string scene)
        {
            switch (scene)
            {
                case "Sanctum": // dark interior: the flashlight matters here
                    return new Look { exposure = 0.1f, ambient = 0.35f, fog = 0.012f, sky = new Color(0.35f, 0.32f, 0.3f), equator = new Color(0.3f, 0.24f, 0.18f), ground = new Color(0.12f, 0.1f, 0.08f), fogColor = new Color(0.12f, 0.1f, 0.09f) };
                case "ShrineCrime":
                    return new Look { exposure = 0.5f, ambient = 0.9f, fog = 0.01f, sky = new Color(0.55f, 0.62f, 0.78f), equator = new Color(0.45f, 0.42f, 0.42f), ground = new Color(0.2f, 0.18f, 0.17f), fogColor = new Color(0.45f, 0.5f, 0.6f) };
                case "PuzzleWall":
                    return new Look { exposure = 0.55f, ambient = 0.9f, fog = 0.008f, sky = new Color(0.7f, 0.6f, 0.5f), equator = new Color(0.55f, 0.44f, 0.32f), ground = new Color(0.22f, 0.18f, 0.14f), fogColor = new Color(0.5f, 0.42f, 0.32f) };
                case "JunglePath":
                    return new Look { exposure = 0.6f, ambient = 1.0f, fog = 0.018f, sky = new Color(0.45f, 0.58f, 0.75f), equator = new Color(0.3f, 0.42f, 0.32f), ground = new Color(0.14f, 0.18f, 0.12f), fogColor = new Color(0.35f, 0.45f, 0.42f) };
                case "Courtyard":
                    return new Look { exposure = 0.75f, ambient = 1.1f, fog = 0.006f, sky = new Color(0.6f, 0.68f, 0.85f), equator = new Color(0.75f, 0.58f, 0.42f), ground = new Color(0.3f, 0.24f, 0.18f), fogColor = new Color(0.85f, 0.7f, 0.55f) };
                default:
                    return new Look { exposure = 0.6f, ambient = 1f, fog = 0.006f, sky = new Color(0.6f, 0.68f, 0.85f), equator = new Color(0.7f, 0.56f, 0.42f), ground = new Color(0.3f, 0.24f, 0.18f), fogColor = new Color(0.8f, 0.68f, 0.55f) };
            }
        }

        private void Apply(string scene)
        {
            var look = For(scene);

            // Post-processing (own global volume, high priority).
            var vgo = GameObject.Find("LookTunerVolume");
            if (vgo == null) vgo = new GameObject("LookTunerVolume");
            var vol = (vgo.TryGetComponent<Volume>(out var __c) ? __c : vgo.AddComponent<Volume>());
            vol.isGlobal = true; vol.priority = 100f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var ca = profile.Add<ColorAdjustments>(true); ca.postExposure.Override(look.exposure); ca.contrast.Override(8f); ca.saturation.Override(6f);
            var bl = profile.Add<Bloom>(true); bl.intensity.Override(0.35f); bl.threshold.Override(1.05f); bl.scatter.Override(0.6f);
            var vg = profile.Add<Vignette>(true); vg.intensity.Override(0.2f); vg.smoothness.Override(0.45f);
            vol.sharedProfile = profile;
            foreach (var cam in Camera.allCameras)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data != null) data.renderPostProcessing = true;
            }

            // Ambient fill + fog.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = look.sky * look.ambient;
            RenderSettings.ambientEquatorColor = look.equator * look.ambient;
            RenderSettings.ambientGroundColor = look.ground * look.ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = look.fog;
            RenderSettings.fogColor = look.fogColor;

            // Soft sky fill light opposite the sun (no shadows, cheap).
            Light sun = RenderSettings.sun;
            if (sun == null) foreach (var l in FindObjectsByType<Light>()) if (l.type == LightType.Directional) { sun = l; break; }
            if (scene != "Sanctum")
            {
                var fill = new GameObject("LookTuner_SkyFill").AddComponent<Light>();
                fill.type = LightType.Directional; fill.shadows = LightShadows.None;
                fill.color = new Color(0.62f, 0.72f, 0.95f); fill.intensity = 0.35f;
                fill.transform.rotation = sun != null ? Quaternion.LookRotation(-sun.transform.forward + Vector3.down * 0.6f) : Quaternion.Euler(40f, 210f, 0f);
                if (sun != null && scene == "Courtyard") { sun.color = new Color(1f, 0.82f, 0.62f); sun.intensity = Mathf.Max(sun.intensity, 1.4f); }
            }

            // Rim light on the hero so he never reads as a black silhouette.
            var player = GameObject.FindWithTag("Player");
            if (player != null && player.transform.Find("HeroRimLight") == null)
            {
                var rim = new GameObject("HeroRimLight").AddComponent<Light>();
                rim.transform.SetParent(player.transform, false);
                rim.transform.localPosition = new Vector3(0.4f, 2.0f, -1.2f);
                rim.type = LightType.Point; rim.range = 4f; rim.intensity = 2.2f; rim.color = new Color(1f, 0.92f, 0.8f); rim.shadows = LightShadows.None;
                var key = new GameObject("HeroFaceFill").AddComponent<Light>();
                key.transform.SetParent(player.transform, false);
                key.transform.localPosition = new Vector3(-0.3f, 1.8f, 1.4f);
                key.type = LightType.Point; key.range = 3f; key.intensity = 1.2f; key.color = new Color(1f, 0.85f, 0.7f); key.shadows = LightShadows.None;
            }

            LightenStone();
            if (scene == "Courtyard") Dress();
        }

        /// <summary>Weathered warm sandstone: lighten dark red-brown stone materials (instanced, assets untouched).</summary>
        private static void LightenStone()
        {
            var sand = new Color(0.82f, 0.68f, 0.52f);
            foreach (var r in FindObjectsByType<MeshRenderer>())
            {
                var mats = r.sharedMaterials;
                bool any = false;
                foreach (var m in mats) if (m != null && IsStone(m.name)) { any = true; break; }
                if (!any) continue;
                var inst = r.materials;
                foreach (var m in inst)
                {
                    if (m == null || !IsStone(m.name)) continue;
                    Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
                    Color n = Color.Lerp(c, sand, 0.45f); n.a = c.a;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", n); else m.color = n;
                }
            }
        }

        private static bool IsStone(string n)
        {
            n = n.ToLowerInvariant();
            return n.Contains("stone") || n.Contains("sandstone") || n.Contains("temple") || n.Contains("laterite") || n.Contains("plinth") || n.Contains("carved");
        }

        /// <summary>Low-cost scenery for the Courtyard edges: trees, shrubs, lamps, flags and market stalls (no colliders).</summary>
        private static void Dress()
        {
            if (GameObject.Find("LookTuner_Decor") != null) return;
            var root = new GameObject("LookTuner_Decor").transform;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Material M(Color c) { var m = new Material(lit); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.15f); return m; }
            var bark = M(new Color(0.32f, 0.22f, 0.14f)); var leaf = M(new Color(0.22f, 0.42f, 0.18f)); var leaf2 = M(new Color(0.3f, 0.5f, 0.2f));
            var brass = M(new Color(0.85f, 0.65f, 0.3f)); var cloth = M(new Color(0.85f, 0.35f, 0.15f)); var cloth2 = M(new Color(0.95f, 0.75f, 0.25f)); var wood = M(new Color(0.45f, 0.3f, 0.18f));
            GameObject P(PrimitiveType t, Vector3 pos, Vector3 scale, Material m)
            {
                var g = GameObject.CreatePrimitive(t); Object.Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(root, false); g.transform.position = pos; g.transform.localScale = scale;
                g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
            }
            for (int i = 0; i < 6; i++)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = side * (20f + (i % 2) * 3f), z = -10f + i * 7f;
                    P(PrimitiveType.Cylinder, new Vector3(x, 1.6f, z), new Vector3(0.35f, 1.6f, 0.35f), bark);
                    P(PrimitiveType.Sphere, new Vector3(x, 4.0f, z), new Vector3(3.2f, 2.6f, 3.2f), i % 2 == 0 ? leaf : leaf2);
                    P(PrimitiveType.Sphere, new Vector3(x + side * 1.8f, 0.5f, z + 2f), new Vector3(1.4f, 0.9f, 1.4f), leaf2);
                }
            }
            for (int i = 0; i < 4; i++)
            {
                float z = 4f + i * 5f;
                foreach (float side in new[] { -1f, 1f })
                {
                    P(PrimitiveType.Cylinder, new Vector3(side * 8.5f, 1.2f, z), new Vector3(0.12f, 1.2f, 0.12f), brass);
                    var lampLight = new GameObject("DecorLamp").AddComponent<Light>();
                    lampLight.transform.SetParent(root, false); lampLight.transform.position = new Vector3(side * 8.5f, 2.6f, z);
                    lampLight.type = LightType.Point; lampLight.range = 5f; lampLight.intensity = 1.2f; lampLight.color = new Color(1f, 0.7f, 0.35f); lampLight.shadows = LightShadows.None;
                    P(PrimitiveType.Sphere, new Vector3(side * 8.5f, 2.55f, z), Vector3.one * 0.3f, cloth2);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                float x = -6f + i * 6f, z = 26f;
                P(PrimitiveType.Cube, new Vector3(x, 0.5f, z), new Vector3(2.4f, 1f, 1.2f), wood);
                P(PrimitiveType.Cube, new Vector3(x, 2.2f, z), new Vector3(2.8f, 0.08f, 1.6f), i % 2 == 0 ? cloth : cloth2);
                foreach (float dx in new[] { -1.3f, 1.3f }) P(PrimitiveType.Cylinder, new Vector3(x + dx, 1.1f, z + 0.7f), new Vector3(0.06f, 1.1f, 0.06f), wood);
            }
            for (int i = 0; i < 5; i++)
            {
                float x = -12f + i * 6f;
                P(PrimitiveType.Cylinder, new Vector3(x, 3f, 30f), new Vector3(0.06f, 3f, 0.06f), wood);
                P(PrimitiveType.Cube, new Vector3(x + 0.45f, 5.4f, 30f), new Vector3(0.8f, 0.5f, 0.02f), i % 2 == 0 ? cloth : cloth2);
            }
        }
    }
}
