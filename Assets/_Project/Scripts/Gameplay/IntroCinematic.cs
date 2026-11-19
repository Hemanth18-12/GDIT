using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;
using Unity.Cinemachine;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.World;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// The opening, about 68 seconds, in engine: a Timeline (built at start) cuts and blends between Cinemachine
    /// cameras; each camera has its own move (dolly, crane, tracking), depth of field and grade (warm dawn or cold
    /// night). Five beats: temple reveal and title, the robbery (the project's two robbers), the Pujari finds it,
    /// the jeep drives through the village and the investigator steps out, Meera gives directions. Then the camera
    /// hands over to the normal third-person camera and the objective appears. Hold Esc to skip at any time.
    /// </summary>
    public sealed class IntroCinematic : MonoBehaviour
    {
        public static bool Playing { get; private set; }
        /// <summary>True from the first frame of the first shot to the last (what the recorder captures).</summary>
        public static bool Rolling { get; private set; }
        public static bool Finished { get; private set; }
        public static string LastShot { get; private set; } = "";
        public static string LastVoice { get; private set; } = "";
        public const float Length = 68.5f;
        /// <summary>
        /// New Game plays the arrival only (village, jeep, directions): the temple reveal, robbery and discovery are the intro
        /// video that plays when the game starts. "-introVideoPart" plays just that first part (used to record the video).
        /// </summary>
        public static float StartAt = 30.6f, EndAt = Length;
        public static bool VideoPart => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-introVideoPart") >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook() { OpeningCinematic.Prelude = Play; }

        private static IEnumerator Play()
        {
            var go = new GameObject("IntroCinematic"); var ic = go.AddComponent<IntroCinematic>();
            yield return ic.Run();
            Destroy(go);
        }

        // ------------------------------------------------------------------ shots
        private enum Mode { World, Follow }
        private sealed class Shot
        {
            public string name; public float start, end; public bool night; public float blendIn;
            public Mode mode; public Vector3 a, b, lookA, lookB; public Transform rel, focus; public float fov = 40f;
            public CinemachineCamera cam;
            public bool lockPose, cached; public Matrix4x4 pose;   // lockPose: frame the actor where he is when the shot starts, then hold
        }
        private readonly List<Shot> shots = new List<Shot>();
        private PlayableDirector director; private CinemachineBrain brain; private Camera mainCam;
        private PlayerController player; private OrbitCamera orbit;
        private Transform pujari, meera, door, robber1, robber2, loot; private Vector3 meeraHome, pujariHome; private Quaternion meeraRot, pujariRot;
        private Jeep jeep; private Vector3 jeepFrom; private Vector3 jeepStop; private Vector3 tower;
        private VolumeProfile dawn, night;
        private AudioSource music, voice;
        private float t; private bool skipped; private float escHeld;
        /// <summary>Demo menu hook: end the intro now (same safe hand-over as holding Esc).</summary>
        public static bool SkipNow;
        private readonly HashSet<string> fired = new HashSet<string>();
        private string subtitle, speaker; private float subUntil; private UnityEngine.UI.Image fadeImg;

        private IEnumerator Run()
        {
            Playing = true; Finished = false; DawnClock.Hold = true; InputGate.CinematicActive = true;
            yield return null; yield return null;
            float w = 0f; while (VillageLife.Instance == null || VillageLife.NpcCount == 0 && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }   // the village builds itself just after load
            yield return new WaitForSeconds(0.3f);
            if (!Setup()) { Debug.LogWarning("[Intro] could not set up (missing village or camera): skipped"); Finish(); yield break; }
            if (VideoPart) { StartAt = 0f; EndAt = 31.2f; } else { StartAt = 30.6f; EndAt = Length; }
            director.time = StartAt; director.Play(); t = StartAt; Rolling = true;
            if (music != null && music.clip != null) music.time = Mathf.Min(StartAt, music.clip.length - 0.1f);
            while (t < EndAt && !skipped && !OpeningCinematic.SkipRequested)
            {
                t = (float)director.time; Beat(); Move(); Ui();
                var kb = Keyboard.current; var gp = Gamepad.current;
                bool esc = (kb != null && kb.escapeKey.isPressed) || (gp != null && gp.startButton.isPressed);
                escHeld = esc ? escHeld + Time.unscaledDeltaTime : 0f;
                if (escHeld > 1.0f || SkipNow) { skipped = true; SkipNow = false; }
                if (esc) EventBus.EscapeConsumedFrame = Time.frameCount;   // holding Esc must not open the pause menu
                yield return null;
            }
            Finish();
        }

        // ------------------------------------------------------------------ setup
        private bool Setup()
        {
            mainCam = Camera.main; player = FindAnyObjectByType<PlayerController>(); orbit = FindAnyObjectByType<OrbitCamera>();
            if (mainCam == null || player == null || Village.RoadStart == Vector3.zero) return false;
            foreach (var n in FindObjectsByType<NPCDialogueTrigger>())
            {
                if (n.Id == "npc_priest") pujari = n.transform; else if (n.Id == "npc_villager") meera = n.transform;
            }
            foreach (var p in FindObjectsByType<SceneTransitionTrigger>()) if (p.Id == "portal_to_" + Constants.Sanctum) door = p.transform;
            if (meera != null) { meeraHome = meera.position; meeraRot = meera.rotation; }
            if (pujari != null) { pujariHome = pujari.position; pujariRot = pujari.rotation; }
            jeep = Jeep.Instance;
            if (jeep == null) jeep = Jeep.Spawn(Village.RoadStart + new Vector3(2.2f, 0.6f, -2f), Quaternion.LookRotation(Vector3.back));
            jeepStop = new Vector3(Village.Centre.x + 2.2f, Village.Top, Village.ZMax + 30f);
            var temple = GameObject.Find("Temple_Site");
            if (!VideoPart && TempleSite.Instance != null) TempleSite.Instance.SetHidden(true); Debug.Log($"[Intro] temple hidden: {(TempleSite.Instance != null)}");   // the temple is not seen on the approach
            tower = temple != null ? temple.transform.position + Vector3.up * 40f : new Vector3(Village.Centre.x, Village.Top + 18f, Village.Centre.z - 18f);

            // controls locked, player hidden until the jeep stops
            player.enabled = false; if (orbit != null) orbit.enabled = false; Show(player.transform, false);
            if (player.GetComponent<NpcAnimDriver>() == null) player.gameObject.AddComponent<NpcAnimDriver>();
            if (meera != null) Show(meera, false);
            if (pujari != null) Show(pujari, false);
            if (jeep != null) { jeep.transform.position = Village.RoadStart + new Vector3(2.2f, 0.6f, -2f); jeep.transform.rotation = Quaternion.LookRotation(Vector3.back); jeep.BeginScripted(); jeep.Lights.SetHeadLights(true); jeepFrom = jeep.transform.position; jeep.transform.position = JeepPath(0f); jeep.SeatDriver(player.transform, true); Show(player.transform, true); }   // Aryan drives in, visible at the wheel

            // the two robbers
            Vector3 d = door != null ? door.position : Village.Centre + new Vector3(0f, 0f, -12f);
            Vector3 df = DoorOut(d);
            robber1 = Actor("NPC/Robber1_Cine", d + df * 9f + Vector3.right * 7f, d);
            robber2 = Actor("NPC/Robber2_Cine", d + df * 10f + Vector3.right * 8.5f, d);
            if (robber2 != null) { loot = ArtUpgrade.Place(robber2, "brass_pot_01", robber2.position + robber2.right * 0.3f + Vector3.up * 0.95f, 1.6f, 0f, false)?.transform; if (loot != null) loot.gameObject.SetActive(false); }

            BuildProfiles(); BuildShots(d, df);
            // Cinemachine brain on the main camera, Timeline with one Cinemachine shot per camera
            brain = mainCam.GetComponent<CinemachineBrain>() ?? mainCam.gameObject.AddComponent<CinemachineBrain>();
            brain.enabled = true; brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.0f);
            var tl = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = tl.CreateTrack<CinemachineTrack>(null, "Intro Cameras");
            director = gameObject.AddComponent<PlayableDirector>();
            foreach (var s in shots)
            {
                var clip = track.CreateClip<CinemachineShot>(); clip.displayName = s.name;
                clip.start = Mathf.Max(0f, s.start - s.blendIn); clip.duration = s.end - clip.start;
                if (s.blendIn > 0f) clip.blendInDuration = s.blendIn;
                var cs = (CinemachineShot)clip.asset; var key = new PropertyName("intro_" + s.name);
                cs.VirtualCamera.exposedName = key; director.SetReferenceValue(key, s.cam);
            }
            director.playableAsset = tl; director.SetGenericBinding(track, brain);
            director.timeUpdateMode = DirectorUpdateMode.GameTime; director.extrapolationMode = DirectorWrapMode.Hold;

            music = gameObject.AddComponent<AudioSource>(); music.clip = AudioManager.Instance != null ? AudioManager.Instance.GetClip("mus_intro") : null; music.volume = 0.5f; music.spatialBlend = 0f; AudioManager.Route(music, "Music");
            AudioManager.Instance?.StopBGM(); if (music.clip != null) music.Play();
            voice = gameObject.AddComponent<AudioSource>(); voice.spatialBlend = 0f; voice.volume = 1f; voice.priority = 0; voice.bypassListenerEffects = true;   // straight to the speakers: no mixer group can duck it
            return true;
        }

        /// <summary>From the storage door out into the courtyard (the door object's own forward is not reliable).</summary>
        private Vector3 DoorOut(Vector3 d) { Vector3 o = Village.Centre - d; o.y = 0f; return o.sqrMagnitude > 1f ? o.normalized : Vector3.forward; }
        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward; }
        private static void Show(Transform t, bool on) { foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = on; }

        private Transform Actor(string res, Vector3 at, Vector3 lookAt)
        {
            var pf = Resources.Load<GameObject>(res); if (pf == null) { Debug.LogWarning("[Intro] missing " + res); return null; }
            var g = Instantiate(pf, at, Quaternion.LookRotation(Flat(lookAt - at))); g.name = pf.name;
            Show(g.transform, false); return g.transform;
        }

        private void BuildProfiles()
        {
            dawn = ScriptableObject.CreateInstance<VolumeProfile>(); night = ScriptableObject.CreateInstance<VolumeProfile>();
            foreach (var (p, isNight) in new[] { (dawn, false), (night, true) })
            {
                var ca = p.Add<ColorAdjustments>(true);
                ca.postExposure.Override(isNight ? 0.35f : 0.1f); ca.contrast.Override(isNight ? 16f : 14f); ca.saturation.Override(isNight ? -18f : 12f);
                ca.colorFilter.Override(isNight ? new Color(0.72f, 0.82f, 1f) : new Color(1f, 0.97f, 0.94f));
                var wb = p.Add<WhiteBalance>(true); wb.temperature.Override(isNight ? -30f : 6f);
                var vg = p.Add<Vignette>(true); vg.intensity.Override(isNight ? 0.42f : 0.3f); vg.smoothness.Override(0.5f);
                var dof = p.Add<DepthOfField>(true); dof.mode.Override(DepthOfFieldMode.Bokeh); dof.focalLength.Override(55f); dof.aperture.Override(isNight ? 2.8f : 4.5f); dof.focusDistance.Override(8f);
                var bl = p.Add<Bloom>(true); bl.intensity.Override(isNight ? 0.6f : 0.45f); bl.threshold.Override(0.95f);
            }
        }

        private Shot S(string name, float start, float end, bool isNight, Vector3 a, Vector3 b, Vector3 lookA, Vector3 lookB, float fov = 40f, float blend = 0f, Transform rel = null, Transform focus = null)
        {
            var s = new Shot { name = name, start = start, end = end, night = isNight, a = a, b = b, lookA = lookA, lookB = lookB, fov = fov, blendIn = blend, rel = rel, focus = focus, mode = rel != null ? Mode.Follow : Mode.World };
            var g = new GameObject("CM_" + name); g.transform.SetParent(transform, false);
            s.cam = g.AddComponent<CinemachineCamera>(); var lens = s.cam.Lens; lens.FieldOfView = fov; lens.NearClipPlane = 0.1f; lens.FarClipPlane = 1500f; s.cam.Lens = lens;
            var vs = g.AddComponent<CinemachineVolumeSettings>(); vs.Profile = isNight ? night : dawn;
            if (focus != null) { vs.FocusTracking = CinemachineVolumeSettings.FocusTrackingMode.CustomTarget; vs.FocusTarget = focus; }
            else vs.FocusTracking = CinemachineVolumeSettings.FocusTrackingMode.None;
            g.transform.position = a; g.transform.rotation = Quaternion.LookRotation(Flat3(lookA - a));
            shots.Add(s); return s;
        }
        private static Vector3 Flat3(Vector3 v) => v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;

        private void BuildShots(Vector3 d, Vector3 df)
        {
            Vector3 c = Village.Centre, g = Village.GatePos == Vector3.zero ? c + Vector3.forward * 30f : Village.GatePos, r0 = Village.RoadStart, up = Vector3.up;
            Vector3 dr = Vector3.Cross(Vector3.up, df);
            Transform jt = jeep != null ? jeep.transform : null;
            // 01 temple reveal
            S("01a_aerial_village", 0f, 6f, false, r0 + up * 60f + Vector3.forward * 10f, g + up * 38f + Vector3.forward * 45f, tower, tower, 45f);
            S("01b_crane_to_gate", 6f, 11.5f, false, g + up * 24f + Vector3.forward * 12f, g + up * 1.3f + Vector3.forward * 9f, tower, tower + up * 4f, 50f, 1.2f);
            // 02 the robbery (night)
            S("02a_low_tracking", 11.5f, 15f, true, new Vector3(-2.6f, 0.6f, -7.5f), new Vector3(-2.2f, 0.6f, -5.8f), new Vector3(0f, 1.3f, 1.5f), new Vector3(0f, 1.4f, 1.5f), 44f, 0f, robber1, robber1);
            S("02b_closeup_lookout", 15f, 18f, true, new Vector3(0.9f, 1.75f, 2.4f), new Vector3(0.75f, 1.72f, 2.1f), new Vector3(0f, 1.62f, 0f), new Vector3(0f, 1.62f, 0f), 34f, 0f, robber1, robber1).lockPose = true;
            S("02c_high_wide_door", 18f, 21f, true, d + df * 10f + dr * -6f + up * 7f, d + df * 9f + dr * -5f + up * 6f, d + up * 1f, d + df * 3f + up * 1f, 46f);
            S("02d_escape_track", 21f, 24f, true, new Vector3(7.0f, 1.8f, 1.5f), new Vector3(6.6f, 1.8f, 0.5f), new Vector3(0f, 1.2f, 0.8f), new Vector3(0f, 1.2f, 0.8f), 44f, 0f, robber2, robber2);
            // 03 the discovery: the Pujari points at the broken lock, then turns, shocked
            S("03a_pujari_medium", 24f, 28f, true, d + df * 4.6f + dr * 2.4f + up * 1.6f, d + df * 4.1f + dr * 2.0f + up * 1.6f, d + df * 1.2f + up * 1.3f, d + df * 1.2f + up * 1.3f, 40f, 0f, null, pujari);
            S("03b_pujari_closeup", 28f, 31f, true, new Vector3(0.45f, 1.55f, 2.0f), new Vector3(0.35f, 1.52f, 1.7f), new Vector3(0f, 1.42f, 0f), new Vector3(0f, 1.42f, 0f), 32f, 0f, pujari, pujari);
            // 04 the arrival (dawn): jeep-relative shots use local offsets
            S("04a_aerial_follow", 31f, 36f, false, new Vector3(0f, 26f, -26f), new Vector3(0f, 18f, -14f), new Vector3(0f, 0f, 14f), new Vector3(0f, 0f, 10f), 48f, 0f, jt, jt);
            S("04b_low_front", 36f, 40f, false, new Vector3(0.6f, 0.45f, 13f), new Vector3(0.6f, 0.45f, 8f), new Vector3(0f, 1.1f, 0f), new Vector3(0f, 1.1f, 0f), 38f, 0f, jt, jt);
            S("04c_wheel_closeup", 40f, 43f, false, new Vector3(-2.0f, 0.55f, 2.6f), new Vector3(-1.9f, 0.5f, 1.9f), new Vector3(-0.9f, 0.4f, 1.4f), new Vector3(-0.9f, 0.4f, 1.4f), 34f, 0f, jt, null);
            S("04d_side_tracking", 43f, 49f, false, new Vector3(7f, 1.7f, 1f), new Vector3(7f, 1.7f, -1f), new Vector3(0f, 1.1f, 0f), new Vector3(0f, 1.1f, 0f), 42f, 0.8f, jt, jt);
            S("04e_wide_stop", 49f, 53f, false, jeepStop + new Vector3(-9f, 3.5f, -11f), jeepStop + new Vector3(-8f, 3.2f, -9f), jeepStop + up * 1f, jeepStop + up * 1f, 40f, 0.8f);
            Vector3 exit = jeepStop + new Vector3(-1.8f, 0f, 0.4f);
            S("04f_boots_to_face", 53f, 57f, false, exit + new Vector3(-1.4f, 0.25f, -0.9f), exit + new Vector3(-1.5f, 1.55f, -1.2f), exit + up * 0.15f, exit + up * 1.65f, 34f, 0f, null, player.transform);
            // 05 directions at the tea stall, then the hand-over
            Vector3 ts = VillageLife.Instance != null ? VillageLife.Instance.TeaStallStand : jeepStop + new Vector3(-6f, 0f, 3f);
            S("05a_over_shoulder", 57f, 63f, false, ts + new Vector3(2.2f, 1.75f, -1.6f), ts + new Vector3(2.0f, 1.7f, -1.3f), ts + up * 1.5f, ts + up * 1.5f, 36f, 1.0f, null, meera);
            S("05b_two_shot", 63f, 67f, false, ts + new Vector3(1.6f, 1.6f, 2.6f), ts + new Vector3(1.4f, 1.6f, 2.3f), ts + new Vector3(0.9f, 1.4f, 0f), ts + new Vector3(0.9f, 1.4f, 0f), 40f, 0.8f, null, player.transform);
            S("05c_handover", 67f, Length, false, ts + new Vector3(4.2f, 2.1f, 0.6f), ts + new Vector3(4.6f, 2.1f, 0.6f), ts + new Vector3(1.0f, 1.4f, 0f), ts + new Vector3(1.0f, 1.4f, 0f), 50f, 1.2f);
        }

        // ------------------------------------------------------------------ camera moves (each shot's own path, eased)
        private void Move()
        {
            foreach (var s in shots)
            {
                if (t < s.start - s.blendIn - 0.05f || t > s.end + 0.05f) continue;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(s.start - s.blendIn, s.end, t));
                Vector3 p = Vector3.Lerp(s.a, s.b, k), l = Vector3.Lerp(s.lookA, s.lookB, k);
                if (s.mode == Mode.Follow && s.rel != null)
                {
                    if (s.lockPose) { if (!s.cached) { s.pose = s.rel.localToWorldMatrix; s.cached = true; } p = s.pose.MultiplyPoint3x4(p); l = s.pose.MultiplyPoint3x4(l); }
                    else { p = s.rel.TransformPoint(p); l = s.rel.TransformPoint(l); }
                }
                s.cam.transform.position = p;
                if ((l - p).sqrMagnitude > 0.001f) s.cam.transform.rotation = Quaternion.LookRotation(l - p, Vector3.up);
                if (t >= s.start && t < s.end) LastShot = s.name;
            }
        }

        // ------------------------------------------------------------------ beats
        private bool Once(string id, float at) { if (t < at || fired.Contains(id)) return false; fired.Add(id); return at >= StartAt - 0.01f; }   // beats before the start point are skipped, not replayed
        private void Say(string speakerName, string line, string clip, float seconds)
        {
            subtitle = line; speaker = speakerName; subUntil = t + seconds;
            var c = Resources.Load<AudioClip>("Voice/" + clip); if (c != null) { voice.Stop(); voice.clip = c; voice.Play(); }
            LastVoice = clip + (c != null ? $" ({c.length:0.0}s, playing={voice.isPlaying}, listener volume {AudioListener.volume:0.00})" : " MISSING"); Debug.Log("[Intro] voice " + LastVoice);
            if (music != null) music.volume = 0.22f;   // the music ducks under the voice
        }
        private static void Walk(Transform a, Vector3 to, float speed)
        {
            if (a == null) return; Vector3 d = to - a.position; d.y = 0f;
            if (d.magnitude < 0.05f) return;
            a.rotation = Quaternion.Slerp(a.rotation, Quaternion.LookRotation(d), Time.deltaTime * 6f);
            Vector3 np = a.position + d.normalized * Mathf.Min(speed * Time.deltaTime, d.magnitude);
            if (Physics.Raycast(np + Vector3.up * 1.5f, Vector3.down, out RaycastHit h, 4f, ~0, QueryTriggerInteraction.Ignore) && h.collider.GetComponentInParent<Animator>() == null && h.collider.GetComponentInParent<Jeep>() == null) np.y = h.point.y;
            a.position = np;
        }

        private void Beat()
        {
            // light: golden hour for the reveal, night for the robbery and the discovery, morning for the arrival (cuts hide the change)
            LookTuner.Dawn = t < 11.5f ? 1f : t < 31f ? 0f : 0.8f;
            Vector3 d = door != null ? door.position : Village.Centre + new Vector3(0f, 0f, -12f), df = DoorOut(d), dr = Vector3.Cross(Vector3.up, df);
            if (Once("bell1", 1f)) AudioManager.Instance?.PlaySFX("sfx_bell_temple", 0.5f);
            if (Once("bell2", 7f)) AudioManager.Instance?.PlaySFX("sfx_bell_temple", 0.35f, 0.9f);
            // 02 robbery
            if (Once("robbers_on", 11.4f)) { if (robber1 != null) Show(robber1, true); if (robber2 != null) Show(robber2, true); }
            if (t > 11.5f && t < 17.5f) { Walk(robber1, d + df * 1.2f + dr * 0.6f, 1.6f); Walk(robber2, d + df * 4.0f + dr * 3.6f, 1.5f); }
            if (t > 15.2f && t < 17.8f && robber1 != null) robber1.rotation = Quaternion.LookRotation(Quaternion.Euler(0f, Mathf.Sin((t - 15.2f) * 2.4f) * 70f, 0f) * -df);   // looks round
            if (Once("inside", 17.9f)) { if (robber1 != null) Show(robber1, false); if (robber2 != null) Show(robber2, false); AudioManager.Instance?.PlaySFX("sfx_door_stone", 0.35f); }
            if (Once("out", 19.2f))
            {
                if (robber1 != null) { robber1.position = d + df * 0.8f; Show(robber1, true); }
                if (robber2 != null) { robber2.position = d + df * 2.6f + dr * 2.6f; Show(robber2, true); if (loot != null) loot.gameObject.SetActive(true); }
            }
            if (t > 19.2f && t < 24.5f) { Walk(robber1, d + df * 5f + dr * 24f, 4.2f); Walk(robber2, d + df * 8.5f + dr * 25f, 4.0f); }
            if (Once("robbers_off", 24.2f)) { if (robber1 != null) robber1.gameObject.SetActive(false); if (robber2 != null) robber2.gameObject.SetActive(false); }
            // 03 discovery
            if (Once("pujari_on", 23.9f) && pujari != null) { pujari.position = d + df * 1.3f + dr * -0.4f; pujari.rotation = Quaternion.LookRotation(-df); Show(pujari, true); }
            if (Once("pujari_point", 25.2f) && pujari != null) { var an = pujari.GetComponentInChildren<Animator>(); if (an != null) an.SetTrigger("Point"); }
            if (Once("pujari_turn", 27.6f) && pujari != null) pujari.rotation = Quaternion.LookRotation(df);   // he turns from the empty hall
            if (Once("pujari_line", 28.3f)) Say("Pujari Ananda", "Something terrible has happened!", "pujari/intro_puj_01_tts", 2.6f);
            if (Once("phone", 30.0f)) AudioManager.Instance?.PlaySFX("sfx_phone_ring", 0.5f);
            // 04 arrival
            if (Once("pujari_home", 31f) && pujari != null) { pujari.position = pujariHome; pujari.rotation = pujariRot; }
            if (jeep != null && t > 31f && t < 53f)
            {   // the arrival is a camera path, not physics: the jeep rolls along a smooth curve, slows and stops (wheels spin, body rides its springs)
                float x = Mathf.Clamp01((t - 31f) / 20.6f), e = 1f - (1f - x) * (1f - x);
                jeep.ScriptedMove(JeepPath(e), Quaternion.LookRotation(Flat(JeepPath(Mathf.Min(1f, e + 0.01f)) - JeepPath(Mathf.Max(0f, e - 0.01f))) + Vector3.back * 1e-3f));
                if (t > 50.5f) jeep.Audio.engineLevel = Mathf.MoveTowards(jeep.Audio.engineLevel, 0f, Time.deltaTime * 0.6f);   // engine fades as it parks
            }
            // parked: the jeep stays kinematic (scripted) until the hand-over, so nothing can shove it
            if (Once("door_open", 52.2f) && jeep != null) StartCoroutine(jeep.DriverDoor(true, 0.6f));
            if (Once("hero_out", 52.7f))
            {
                Vector3 ex = jeep != null ? jeep.DriverExitPoint : jeepStop + new Vector3(-1.8f, 0f, 0f);
                if (jeep != null) jeep.SeatDriver(player.transform, false, ex);
                player.Teleport(ex, Quaternion.LookRotation(Vector3.left)); Show(player.transform, true);
                if (meera != null && VillageLife.Instance != null) { var ts = VillageLife.Instance.TeaStallStand; meera.position = ts + new Vector3(-0.4f, 0f, 0f); meera.rotation = Quaternion.LookRotation(Vector3.right); Show(meera, true); }
            }
            if (Once("door_close", 55.2f) && jeep != null) StartCoroutine(jeep.DriverDoor(false, 0.5f));
            // 05 directions
            Vector3 stand = VillageLife.Instance != null ? VillageLife.Instance.TeaStallStand + new Vector3(1.0f, 0f, 0f) : jeepStop;
            if (t > 55.5f && t < 57.1f && jeep != null) Walk(player.transform, jeep.transform.position + jeep.transform.forward * 3.0f + Vector3.left * 0.0f, 1.6f);   // round the front of the jeep, not through it
            else if (t >= 57.1f && t < 58.8f) Walk(player.transform, stand, 1.6f);
            if (t > 58.8f && t < 67f && meera != null) { Vector3 to = meera.position - player.transform.position; to.y = 0f; if (to.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 4f); }
            if (Once("ask", 58.6f)) Say("Aryan", "Excuse me, could you tell me the way to Jagannath Temple?", "aryan/intro_ary_01_tts", 3.0f);
            if (Once("meera_gesture", 61.6f) && meera != null) { var an = meera.GetComponentInChildren<Animator>(); if (an != null) { an.SetTrigger("Pray"); an.SetBool("IsTalking", true); } }
            if (Once("answer", 61.7f)) Say("Meera", "Go straight along this road and follow the old stone entrance. The temple is at the end.", "meera/intro_mee_01_tts", 4.6f);
            if (Once("thanks", 66.4f)) { Say("Aryan", "Thank you.", "aryan/intro_ary_02_tts", 1.6f); if (meera != null) { var an = meera.GetComponentInChildren<Animator>(); if (an != null) an.SetBool("IsTalking", false); } }
            if (t > subUntil && music != null) music.volume = Mathf.MoveTowards(music.volume, 0.5f, Time.deltaTime * 0.4f);
        }

        private void Ui()
        {
            float fade = StartAt > 0f && t < StartAt + 1.2f ? 1f - (t - StartAt) / 1.2f : t > EndAt - 0.8f && EndAt < Length ? (t - (EndAt - 0.8f)) / 0.8f : t < 1.5f ? 1f - t / 1.5f : (t > 11.1f && t < 11.9f ? 1f - Mathf.Abs(t - 11.5f) / 0.4f : (t > 30.6f && t < 31.4f ? 1f - Mathf.Abs(t - 31f) / 0.4f : 0f));
            if (fadeImg == null)
            {
                var cv = new GameObject("IntroFade").AddComponent<Canvas>(); cv.transform.SetParent(transform, false); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 700;
                fadeImg = new GameObject("Black", typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>(); fadeImg.transform.SetParent(cv.transform, false); fadeImg.raycastTarget = false;
                var rt = fadeImg.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            fadeImg.color = new Color(0f, 0f, 0f, Mathf.Clamp01(fade));
            {   // the title: antique gold Cinzel straight on the picture, no box
                float a = t > 6.5f && t < 11f ? Mathf.Clamp01((t - 6.5f) / 1.2f) * Mathf.Clamp01((11f - t) / 0.8f) : 0f;
                if (title == null) MakeTitle();
                title.alpha = a; subTitle.alpha = a * 0.9f;
                title.characterSpacing = Mathf.Lerp(18f, 6f, Mathf.Clamp01((t - 6.5f) / 4f));
            }
            if (t > 11.6f && t < 14f) ScreenText.Panel("intro_card", null, "<color=#5CE1E6>3:10 AM</color>   <color=#FFF4D6>The storage hall</color>", PanelAnchor.BottomLeft, 520f, null, false, 80);
            if (t > 31.2f && t < 34f) ScreenText.Panel("intro_card", null, "<color=#FFC857>First light</color>   <color=#FFF4D6>Suryapur village</color>", PanelAnchor.BottomLeft, 560f, null, false, 80);
            if (t < subUntil && !string.IsNullOrEmpty(subtitle) && PlayerPrefs.GetInt("Settings_Subtitles", 1) == 1)
                ScreenText.Panel("intro_sub", null, $"<color=#FFC857><b>{speaker}</b></color>  {subtitle}", PanelAnchor.BottomCenter, 1400f, null, false, 85);
            string ring = escHeld > 0.05f ? $"  <color=#FFC857>{Mathf.RoundToInt(Mathf.Clamp01(escHeld) * 100)}%</color>" : "";
            if (!VideoPart) ScreenText.Panel("intro_skip", null, $"HOLD {ScreenText.Key("Esc")} TO SKIP{ring}", PanelAnchor.TopRight, 470f, null, false, 96);   // the video has its own
        }

        private TMPro.TextMeshProUGUI title, subTitle; private static TMPro.TMP_FontAsset cinzel;
        private void MakeTitle()
        {
            if (cinzel == null) { var f = Resources.Load<Font>("Fonts/Cinzel"); if (f != null) cinzel = TMPro.TMP_FontAsset.CreateFontAsset(f, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, TMPro.AtlasPopulationMode.Dynamic, true); }
            if (fadeImg == null) Ui();
            var cv = fadeImg.canvas.transform;
            TMPro.TextMeshProUGUI T(string n, float size, float y, Color c)
            {
                var tx = new GameObject(n, typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>(); tx.transform.SetParent(cv, false);
                if (cinzel != null) tx.font = cinzel; tx.fontSize = size; tx.color = c; tx.alignment = TMPro.TextAlignmentOptions.Center; tx.raycastTarget = false;
                tx.textWrappingMode = TMPro.TextWrappingModes.NoWrap; tx.outlineWidth = 0.15f; tx.outlineColor = new Color32(40, 22, 4, 255);
                var r = tx.rectTransform; r.anchorMin = new Vector2(0f, 0.5f); r.anchorMax = new Vector2(1f, 0.5f); r.sizeDelta = new Vector2(0f, size * 1.4f); r.anchoredPosition = new Vector2(0f, y);
                tx.enableVertexGradient = true; return tx;
            }
            var sc = cv.GetComponent<UnityEngine.UI.CanvasScaler>() ?? cv.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            sc.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            title = T("IntroTitle", 104f, 40f, Color.white); title.text = "LEGACY OF THE TEMPLE"; title.fontStyle = TMPro.FontStyles.Bold;
            title.colorGradient = new TMPro.VertexGradient(new Color(1f, 0.9f, 0.55f), new Color(1f, 0.9f, 0.55f), new Color(0.78f, 0.52f, 0.16f), new Color(0.78f, 0.52f, 0.16f));
            subTitle = T("IntroSubtitle", 34f, -48f, new Color(1f, 0.95f, 0.84f)); subTitle.text = "A HERITAGE MYSTERY"; subTitle.characterSpacing = 30f; subTitle.enableVertexGradient = false;
        }

        // ------------------------------------------------------------------ hand-over (also the skip path: everything ends in the same safe state)
        /// <summary>Arrival route (0..1): a gentle S-bend down the lane to the stop, standing on the ground.</summary>
        private Vector3 JeepPath(float u)
        {
            Vector3 p = Vector3.Lerp(jeepFrom, jeepStop, u);
            p.x = Village.RoadPoint(p.z).x + (jeepStop.x - Village.Centre.x);   // keep to the left lane of the spline road through its bend
            var hits = Physics.RaycastAll(p + Vector3.up * 4f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            foreach (var h in hits) if (h.collider.GetComponentInParent<Jeep>() == null && h.collider.GetComponentInParent<Animator>() == null && h.point.y > best) best = h.point.y;
            if (!float.IsNegativeInfinity(best)) p.y = best;
            return p;
        }

        private void Finish()
        {
            Rolling = false; if (director != null) director.Stop();
            if (jeep != null)
            {
                var rbj = jeep.GetComponent<Rigidbody>();
                if (skipped || OpeningCinematic.SkipRequested || Vector3.Distance(jeep.transform.position, jeepStop) > 6f)
                { jeep.transform.position = JeepPath(1f); jeep.transform.rotation = Quaternion.LookRotation(Vector3.back); }
                // Skipped before the hero got out: take him out of the seat first. Otherwise his collision comes back on inside the jeep and throws it over.
                if (player != null && player.GetComponent<LegacyOfTemples.Gameplay.Vehicles.SeatedPose>() != null) jeep.SeatDriver(player.transform, false, jeep.DriverExitPoint);
                jeep.EndScripted(true); if (rbj != null) { rbj.linearVelocity = Vector3.zero; rbj.angularVelocity = Vector3.zero; }
                jeep.Car.autopilot = false; jeep.Car.autoBrake = false; jeep.Audio.engineLevel = 0.6f; jeep.Lights.SetHeadLights(false);
            }
            if (robber1 != null) Destroy(robber1.gameObject); if (robber2 != null) Destroy(robber2.gameObject);
            if (pujari != null) { pujari.position = pujariHome; pujari.rotation = pujariRot; Show(pujari, true); }
            if (meera != null) { Show(meera, true); var an = meera.GetComponentInChildren<Animator>(); if (an != null) an.SetBool("IsTalking", false); if (!skipped && !OpeningCinematic.SkipRequested && VillageLife.Instance != null) VillageLife.Instance.StartCoroutine(MeeraHome()); else { meera.position = meeraHome; meera.rotation = meeraRot; } }
            if (player != null)
            {
                if (skipped || OpeningCinematic.SkipRequested || t < 55f)
                {
                    Vector3 stand = VillageLife.Instance != null ? VillageLife.Instance.TeaStallStand + new Vector3(1.4f, 0f, -0.6f) : jeepStop + new Vector3(-2f, 0f, 0f);
                    player.Teleport(stand, Quaternion.LookRotation(Vector3.back));
                }
                else player.transform.rotation = Quaternion.LookRotation(Vector3.back);   // facing up the road, towards the temple
                Show(player.transform, true);
                var drv = player.GetComponent<NpcAnimDriver>(); if (drv != null) Destroy(drv);
                player.enabled = true;
            }
            foreach (var s in shots) if (s.cam != null) Destroy(s.cam.gameObject);
            if (brain != null) Destroy(brain);
            if (orbit != null) { orbit.enabled = true; orbit.SetTarget(player != null ? player.transform : null); }
            if (music != null) music.Stop();
            DawnClock.Hold = false; InputGate.CinematicActive = false; Playing = false; Finished = true; LookTuner.Dawn = 0.8f;   // play goes on in the same morning light
            StoryDialogueSystem.Instance?.RegisterMinigame("reach_temple");
            Debug.Log($"[Intro] reveal check: videoPart={VideoPart}, site={(TempleSite.Instance != null)}, skipped={skipped}, skipReq={OpeningCinematic.SkipRequested}");
            if (!VideoPart && TempleSite.Instance != null && !skipped && !OpeningCinematic.SkipRequested)
                TempleSite.Instance.StartCoroutine(TempleSite.Instance.Reveal(player, () => ObjectiveBanner.Show("OBJECTIVE", "REACH JAGANNATH TEMPLE", 4.5f)));
            else { if (TempleSite.Instance != null) TempleSite.Instance.SetHidden(false); ObjectiveBanner.Show("OBJECTIVE", "REACH JAGANNATH TEMPLE", 4.5f); }
            Debug.Log($"[Intro] finished at {t:0.0}s ({(skipped || OpeningCinematic.SkipRequested ? "skipped" : "played through")}), last shot {LastShot}");
        }

        private IEnumerator MeeraHome()
        {
            yield return new WaitForSeconds(2.5f);
            if (meera == null) yield break;
            var drv = meera.GetComponent<NpcAnimDriver>() ?? meera.gameObject.AddComponent<NpcAnimDriver>();
            meera.position = meeraHome; meera.rotation = meeraRot;   // she goes back to the courtyard before the player gets there
        }
    }

    /// <summary>Short centre-top banner: OBJECTIVE / NEW LOCATION DISCOVERED.</summary>
    public sealed class ObjectiveBanner : MonoBehaviour
    {
        private static ObjectiveBanner inst; private string head, body; private float until;
        public static string Last { get; private set; } = "";
        public static void Show(string heading, string text, float seconds)
        {
            if (inst == null) { inst = new GameObject("ObjectiveBanner").AddComponent<ObjectiveBanner>(); DontDestroyOnLoad(inst.gameObject); }
            inst.head = heading; inst.body = text; inst.until = Time.unscaledTime + seconds; Last = heading + ": " + text;
            AudioManager.Instance?.PlaySFX("sfx_objective", 0.6f);
        }
        private void Update()
        {
            if (Time.unscaledTime > until) return;
            ScreenText.Panel("objective_banner", null, string.IsNullOrEmpty(body) ? $"<color=#FFC857><b>{head}</b></color>" : $"<size=80%><color=#5CE1E6>{head}</color></size>\n<color=#FFC857><b>{body}</b></color>", PanelAnchor.TopCenter, 460f, null, false, 70);
        }
    }
}
