using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.World
{
    /// <summary>Small transform sway for trees, shrubs, flags and cloth, driven by the weather's wind.</summary>
    public sealed class WindSway : MonoBehaviour
    {
        public float amount = 1f;
        private Quaternion rest; private float phase;
        private void Start() { rest = transform.localRotation; phase = (transform.position.x * 0.37f + transform.position.z * 0.61f) % 6.28f; }
        private void Update()
        {
            float w = WeatherManager.Instance != null ? WeatherManager.Instance.Wind : 0.2f;
            float t = Time.time;
            float a = (Mathf.Sin(t * (0.9f + w) + phase) + 0.5f * Mathf.Sin(t * 2.3f + phase * 1.7f)) * (0.6f + 4.5f * w) * amount;
            transform.localRotation = rest * Quaternion.Euler(a, 0f, a * 0.6f);
        }
    }

    /// <summary>
    /// Rain and storm. Camera-following GPU rain streaks, fine mist, ground splashes and ripples, drips from
    /// roof edges, puddles, wet-stone look, lightning with thunder delayed by distance, wind sway, layered rain audio.
    /// Intensity is set per map (and by the story); quality comes from Settings (Off / Low / High).
    /// </summary>
    public sealed class WeatherManager : MonoBehaviour
    {
        public static WeatherManager Instance { get; private set; }
        public float Intensity { get; private set; }
        public float Target { get; private set; }
        public float Wind => 0.15f + Intensity * 0.6f + (Storm ? 0.25f : 0f);
        public bool Storm { get; private set; }
        public bool Interior { get; private set; }
        public int ParticleCount { get; private set; }
        public int LightningCount { get; private set; }
        public bool RainActive => rain != null && rain.isPlaying && rain.emission.rateOverTimeMultiplier > 1f;
        /// <summary>1 = dry, lower when it rains outside (lamp flames gutter).</summary>
        public float FlameFactor => Interior ? 1f : 1f - 0.3f * Intensity;
        public static event System.Action<float> OnThunder; // volume 0..1

        private ParticleSystem rain, mist, splash, ripple, drip;
        private Light flash; private float flashT = -1f, nextLightning; private readonly List<(float time, float vol)> thunder = new List<(float, float)>();
        private AudioSource aLight, aHeavy, aInterior;
        private readonly List<(Material m, Color col, float smooth)> wet = new List<(Material, Color, float)>();
        private float wetApplied = -1f; private Transform puddleRoot; private Material puddleMat; private Color puddleBase;
        private float dripTimer; private Color ambSky, ambEq, ambGround; private bool ambSaved;
        private bool overridden; private float skyBase = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("WeatherManager"); DontDestroyOnLoad(go);
            Instance = go.AddComponent<WeatherManager>();
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += (s, m) => OnScene(s.name);
            GameOptions.OnChanged += () => wetApplied = -1f;
            aLight = Loop("Audio/amb_rain_light"); aHeavy = Loop("Audio/amb_rain_heavy"); aInterior = Loop("Audio/amb_rain_interior");
            OnScene(SceneManager.GetActiveScene().name);
        }

        private AudioSource Loop(string clip)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.clip = Resources.Load<AudioClip>(clip); a.loop = true; a.playOnAwake = false; a.volume = 0f; a.spatialBlend = 0f;
            AudioManager.Route(a, "Ambience");
            return a;
        }

        /// <summary>Default weather per map: drizzle in the courtyard, heavy rain at the outer shrine, storm in the jungle.</summary>
        public static float DefaultFor(string scene)
        {
            return 0f; // dry by default: rain belongs to the final round (set by the fight)
        }

        /// <summary>Story override (for example: storm 1.0 in the final fight, 0 at first light).</summary>
        public void SetTarget(float intensity, bool storm, bool instant = false)
        {
            Target = Mathf.Clamp01(intensity); Storm = storm; overridden = true;
            if (instant) Intensity = Target;
        }

        private void OnScene(string scene)
        {
            Interior = scene == "Sanctum" || scene == "PuzzleWall";
            if (!overridden) { Target = 0f; Storm = false; }
            overridden = false;
            Intensity = Target;
            wet.Clear(); wetApplied = -1f; ambSaved = false; thunder.Clear();
            nextLightning = Time.time + Random.Range(6f, 12f);
            bool game = scene == "Courtyard" || scene == "Sanctum" || scene == "ShrineCrime" || scene == "PuzzleWall" || scene == "JunglePath";
            if (!game) { Target = Intensity = 0f; return; }
            Invoke(nameof(Collect), 0.6f); // after LookTuner made its material instances and decor
        }

        private static bool Wettable(string n)
        {
            n = n.ToLowerInvariant();
            return n.Contains("stone") || n.Contains("sandstone") || n.Contains("leaves") || n.Contains("temple") || n.Contains("floor") || n.Contains("ground") || n.Contains("pav") || n.Contains("wall") ||
                   n.Contains("step") || n.Contains("brick") || n.Contains("rock") || n.Contains("plinth") || n.Contains("carved") || n.Contains("laterite") || n.Contains("courtyard");
        }

        private void Collect()
        {
            wet.Clear();
            var seen = new HashSet<Material>();
            int n = 0;
            foreach (var r in FindObjectsByType<MeshRenderer>())
            {
                if (n > 400) break;
                bool any = false;
                foreach (var m in r.sharedMaterials) if (m != null && Wettable(m.name)) { any = true; break; }
                if (!any) continue;
                foreach (var m in r.materials)
                {
                    if (m == null || !Wettable(m.name) || !m.HasProperty("_BaseColor") || !m.HasProperty("_Smoothness") || !seen.Add(m)) continue;
                    wet.Add((m, m.GetColor("_BaseColor"), m.GetFloat("_Smoothness"))); n++;
                }
            }
            int sway = 0;
            foreach (var t in FindObjectsByType<Transform>())
            {
                if (sway > 80) break;
                string nm = t.name.ToLowerInvariant();
                if (t.GetComponent<WindSway>() != null || t.GetComponent<Collider>() != null) continue;
                bool leaf = nm.Contains("tree") || nm.Contains("shrub") || nm.Contains("bush") || nm.Contains("flag") || nm.Contains("cloth") || nm.Contains("garland") || nm.Contains("vine") || nm.Contains("grass") || nm.Contains("leaf") || nm.Contains("fern");
                if (leaf && t.GetComponent<Renderer>() != null) { t.gameObject.AddComponent<WindSway>(); sway++; }
            }
            BuildPuddles();
            wetApplied = -1f;
        }

        private void BuildPuddles()
        {
            if (puddleRoot != null) Destroy(puddleRoot.gameObject);
            puddleMat = Resources.Load<Material>("FX/Puddle");
            if (puddleMat == null || Interior) return;
            puddleMat = new Material(puddleMat); puddleBase = puddleMat.GetColor("_BaseColor");
            puddleRoot = new GameObject("Weather_Puddles").transform;
            var player = GameObject.FindWithTag("Player");
            Vector3 c = player != null ? player.transform.position : Vector3.zero;
            var rng = new System.Random(7);
            int made = 0;
            for (int i = 0; i < 80 && made < 14; i++)
            {
                Vector3 p = c + new Vector3((float)(rng.NextDouble() * 2 - 1) * 26f, 12f, (float)(rng.NextDouble() * 2 - 1) * 26f);
                if (!Physics.Raycast(p, Vector3.down, out RaycastHit h, 30f, ~0, QueryTriggerInteraction.Ignore) || h.normal.y < 0.97f || h.point.y > c.y + 0.6f) continue;
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>());
                q.name = "Puddle"; q.transform.SetParent(puddleRoot, false);
                q.transform.position = h.point + Vector3.up * 0.012f; q.transform.rotation = Quaternion.Euler(90f, (float)rng.NextDouble() * 360f, 0f);
                float s = 1.4f + (float)rng.NextDouble() * 2.6f; q.transform.localScale = new Vector3(s, s * (0.5f + (float)rng.NextDouble() * 0.5f), 1f);
                var mr = q.GetComponent<MeshRenderer>(); mr.sharedMaterial = puddleMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                made++;
            }
        }

        private ParticleSystem Make(string name, string mat, int max, float life, float size0, float size1, ParticleSystemRenderMode mode)
        {
            var go = new GameObject("Weather_" + name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = max; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = life; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(size0, size1);
            var em = ps.emission; em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = mode; r.sharedMaterial = Resources.Load<Material>("FX/" + mat);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return ps;
        }

        private void Ensure()
        {
            if (rain != null) return;
            rain = Make("Rain", "Rain", 3000, 0.85f, 0.018f, 0.03f, ParticleSystemRenderMode.Stretch);
            var rr = rain.GetComponent<ParticleSystemRenderer>(); rr.velocityScale = 0.05f; rr.lengthScale = 1.2f;
            var sh = rain.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(26f, 0.2f, 26f);
            var v = rain.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World; v.x = 1.5f; v.y = -17f; v.z = 0.8f;

            mist = Make("Mist", "Mist", 60, 9f, 6f, 11f, ParticleSystemRenderMode.Billboard);
            var ms = mist.shape; ms.shapeType = ParticleSystemShapeType.Box; ms.scale = new Vector3(34f, 1.5f, 34f);
            var mv = mist.velocityOverLifetime; mv.enabled = true; mv.space = ParticleSystemSimulationSpace.World; mv.x = 0.5f; mv.y = 0.02f; mv.z = 0.25f;
            var mc = mist.colorOverLifetime; mc.enabled = true; mc.color = Fade();

            splash = Make("Splash", "Splash", 500, 0.28f, 0.04f, 0.09f, ParticleSystemRenderMode.Billboard);
            var ss = splash.shape; ss.shapeType = ParticleSystemShapeType.Circle; ss.radius = 9f; ss.rotation = new Vector3(90f, 0f, 0f);
            var sm = splash.main; sm.startSpeed = 0f; sm.gravityModifier = 1.2f;
            var sv = splash.velocityOverLifetime; sv.enabled = true; sv.space = ParticleSystemSimulationSpace.World; sv.x = 0f; sv.y = 1.6f; sv.z = 0f;

            ripple = Make("Ripple", "Ripple", 120, 0.7f, 0.25f, 0.5f, ParticleSystemRenderMode.HorizontalBillboard);
            var rs = ripple.shape; rs.shapeType = ParticleSystemShapeType.Circle; rs.radius = 8f; rs.rotation = new Vector3(90f, 0f, 0f);
            var rsz = ripple.sizeOverLifetime; rsz.enabled = true; rsz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.15f, 1f, 1f));
            var rc = ripple.colorOverLifetime; rc.enabled = true; rc.color = Fade();

            drip = Make("Drip", "Drip", 150, 1.6f, 0.02f, 0.03f, ParticleSystemRenderMode.Stretch);
            var dr = drip.GetComponent<ParticleSystemRenderer>(); dr.velocityScale = 0.03f; dr.lengthScale = 1f;
            var dm = drip.main; dm.gravityModifier = 1f;

            flash = new GameObject("Weather_Lightning").AddComponent<Light>(); flash.transform.SetParent(transform, false);
            flash.type = LightType.Directional; flash.color = new Color(0.8f, 0.87f, 1f); flash.intensity = 0f; flash.shadows = LightShadows.None;
            flash.transform.rotation = Quaternion.Euler(55f, 30f, 0f);
            foreach (var p in new[] { rain, mist, splash, ripple, drip }) p.Play();
        }

        private static ParticleSystem.MinMaxGradient Fade()
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private void Update()
        {
            int q = GameOptions.WeatherQuality;
            Intensity = Mathf.MoveTowards(Intensity, Target, Time.deltaTime * 0.12f);
            var cam = Camera.main;
            bool on = q > 0 && cam != null && (Intensity > 0.01f || Target > 0.01f || rain != null);
            if (!on) { if (rain != null) SetRates(0f, 0, null); Audio(0f); return; }
            Ensure();
            Transform anchor = cam.transform;
            var player = GameObject.FindWithTag("Player");
            Vector3 feet = player != null ? player.transform.position : anchor.position + Vector3.down * 1.6f;
            SetRates(Interior ? 0f : Intensity, q, anchor);
            rain.transform.position = anchor.position + anchor.forward * 6f + Vector3.up * 9f;
            mist.transform.position = feet + Vector3.up * 1.2f;
            splash.transform.position = feet + Vector3.up * 0.04f;
            ripple.transform.position = feet + Vector3.up * 0.025f;
            Drips(feet, q);
            Lightning(q);
            Wetness(q);
            Audio(q > 0 ? Intensity : 0f);
            ParticleCount = rain.particleCount + mist.particleCount + splash.particleCount + ripple.particleCount + drip.particleCount;
        }

        private void SetRates(float k, int q, Transform cam)
        {
            float hi = q >= 2 ? 1f : 0.4f;
            var e = rain.emission; e.rateOverTimeMultiplier = k * 3200f * hi;
            var m = rain.main; m.maxParticles = q >= 2 ? 3000 : 1200;
            var v = rain.velocityOverLifetime; v.xMultiplier = 1.5f + Wind * 5f;
            e = mist.emission; e.rateOverTimeMultiplier = q >= 2 ? k * 5f : 0f;
            e = splash.emission; e.rateOverTimeMultiplier = k * 520f * hi;
            e = ripple.emission; e.rateOverTimeMultiplier = q >= 2 ? k * 60f : 0f;
        }

        private void Drips(Vector3 feet, int q)
        {
            if (q < 2) return;
            dripTimer -= Time.deltaTime;
            float rate = Interior ? 0.12f : Mathf.Lerp(0.5f, 0.04f, Intensity);
            if (dripTimer > 0f || (Intensity < 0.05f && !Interior)) return;
            dripTimer = rate;
            Vector2 r = Random.insideUnitCircle * 9f;
            Vector3 p = feet + new Vector3(r.x, 0f, r.y);
            var ep = new ParticleSystem.EmitParams { velocity = Vector3.down * 0.5f };
            if (Interior)
            {   // damp ceiling: a drop falls from wherever there is stone above
                if (!Physics.Raycast(p + Vector3.up * 1.2f, Vector3.up, out RaycastHit up, 14f, ~0, QueryTriggerInteraction.Ignore)) return;
                ep.position = up.point + Vector3.down * 0.05f;
            }
            else
            {   // roof or ledge edge: high hit here, low hit 0.7 m away
                if (!Physics.Raycast(p + Vector3.up * 16f, Vector3.down, out RaycastHit a, 30f, ~0, QueryTriggerInteraction.Ignore) || a.point.y < feet.y + 2.2f) return;
                Vector3 o = p + new Vector3(Random.value > 0.5f ? 0.7f : -0.7f, 0f, Random.value > 0.5f ? 0.7f : -0.7f);
                if (!Physics.Raycast(o + Vector3.up * 16f, Vector3.down, out RaycastHit b, 30f, ~0, QueryTriggerInteraction.Ignore) || b.point.y > a.point.y - 1.5f) return;
                ep.position = new Vector3(o.x, a.point.y - 0.05f, o.z);
            }
            drip.Emit(ep, 1);
        }

        private void Lightning(int q)
        {
            if (!ambSaved) { ambSky = RenderSettings.ambientSkyColor; ambEq = RenderSettings.ambientEquatorColor; ambGround = RenderSettings.ambientGroundColor; ambSaved = true; }
            if (Storm && Intensity > 0.6f && !Interior && Time.time > nextLightning && flashT < 0f)
            {
                flashT = 0f; LightningCount++;
                nextLightning = Time.time + Random.Range(9f, 20f);
                float dist = Random.Range(300f, 2400f);
                thunder.Add((Time.time + dist / 343f, Mathf.Lerp(1f, 0.35f, dist / 2400f)));
                flash.transform.rotation = Quaternion.Euler(Random.Range(40f, 70f), Random.Range(0f, 360f), 0f);
            }
            if (flashT >= 0f)
            {
                flashT += Time.deltaTime;
                float k = flashT < 0.07f ? 1f : flashT < 0.14f ? 0.15f : flashT < 0.22f ? 0.8f : Mathf.Max(0f, 1f - (flashT - 0.22f) / 0.35f) * 0.5f;
                k *= q >= 2 ? 1f : 0.5f;
                flash.intensity = k * 2.6f;
                RenderSettings.ambientSkyColor = ambSky + new Color(0.5f, 0.55f, 0.7f) * k;
                RenderSettings.ambientEquatorColor = ambEq + new Color(0.3f, 0.33f, 0.42f) * k;
                var sky = RenderSettings.skybox; if (sky != null && sky.HasProperty("_Exposure")) { if (skyBase < 0f) skyBase = sky.GetFloat("_Exposure"); sky.SetFloat("_Exposure", skyBase + k * 1.4f); } // sky flash
                if (flashT > 0.6f) { var sk = RenderSettings.skybox; if (sk != null && skyBase >= 0f) sk.SetFloat("_Exposure", skyBase); skyBase = -1f; flashT = -1f; flash.intensity = 0f; RenderSettings.ambientSkyColor = ambSky; RenderSettings.ambientEquatorColor = ambEq; RenderSettings.ambientGroundColor = ambGround; ambSaved = false; }
            }
            for (int i = thunder.Count - 1; i >= 0; i--)
                if (Time.time >= thunder[i].time)
                {
                    float vol = thunder[i].vol; thunder.RemoveAt(i);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(vol > 0.7f ? "sfx_thunder_near" : "sfx_thunder_far", vol);
                    OnThunder?.Invoke(vol);
                }
        }

        /// <summary>Wet stone: darker albedo and higher smoothness as the rain builds. Puddles fade in with it.</summary>
        private void Wetness(int q)
        {
            float w = Interior ? Intensity * 0.35f : Intensity;
            if (Mathf.Abs(w - wetApplied) < 0.04f) return;
            wetApplied = w;
            foreach (var e in wet)
            {
                if (e.m == null) continue;
                Color c = e.col * Mathf.Lerp(1f, 0.72f, w); c.a = e.col.a;
                e.m.SetColor("_BaseColor", c);
                e.m.SetFloat("_Smoothness", Mathf.Lerp(e.smooth, Mathf.Max(e.smooth, 0.78f), w));
            }
            if (puddleMat != null) { var c = puddleBase; c.a = puddleBase.a * Mathf.Clamp01(w * 1.4f); puddleMat.SetColor("_BaseColor", c); }
            if (puddleRoot != null) puddleRoot.gameObject.SetActive(w > 0.08f && q >= 2);
        }

        private void Audio(float k)
        {
            float l = Interior ? 0f : Mathf.Clamp01(k * 2f) * (1f - Mathf.Clamp01((k - 0.5f) * 2f) * 0.5f) * 0.55f;
            float h = Interior ? 0f : Mathf.Clamp01((k - 0.4f) / 0.6f) * 0.7f;
            float i = Interior ? Mathf.Clamp01(k * 1.5f) * 0.5f : 0f;
            Fade(aLight, l); Fade(aHeavy, h); Fade(aInterior, i);
        }

        private static void Fade(AudioSource a, float target)
        {
            if (a == null || a.clip == null) return;
            a.volume = Mathf.MoveTowards(a.volume, target, Time.unscaledDeltaTime * 0.5f);
            if (a.volume > 0.005f && !a.isPlaying) a.Play();
            else if (a.volume <= 0.005f && a.isPlaying) a.Stop();
        }
    }
}
