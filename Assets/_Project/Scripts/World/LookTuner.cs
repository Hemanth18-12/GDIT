using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

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

        /// <summary>0 = night (chapters 1-5), 1 = first light (the ending). Set by the story.</summary>
        public static float Dawn;
        /// <summary>0..1 through the last chapter: the horizon slowly brightens in the jungle.</summary>
        public static float DawnProgress;
        public static LookTuner Instance { get; private set; }
        public int ExtrasBuilt { get; private set; }

        private struct Look
        {
            public float exposure, ambient, fog, sunIntensity, skyExposure, bloom, saturation, vignette, fill;
            public Color sky, equator, ground, fogColor, sunColor, skyTint; public Vector2 sunAngles;
        }

        private Volume vol; private DepthOfField dof; private ColorAdjustments grade; private Material skyMat; private string scene = "";
        private float appliedDawn = -1f;

        private void Awake()
        {
            Instance = this;
            SceneManager.sceneLoaded += (s, m) => Apply(s.name);
            EventBus.OnDialogueStart += _ => SetDof(true);
            EventBus.OnDialogueEnd += _ => SetDof(false);
            Apply(SceneManager.GetActiveScene().name);
        }

        private void SetDof(bool on) { if (dof != null) dof.active = on; }

        private void Update()
        {   // the jungle's sky warms as dawn approaches; the ending switches the courtyard to first light
            float d = scene == "JunglePath" ? DawnProgress * 0.45f : Dawn;
            if (Mathf.Abs(d - appliedDawn) > 0.03f && (scene == "JunglePath" || scene == "Courtyard")) Relight(scene);
        }

        private static Color C(float r, float g, float b) => new Color(r, g, b);

        private static Look For(string scene)
        {
            switch (scene)
            {
                case "Sanctum": // dark storage hall: the flashlight and the relit lamps matter here
                    return new Look { exposure = 0.6f, ambient = 0.7f, fog = 0.01f, sky = C(0.33f, 0.3f, 0.28f), equator = C(0.3f, 0.23f, 0.16f), ground = C(0.12f, 0.1f, 0.08f), fogColor = C(0.11f, 0.09f, 0.07f),
                        sunIntensity = 0.15f, sunColor = C(1f, 0.8f, 0.6f), sunAngles = new Vector2(60f, 20f), skyExposure = 0.15f, skyTint = C(0.2f, 0.2f, 0.3f), bloom = 0.45f, saturation = 4f, vignette = 0.3f, fill = 0f };
                case "ShrineCrime": // blue moonlight against warm lamps, in rain
                    return new Look { exposure = 1.35f, ambient = 1.5f, fog = 0.008f, sky = C(0.36f, 0.42f, 0.6f), equator = C(0.34f, 0.32f, 0.36f), ground = C(0.16f, 0.14f, 0.14f), fogColor = C(0.12f, 0.16f, 0.26f),
                        sunIntensity = 1.5f, sunColor = C(0.82f, 0.88f, 1f), sunAngles = new Vector2(42f, 30f), skyExposure = 0.45f, skyTint = C(0.2f, 0.28f, 0.5f), bloom = 0.4f, saturation = 10f, vignette = 0.26f, fill = 0.22f };
                case "PuzzleWall": // torch-lit stone
                    return new Look { exposure = 1.0f, ambient = 1.4f, fog = 0.008f, sky = C(0.24f, 0.19f, 0.15f), equator = C(0.32f, 0.22f, 0.14f), ground = C(0.11f, 0.09f, 0.07f), fogColor = C(0.13f, 0.09f, 0.06f),
                        sunIntensity = 0.3f, sunColor = C(1f, 0.75f, 0.5f), sunAngles = new Vector2(55f, 0f), skyExposure = 0.15f, skyTint = C(0.2f, 0.18f, 0.2f), bloom = 0.45f, saturation = 6f, vignette = 0.28f, fill = 0f };
                case "JunglePath": // moonlit storm, a glowing temple far away
                    return new Look { exposure = 1.35f, ambient = 1.5f, fog = 0.014f, sky = C(0.3f, 0.4f, 0.5f), equator = C(0.24f, 0.34f, 0.3f), ground = C(0.12f, 0.16f, 0.12f), fogColor = C(0.08f, 0.13f, 0.17f),
                        sunIntensity = 1.4f, sunColor = C(0.82f, 0.9f, 1f), sunAngles = new Vector2(48f, -20f), skyExposure = 0.45f, skyTint = C(0.18f, 0.28f, 0.46f), bloom = 0.4f, saturation = 12f, vignette = 0.3f, fill = 0.2f };
                case "Courtyard": // 3:10 AM: moonlight, drizzle and oil lamps
                    return new Look { exposure = 1.4f, ambient = 1.5f, fog = 0.006f, sky = C(0.36f, 0.42f, 0.6f), equator = C(0.36f, 0.32f, 0.34f), ground = C(0.18f, 0.15f, 0.14f), fogColor = C(0.1f, 0.13f, 0.22f),
                        sunIntensity = 1.5f, sunColor = C(0.84f, 0.89f, 1f), sunAngles = new Vector2(38f, -40f), skyExposure = 0.5f, skyTint = C(0.2f, 0.28f, 0.5f), bloom = 0.4f, saturation = 10f, vignette = 0.25f, fill = 0.25f };
                default:
                    return new Look { exposure = 0.6f, ambient = 1f, fog = 0.006f, sky = C(0.6f, 0.68f, 0.85f), equator = C(0.7f, 0.56f, 0.42f), ground = C(0.3f, 0.24f, 0.18f), fogColor = C(0.8f, 0.68f, 0.55f),
                        sunIntensity = 1.2f, sunColor = C(1f, 0.9f, 0.8f), sunAngles = new Vector2(40f, 30f), skyExposure = 1f, skyTint = C(0.5f, 0.5f, 0.5f), bloom = 0.35f, saturation = 6f, vignette = 0.2f, fill = 0.35f };
            }
        }

        /// <summary>Golden first light: low warm sun, long shadows, warm mist (the ending).</summary>
        private static Look DawnLook() => new Look { exposure = 0.8f, ambient = 1.1f, fog = 0.02f, sky = C(0.56f, 0.65f, 0.78f), equator = C(0.91f, 0.72f, 0.54f), ground = C(0.3f, 0.24f, 0.18f), fogColor = C(0.85f, 0.74f, 0.58f),
            sunIntensity = 1.5f, sunColor = C(1f, 0.79f, 0.54f), sunAngles = new Vector2(8f, 115f), skyExposure = 1.25f, skyTint = C(1f, 0.72f, 0.5f), bloom = 0.5f, saturation = 8f, vignette = 0.2f, fill = 0.35f };

        private static Look Blend(Look a, Look b, float t)
        {
            return new Look { exposure = Mathf.Lerp(a.exposure, b.exposure, t), ambient = Mathf.Lerp(a.ambient, b.ambient, t), fog = Mathf.Lerp(a.fog, b.fog, t), sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
                skyExposure = Mathf.Lerp(a.skyExposure, b.skyExposure, t), bloom = Mathf.Lerp(a.bloom, b.bloom, t), saturation = Mathf.Lerp(a.saturation, b.saturation, t), vignette = Mathf.Lerp(a.vignette, b.vignette, t), fill = Mathf.Lerp(a.fill, b.fill, t),
                sky = Color.Lerp(a.sky, b.sky, t), equator = Color.Lerp(a.equator, b.equator, t), ground = Color.Lerp(a.ground, b.ground, t), fogColor = Color.Lerp(a.fogColor, b.fogColor, t), sunColor = Color.Lerp(a.sunColor, b.sunColor, t),
                skyTint = Color.Lerp(a.skyTint, b.skyTint, t), sunAngles = Vector2.Lerp(a.sunAngles, b.sunAngles, t) };
        }

        private Light sun, skyFill;

        private void Apply(string sceneName)
        {
            scene = sceneName; appliedDawn = -1f; skyFill = null; sun = null;
            var vgo = GameObject.Find("LookTunerVolume");
            if (vgo == null) vgo = new GameObject("LookTunerVolume");
            vol = (vgo.TryGetComponent<Volume>(out var __c) ? __c : vgo.AddComponent<Volume>());
            vol.isGlobal = true; vol.priority = 100f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            grade = profile.Add<ColorAdjustments>(true); grade.contrast.Override(10f);
            var bl = profile.Add<Bloom>(true); bl.threshold.Override(1.0f); bl.scatter.Override(0.7f);
            var vg = profile.Add<Vignette>(true); vg.smoothness.Override(0.45f);
            var lgg = profile.Add<LiftGammaGain>(true); // shadows toward indigo, highlights toward amber
            lgg.lift.Override(new Vector4(0.96f, 0.98f, 1.06f, 0f)); lgg.gain.Override(new Vector4(1.05f, 1.0f, 0.93f, 0f));
            var fg = profile.Add<FilmGrain>(true); fg.type.Override(FilmGrainLookup.Thin1); fg.intensity.Override(0.15f); fg.response.Override(0.8f);
            dof = profile.Add<DepthOfField>(true); dof.mode.Override(DepthOfFieldMode.Bokeh); dof.focusDistance.Override(2.6f); dof.aperture.Override(4f); dof.focalLength.Override(70f);
            dof.active = false; // only in dialogue close-ups
            vol.sharedProfile = profile;
            foreach (var cam in Camera.allCameras)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data != null) { data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; }
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            sun = RenderSettings.sun;
            if (sun == null) foreach (var l in FindObjectsByType<Light>()) if (l.type == LightType.Directional && l.name != "Weather_Lightning") { sun = l; break; }
            bool game = scene == "Courtyard" || scene == "Sanctum" || scene == "ShrineCrime" || scene == "PuzzleWall" || scene == "JunglePath";
            if (game && scene != "Sanctum" && scene != "PuzzleWall")
            {
                skyFill = new GameObject("LookTuner_SkyFill").AddComponent<Light>();
                skyFill.type = LightType.Directional; skyFill.shadows = LightShadows.None; skyFill.color = new Color(0.62f, 0.72f, 0.95f);
            }
            if (game && RenderSettings.skybox != null) { skyMat = new Material(RenderSettings.skybox); RenderSettings.skybox = skyMat; } else skyMat = null;
            Relight(scene);

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

            if (game) ArtUpgrade.Apply(scene); // real stone, wood and ground surfaces instead of flat colours
            if (scene == "Courtyard") Dress();
            if (game) { CancelInvoke(nameof(Extras)); Invoke(nameof(Extras), 1.0f); }
        }

        /// <summary>Lighting, sky, fog and grading for the current map and time (night, pre-dawn or first light).</summary>
        private void Relight(string sceneName)
        {
            var look = For(sceneName);
            float d = sceneName == "JunglePath" ? DawnProgress * 0.45f : sceneName == "Courtyard" ? Dawn : 0f;
            appliedDawn = d;
            if (d > 0.001f) look = Blend(look, DawnLook(), d);
            if (grade != null) { grade.postExposure.Override(look.exposure); grade.saturation.Override(look.saturation); }
            if (vol != null && vol.sharedProfile != null)
            {
                if (vol.sharedProfile.TryGet<Bloom>(out var bl)) bl.intensity.Override(look.bloom);
                if (vol.sharedProfile.TryGet<Vignette>(out var vg)) vg.intensity.Override(look.vignette);
            }
            RenderSettings.ambientSkyColor = look.sky * look.ambient;
            RenderSettings.ambientEquatorColor = look.equator * look.ambient;
            RenderSettings.ambientGroundColor = look.ground * look.ambient;
            RenderSettings.fogDensity = look.fog;
            RenderSettings.fogColor = look.fogColor;
            if (sun != null)
            {
                sun.color = look.sunColor; sun.intensity = look.sunIntensity; sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(look.sunAngles.x, look.sunAngles.y, 0f);
                RenderSettings.sun = sun;
            }
            if (skyFill != null) { skyFill.intensity = look.fill; skyFill.transform.rotation = sun != null ? Quaternion.LookRotation(-sun.transform.forward + Vector3.down * 0.6f) : Quaternion.Euler(40f, 210f, 0f); }
            if (skyMat != null)
            {
                if (skyMat.HasProperty("_Exposure")) skyMat.SetFloat("_Exposure", look.skyExposure);
                if (skyMat.HasProperty("_SkyTint")) skyMat.SetColor("_SkyTint", look.skyTint);
                if (skyMat.HasProperty("_GroundColor")) skyMat.SetColor("_GroundColor", look.ground * 1.5f);
                if (skyMat.HasProperty("_AtmosphereThickness")) skyMat.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.6f, 1.25f, d));
            }
        }

        // ------------------------------------------------------------------ extra dressing (light shafts, glows, mist, vegetation, torches)
        private static Material Res(string n) => Resources.Load<Material>("FX/" + n);
        private static Vector3 Ground(Vector3 p) => Physics.Raycast(p + Vector3.up * 8f, Vector3.down, out RaycastHit h, 30f, ~0, QueryTriggerInteraction.Ignore) ? h.point : p;
        private static Material Lit(Color c, float smooth = 0.15f, Color? emit = null)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth);
            if (emit.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emit.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            return m;
        }
        private static GameObject Prim(PrimitiveType t, string name, Transform root, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(t); Object.Destroy(g.GetComponent<Collider>()); g.name = name;
            g.transform.SetParent(root, false); g.transform.position = pos; g.transform.localScale = scale; g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
        }

        private static void Shaft(Transform root, Vector3 top, Vector3 dir, float length, float width, Color tint)
        {
            var mat = new Material(Res("Shaft")); mat.SetColor("_BaseColor", tint);
            for (int i = 0; i < 2; i++)
            {   // two crossed quads read as a volume from most angles
                var q = Prim(PrimitiveType.Quad, "LightShaft", root, top + dir.normalized * length * 0.5f, new Vector3(width, length, 1f), mat);
                q.transform.rotation = Quaternion.LookRotation(Vector3.Cross(dir, i == 0 ? Vector3.right : Vector3.forward).normalized, -dir.normalized);
                q.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static ParticleSystem Motes(Transform root, string mat, Vector3 centre, Vector3 box, int count, float size, float life, Color color, float drift)
        {
            var go = new GameObject("LookTuner_" + mat); go.transform.SetParent(root, false); go.transform.position = centre;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = life; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size); main.startColor = color;
            main.maxParticles = count; main.simulationSpace = ParticleSystemSimulationSpace.World; main.prewarm = true;
            var em = ps.emission; em.rateOverTime = count / life;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
            var v = ps.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = new ParticleSystem.MinMaxCurve(-drift, drift); v.y = new ParticleSystem.MinMaxCurve(-drift * 0.3f, drift * 0.3f); v.z = new ParticleSystem.MinMaxCurve(-drift, drift);
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Res(mat); r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play(); return ps;
        }

        private void Extras()
        {
            if (GameObject.Find("LookTuner_Extras") != null) return;
            var root = new GameObject("LookTuner_Extras").transform;
            var player = GameObject.FindWithTag("Player");
            Vector3 p = player != null ? player.transform.position : Vector3.zero, f = player != null ? player.transform.forward : Vector3.forward, right = Vector3.Cross(Vector3.up, f);
            int n = 0;
            if (scene == "Courtyard") Village.Build(root);

            // a soft glow on every warm lamp
            var glow = new GameObject("LookTuner_LampGlows"); glow.transform.SetParent(root, false);
            var gps = glow.AddComponent<ParticleSystem>(); gps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var gm = gps.main; gm.loop = false; gm.startLifetime = 100000f; gm.startSpeed = 0f; gm.maxParticles = 64; gm.simulationSpace = ParticleSystemSimulationSpace.World;
            var ge = gps.emission; ge.enabled = false;
            glow.GetComponent<ParticleSystemRenderer>().sharedMaterial = Res("Glow");
            foreach (var l in FindObjectsByType<Light>())
            {
                if (n >= 48 || l.type != LightType.Point || l.range > 12f || l.color.b > l.color.r * 0.75f || l.transform.root.CompareTag("Player")) continue;
                gps.Emit(new ParticleSystem.EmitParams { position = l.transform.position, startSize = 1.3f, startColor = new Color(1f, 0.65f, 0.3f, 0.45f), startLifetime = 100000f }, 1); n++;
                if (l.GetComponent<LightFlicker>() == null) l.gameObject.AddComponent<LightFlicker>().Configure(l.intensity * 0.8f, l.intensity * 1.15f, 9f);
            }

            var stone = Lit(new Color(0.42f, 0.33f, 0.25f)); var bark = Lit(new Color(0.2f, 0.14f, 0.09f)); var leaf = Lit(new Color(0.09f, 0.2f, 0.1f)); var leaf2 = Lit(new Color(0.12f, 0.26f, 0.12f));
            switch (scene)
            {
                case "Courtyard":
                    Motes(root, "Mist", Ground(p) + Vector3.up * 0.8f, new Vector3(50f, 1.2f, 50f), 26, 12f, 14f, new Color(0.8f, 0.8f, 0.9f, 0.07f + 0.12f * Dawn), 0.3f);
                    if (Dawn > 0.5f) for (int i = 0; i < 4; i++) Shaft(root, p + f * (6f + i * 5f) + right * (i % 2 == 0 ? -6f : 7f) + Vector3.up * 14f, new Vector3(0.75f, -1f, 0.2f), 18f, 3.5f, new Color(1f, 0.8f, 0.5f, 0.1f));
                    // wet petals on the paving
                    var petal = Lit(new Color(0.75f, 0.1f, 0.08f)); var petal2 = Lit(new Color(0.95f, 0.65f, 0.1f));
                    var rng = new System.Random(3);
                    for (int i = 0; i < 60; i++)
                    {
                        Vector3 q = Ground(p + new Vector3((float)(rng.NextDouble() * 2 - 1) * 12f, 0f, (float)(rng.NextDouble() * 2 - 1) * 12f));
                        var g = Prim(PrimitiveType.Quad, "Petal", root, q + Vector3.up * 0.015f, Vector3.one * 0.09f, i % 3 == 0 ? petal2 : petal); g.transform.rotation = Quaternion.Euler(90f, rng.Next(360), 0f);
                        g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    }
                    break;
                case "Sanctum":
                    for (int i = 0; i < 3; i++) Shaft(root, p + f * (3f + i * 3f) + right * (i - 1) * 3f + Vector3.up * 7f, new Vector3(0.35f, -1f, 0.15f), 8f, 1.4f, new Color(1f, 0.85f, 0.6f, 0.09f));
                    Motes(root, "Dust", p + f * 5f + Vector3.up * 2f, new Vector3(14f, 4f, 14f), 80, 0.05f, 9f, new Color(1f, 0.9f, 0.75f, 0.5f), 0.08f);
                    break;
                case "ShrineCrime":
                    Motes(root, "Mist", Ground(p) + Vector3.up * 0.7f, new Vector3(40f, 1f, 40f), 22, 10f, 14f, new Color(0.7f, 0.78f, 0.95f, 0.08f), 0.35f);
                    for (int i = 0; i < 4; i++)
                    {   // stone lamp towers: warm pools in the blue night
                        Vector3 q = Ground(p + f * (4f + i * 5f) + right * (i % 2 == 0 ? -4.5f : 4.5f));
                        Prim(PrimitiveType.Cylinder, "LampTower", root, q + Vector3.up * 0.9f, new Vector3(0.35f, 0.9f, 0.35f), stone);
                        Prim(PrimitiveType.Sphere, "LampTowerFlame", root, q + Vector3.up * 2.0f, Vector3.one * 0.28f, Lit(new Color(0.3f, 0.1f, 0.02f), 0.1f, new Color(4f, 1.6f, 0.4f)));
                        var l = new GameObject("LampTowerLight").AddComponent<Light>(); l.transform.SetParent(root, false); l.transform.position = q + Vector3.up * 2.2f;
                        l.type = LightType.Point; l.range = 8f; l.intensity = 2.4f; l.color = new Color(1f, 0.66f, 0.3f); l.shadows = LightShadows.None; l.gameObject.AddComponent<LightFlicker>().Configure(1.9f, 2.7f, 9f);
                        gps.Emit(new ParticleSystem.EmitParams { position = l.transform.position, startSize = 1.5f, startColor = new Color(1f, 0.65f, 0.3f, 0.45f), startLifetime = 100000f }, 1);
                    }
                    break;
                case "PuzzleWall":
                    Motes(root, "Dust", p + f * 5f + Vector3.up * 2.2f, new Vector3(14f, 5f, 14f), 90, 0.05f, 9f, new Color(1f, 0.85f, 0.65f, 0.5f), 0.06f);
                    for (int i = 0; i < 4; i++)
                    {   // wall torches
                        Vector3 q = Ground(p + f * (2f + (i / 2) * 6f) + right * (i % 2 == 0 ? -5f : 5f));
                        Prim(PrimitiveType.Cylinder, "TorchPole", root, q + Vector3.up * 1.1f, new Vector3(0.08f, 1.1f, 0.08f), bark);
                        Prim(PrimitiveType.Sphere, "TorchFlame", root, q + Vector3.up * 2.35f, new Vector3(0.22f, 0.34f, 0.22f), Lit(new Color(0.3f, 0.1f, 0.02f), 0.1f, new Color(5f, 1.8f, 0.4f)));
                        var l = new GameObject("TorchLight").AddComponent<Light>(); l.transform.SetParent(root, false); l.transform.position = q + Vector3.up * 2.5f;
                        l.type = LightType.Point; l.range = 10f; l.intensity = 3f; l.color = new Color(1f, 0.6f, 0.26f); l.shadows = i == 0 ? LightShadows.Soft : LightShadows.None; l.gameObject.AddComponent<LightFlicker>().Configure(2.3f, 3.4f, 11f);
                        gps.Emit(new ParticleSystem.EmitParams { position = l.transform.position, startSize = 1.6f, startColor = new Color(1f, 0.6f, 0.25f, 0.5f), startLifetime = 100000f }, 1);
                    }
                    break;
                case "JunglePath":
                    Motes(root, "Mist", Ground(p + f * 14f) + Vector3.up * 0.9f, new Vector3(60f, 1.6f, 60f), 34, 13f, 14f, new Color(0.65f, 0.78f, 0.9f, 0.1f), 0.4f);
                    for (int i = 0; i < 3; i++) Shaft(root, p + f * (8f + i * 9f) + right * (i - 1) * 5f + Vector3.up * 13f, new Vector3(-0.3f, -1f, 0.25f), 15f, 2.6f, new Color(0.6f, 0.75f, 1f, 0.07f));
                    var r2 = new System.Random(11);
                    for (int i = 0; i < 26; i++)
                    {   // trees, ferns and vines on both sides of the path
                        float side = i % 2 == 0 ? -1f : 1f; float along = -6f + i * 2.6f; float off = 7f + (float)r2.NextDouble() * 9f;
                        Vector3 q = Ground(p + f * along + right * side * off); float hgt = 5f + (float)r2.NextDouble() * 4f;
                        Prim(PrimitiveType.Cylinder, "JungleTrunk", root, q + Vector3.up * hgt * 0.5f, new Vector3(0.45f, hgt * 0.5f, 0.45f), bark);
                        var crown = Prim(PrimitiveType.Sphere, "JungleTreeCrown", root, q + Vector3.up * (hgt + 0.8f), new Vector3(4.5f, 3f, 4.5f), i % 3 == 0 ? leaf2 : leaf); crown.AddComponent<WindSway>().amount = 0.4f;
                        var vine = Prim(PrimitiveType.Cube, "JungleVine", root, q + Vector3.up * (hgt - 1.4f) + right * -side * 1.2f, new Vector3(0.05f, 2.8f, 0.05f), leaf2); vine.AddComponent<WindSway>().amount = 1.5f;
                        Vector3 fq = Ground(p + f * (along + 1f) + right * side * (off - 3.5f));
                        var fern = Prim(PrimitiveType.Sphere, "JungleFern", root, fq + Vector3.up * 0.3f, new Vector3(1.5f, 0.7f, 1.5f), leaf2); fern.AddComponent<WindSway>().amount = 1f;
                    }
                    // the glowing temple far ahead: the place they are running to
                    Vector3 far = Ground(p + f * 85f + right * 14f) + Vector3.up * 3f;
                    var lit = Lit(new Color(0.5f, 0.3f, 0.16f), 0.1f, new Color(0.9f, 0.4f, 0.1f));
                    for (int i = 0; i < 7; i++) Prim(PrimitiveType.Cube, "DistantTemple", root, far + Vector3.up * (i * 3.2f), new Vector3(14f - i * 1.7f, 3.2f, 14f - i * 1.7f), i % 2 == 0 ? stone : lit);
                    Prim(PrimitiveType.Sphere, "DistantTempleCrown", root, far + Vector3.up * 23.5f, new Vector3(3.5f, 2.2f, 3.5f), lit);
                    var tl = new GameObject("DistantTempleGlow").AddComponent<Light>(); tl.transform.SetParent(root, false); tl.transform.position = far + Vector3.up * 8f - f * 9f;
                    tl.type = LightType.Point; tl.range = 45f; tl.intensity = 6f; tl.color = new Color(1f, 0.62f, 0.28f); tl.shadows = LightShadows.None;
                    gps.Emit(new ParticleSystem.EmitParams { position = far + Vector3.up * 9f - f * 8f, startSize = 30f, startColor = new Color(1f, 0.6f, 0.25f, 0.35f), startLifetime = 100000f }, 1);
                    break;
            }
            ArtUpgrade.Scatter(root, scene, p, f);
            gps.Play();
            ExtrasBuilt = root.childCount;
        }

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
            var bark = ArtUpgrade.Mat("bark_brown_02") ?? M(new Color(0.32f, 0.22f, 0.14f)); var leaf = M(new Color(0.22f, 0.42f, 0.18f)); var leaf2 = M(new Color(0.3f, 0.5f, 0.2f));
            var brass = M(new Color(0.85f, 0.65f, 0.3f)); var cloth = M(new Color(0.85f, 0.35f, 0.15f)); var cloth2 = M(new Color(0.95f, 0.75f, 0.25f)); var wood = ArtUpgrade.Mat("weathered_planks") ?? M(new Color(0.45f, 0.3f, 0.18f));
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
