using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// The temple feels alive: a bronze bell on a stone frame at the approach that sways and rings when someone passes,
    /// and a row of brass lamp stands with flickering flames leading to the gate (the flag and birds already move).
    /// Also places one reflection probe per V3 place so metal, glass and wet stone reflect their own surroundings.
    /// </summary>
    public sealed class TempleLife : MonoBehaviour
    {
        public static TempleLife Instance { get; private set; }
        public Transform Bell { get; private set; }
        public int Rings { get; private set; }
        public int Probes { get; private set; }
        AudioSource ring; float swing, swingVel, cooldown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded += (s, m) => { if (s.name == Constants.Courtyard) Spawn(); }; if (SceneManager.GetActiveScene().name == Constants.Courtyard) Spawn(); }
        static void Spawn() { if (Instance == null) new GameObject("V3_TempleLife").AddComponent<TempleLife>(); }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            float wait = 0f; while ((TempleSite.Instance == null || TempleSite.Footprint.size.sqrMagnitude < 1f) && wait < 20f) { wait += Time.deltaTime; yield return null; }
            if (TempleSite.Footprint.size.sqrMagnitude < 1f) yield break;
            var b = TempleSite.Footprint; float top = Village.Top; var root = transform;
            // lamp stands along both sides of the temple road
            for (int i = 0; i < 4; i++) foreach (float s in new[] { -1f, 1f })
                ZoneDress.Lamp(root, new Vector3(Village.Centre.x + s * 5.5f, top, b.max.z + 9f + i * 7f), 6f, 2.6f);
            // the bell frame beside the road, near the gate
            var frame = new GameObject("BellFrame").transform; frame.SetParent(root, false); frame.position = new Vector3(Village.Centre.x - 9f, top, b.max.z + 12f);
            ZoneDress.Prop(frame, "SM_V3_Pillar", new Vector3(-1.1f, 0f, 0f), 0f, new Vector3(0.6f, 1.1f, 0.6f)); ZoneDress.Prop(frame, "SM_V3_Pillar", new Vector3(1.1f, 0f, 0f), 0f, new Vector3(0.6f, 1.1f, 0.6f));
            Village.Box(frame, "BellBeam", new Vector3(0f, 3.15f, 0f), new Vector3(2.8f, 0.22f, 0.3f), "medieval_wood", 1f, false);
            var pivot = new GameObject("BellPivot").transform; pivot.SetParent(frame, false); pivot.localPosition = new Vector3(0f, 3.0f, 0f);
            var bronze = ZoneKit.Tinted("clay_plaster", new Color(0.62f, 0.45f, 0.2f), 0.85f); bronze.SetFloat("_Metallic", 1f);
            ZoneKit.Round(pivot, "BellRope", new Vector3(0f, -0.15f, 0f), new Vector3(0.03f, 0.15f, 0.03f), ZoneKit.Tinted("thatch_roof_angled", new Color(0.6f, 0.5f, 0.35f), 0.1f));
            var cup = ZoneKit.Round(pivot, "BellCup", new Vector3(0f, -0.62f, 0f), new Vector3(0.55f, 0.3f, 0.55f), bronze);
            ZoneKit.Round(pivot, "BellLip", new Vector3(0f, -0.9f, 0f), new Vector3(0.68f, 0.04f, 0.68f), bronze);
            Bell = pivot;
            ring = frame.gameObject.AddComponent<AudioSource>(); ring.clip = Resources.Load<AudioClip>("Audio/sfx_bell_temple"); ring.spatialBlend = 1f; ring.minDistance = 4f; ring.maxDistance = 60f; ring.rolloffMode = AudioRolloffMode.Linear; ring.playOnAwake = false;
            AudioManager.Route(ring, "SFX");
            yield return new WaitForSeconds(3f);   // let the zones finish building
            PlaceProbes();
            Debug.Log($"[TempleLife] bell, 8 lamp stands, {Probes} reflection probes");
        }

        void PlaceProbes()
        {
            void Probe(Transform at, Vector3 local, Vector3 size, string name)
            {
                if (at == null) return;
                var g = new GameObject("ReflectionProbe_" + name); g.transform.SetParent(at, false); g.transform.localPosition = local;
                var p = g.AddComponent<ReflectionProbe>(); p.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime; p.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
                p.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces; p.size = size; p.resolution = 128; p.boxProjection = true; p.importance = 2; p.RenderProbe(); Probes++;
            }
            Probe(BreakInPuzzle.Instance != null ? BreakInPuzzle.Instance.transform : null, new Vector3(0f, 2f, 2.5f), new Vector3(14f, 6f, 12f), "storeroom");
            Probe(VaultPuzzle.Instance != null ? VaultPuzzle.Instance.transform : null, new Vector3(0f, 2f, 0f), new Vector3(25f, 4.4f, 10.6f), "vaults");
            Probe(StepwellPuzzle.Instance != null ? StepwellPuzzle.Instance.transform : null, new Vector3(0f, 4f, 0f), new Vector3(26f, 12f, 22f), "stepwell");
            Probe(SunWallPuzzle.Instance != null ? SunWallPuzzle.Instance.transform : null, new Vector3(0f, 4f, 2f), new Vector3(26f, 12f, 36f), "sunwall");
            Probe(JungleChase.Instance != null ? JungleChase.Instance.Court : null, new Vector3(0f, 3f, 0f), new Vector3(32f, 10f, 32f), "ruins");
            Probe(transform, new Vector3(Village.Centre.x, Village.Top + 4f, TempleSite.Footprint.max.z + 18f), new Vector3(40f, 14f, 40f), "approach");
        }

        void Update()
        {
            if (Bell == null) return;
            var pc = ZoneKit.Player; cooldown -= Time.deltaTime;
            if (pc != null && cooldown <= 0f && Vector3.Distance(pc.transform.position, Bell.position) < 4.5f)
            {   // someone passing sets it swinging and it rings once
                cooldown = 25f; swingVel += 55f; Rings++; if (ring != null && ring.clip != null) ring.Play();
            }
            swingVel += (-swing * 9f - swingVel * 0.9f) * Time.deltaTime; swing += swingVel * Time.deltaTime;
            Bell.localRotation = Quaternion.Euler(swing + Mathf.Sin(Time.time * 0.6f) * 1.5f, 0f, 0f);   // a slight sway in the breeze
        }
    }
}
