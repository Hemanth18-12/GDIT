using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Places the user's temple (optimised game copy of temple.fbx) north of the village, at the end of the main road,
    /// with ground under it, a red flag waving on the tower, flickering oil lamps at its gate and birds wheeling round
    /// the tower. Also completes "Reach Jagannath Temple" at the courtyard gateway and shows the discovery moment.
    /// </summary>
    public sealed class TempleSite : MonoBehaviour
    {
        public static TempleSite Instance { get; private set; }
        public static Bounds Footprint { get; private set; }
        public static int Triangles { get; private set; }
        public Transform Temple { get; private set; }
        public Vector3 TowerTop { get; private set; }
        private bool built, discovered; private float wait;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded += (s, m) => { if (s.name == Constants.Courtyard) new GameObject("TempleSite").AddComponent<TempleSite>(); };
            if (SceneManager.GetActiveScene().name == Constants.Courtyard) new GameObject("TempleSite").AddComponent<TempleSite>();
        }
        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (!built)
            {
                wait += Time.unscaledDeltaTime;
                if (GameObject.Find("Village") == null) { if (wait > 8f) built = true; return; }
                built = true; Build();
            }
            CheckArrival();
        }

        private void Build()
        {
            float top = Village.Top;
            Vector3 templePos = new Vector3(Village.Centre.x, top, Village.ZMin - 48f);
            var res = KalingaTempleBuilder.Build(transform, "Temple_Site", templePos, 1.0f);
            Temple = res.root;
            Bounds b = res.footprint;
            Footprint = b;
            Triangles = 0;
            foreach (var filter in Temple.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh != null)
                    for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                        Triangles += (int)filter.sharedMesh.GetIndexCount(sub) / 3;

            // Seal the old black-shaded Sanctum portal box inside Courtyard.unity so players cannot enter darkness or get trapped.
            SealCourtyardBlackGate(top);

            var ground = Village.Box(Temple, "TempleGround", Vector3.zero, new Vector3(b.size.x + 60f, 0.2f, b.size.z + 60f), "red_sandstone_pavement", 3.2f);
            ground.transform.position = new Vector3(b.center.x, top - 0.12f, b.center.z);
            Village.Box(Temple, "TempleRoad", Vector3.zero, new Vector3(8f, 0.2f, 40f), "stony_dirt_path", 4f).transform.position = new Vector3(Village.Centre.x, top - 0.08f, b.max.z + 18f);
            var north = GameObject.Find("VillageBoundary_North"); if (north != null) north.SetActive(false);   // the road now continues to the temple
            foreach (var (pos, size) in new[] { (new Vector3(b.center.x, top + 3f, b.min.z - 28f), new Vector3(b.size.x + 60f, 8f, 1f)), (new Vector3(b.min.x - 28f, top + 3f, b.center.z), new Vector3(1f, 8f, b.size.z + 120f)), (new Vector3(b.max.x + 28f, top + 3f, b.center.z), new Vector3(1f, 8f, b.size.z + 120f)) })
            { var f = new GameObject("TempleBoundary"); f.transform.SetParent(Temple, false); f.transform.position = pos; f.AddComponent<BoxCollider>().size = size; }

            TowerTop = res.towerTop;
            Flag(Temple, TowerTop);
            Birds(Temple, TowerTop);
            for (int s = -1; s <= 1; s += 2)
            {   // oil lamps grounded at the approach
                Vector3 p = new Vector3(Village.Centre.x + s * 5f, top, b.max.z + 4f);
                ArtUpgrade.Place(Temple, "brass_diya_lantern", p, 2.2f, 0f);
                var l = new GameObject("TempleLamp").AddComponent<Light>(); l.transform.SetParent(Temple, false); l.transform.position = p + Vector3.up * 0.9f;
                l.type = LightType.Point; l.range = 7f; l.intensity = 2.4f; l.color = new Color(1f, 0.66f, 0.3f); l.shadows = LightShadows.None;
                l.gameObject.AddComponent<LightFlicker>().Configure(1.8f, 2.8f, 10f);
            }
            Debug.Log($"[TempleSite] grounded Kalinga temple built: {Triangles} triangles, footprint {b.size.x:0} x {b.size.z:0} m, tower top {TowerTop}");
        }

        private static void SealCourtyardBlackGate(float groundY)
        {
            // Close the dark Sanctum doorway behind Pujari & Meera so players stay cleanly inside the Courtyard.
            foreach (var st in FindObjectsByType<SceneTransitionTrigger>(FindObjectsInactive.Include))
            {
                if (st.gameObject.name.Contains("Portal_To_Sanctum") || st.Id == "portal_to_" + Constants.Sanctum)
                {
                    Vector3 p = st.transform.position;
                    var mr = st.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false;
                    var col = st.GetComponent<Collider>(); if (col != null) col.isTrigger = false;
                    st.enabled = false;
                    // Keep the frame under an unscaled root: parenting it to the door cube
                    // multiplied its dimensions into an oversized solid slab.
                    var doorway = new GameObject("Courtyard_Sanctum_ClosedDoors").transform;
                    doorway.position = new Vector3(p.x, groundY, p.z);
                    Village.Box(doorway, "Closed wooden doors", new Vector3(0f, 1.6f, 0f), new Vector3(3.8f, 3.2f, 0.8f), "medieval_wood", 1.5f, true);
                    for (int side = -1; side <= 1; side += 2)
                        Village.Box(doorway, "Stone door jamb", new Vector3(side * 2.06f, 1.7f, 0f), new Vector3(0.32f, 3.4f, 1.05f), "sandstone_blocks_08", 2f, true);
                    Village.Box(doorway, "Stone door lintel", new Vector3(0f, 3.5f, 0f), new Vector3(4.45f, 0.35f, 1.05f), "sandstone_blocks_08", 2f, true);
                }
            }
            // Also neutralize any pure black "mat_SanctumDarkness" box inside Courtyard
            foreach (var r in FindObjectsByType<MeshRenderer>())
            {
                if (r.sharedMaterial != null && r.sharedMaterial.name.Contains("SanctumDarkness"))
                {
                    r.sharedMaterial = ArtUpgrade.Mat("red_sandstone_wall") ?? r.sharedMaterial;
                    var c = r.GetComponent<Collider>(); if (c != null) c.isTrigger = false;
                }
            }
        }

        private static Bounds Measure(GameObject g)
        {
            var rs = g.GetComponentsInChildren<Renderer>(); var b = rs.Length > 0 ? rs[0].bounds : new Bounds(g.transform.position, Vector3.one);
            foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        private static Vector3 HighestPoint(GameObject g)
        {
            Vector3 best = g.transform.position; float by = float.MinValue;
            foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
            {
                var r = mf.GetComponent<Renderer>(); if (r == null || r.bounds.max.y < by || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
                var v = mf.sharedMesh.vertices; var m = mf.transform.localToWorldMatrix;
                for (int i = 0; i < v.Length; i++) { var w = m.MultiplyPoint3x4(v[i]); if (w.y > by) { by = w.y; best = w; } }
            }
            if (by == float.MinValue) { var b = Measure(g); best = new Vector3(b.center.x, b.max.y, b.center.z); }
            return best;
        }

        private static void Flag(Transform root, Vector3 at)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(pole.GetComponent<Collider>()); pole.name = "FlagPole"; pole.transform.SetParent(root, false);
            pole.transform.position = at + Vector3.up * 2.5f; pole.transform.localScale = new Vector3(0.12f, 2.5f, 0.12f); pole.GetComponent<Renderer>().sharedMaterial = ArtUpgrade.Mat("medieval_wood");
            var f = new GameObject("TempleFlag"); f.transform.SetParent(root, false); f.transform.position = at + Vector3.up * 4.4f;
            f.AddComponent<WavingFlag>();
        }

        private static void Birds(Transform root, Vector3 around)
        {
            var g = new GameObject("TempleBirds"); g.transform.SetParent(root, false); g.transform.position = around + Vector3.down * 8f;
            g.AddComponent<CirclingBirds>();
        }

        // ------------------------------------------------------------------ reaching the temple
        // ------------------------------------------------------------------ hidden on the approach, revealed after the directions
        private readonly List<Renderer> hiddenParts = new List<Renderer>();
        /// <summary>Hides the tall temple buildings (not the ground or road) while the jeep arrives, so the first sight of it is the reveal.</summary>
        public void SetHidden(bool hide)
        {
            if (Temple == null) return;
            Debug.Log($"[TempleSite] SetHidden({hide}) temple={(Temple != null)} parts={hiddenParts.Count}");
            if (hide) { hiddenParts.Clear(); foreach (var r in Temple.GetComponentsInChildren<Renderer>()) if (r.enabled && r.bounds.max.y > Village.Top + 4f) { r.enabled = false; hiddenParts.Add(r); } }
            else { foreach (var r in hiddenParts) if (r != null) r.enabled = true; hiddenParts.Clear(); }
        }
        /// <summary>The reveal: a short fade, the camera rises over the village rooftops to the temple tower, bell, banner. Then play.</summary>
        public IEnumerator Reveal(PlayerController pc, System.Action done)
        {
            var cam = Camera.main; var orbit = FindAnyObjectByType<OrbitCamera>();
            if (pc != null) pc.enabled = false; if (orbit != null) orbit.enabled = false;
            InputGate.CinematicActive = true;   // HUD hints stay away during the reveal
            SetHidden(false);
            if (cam != null)
            {
                Vector3 look = TowerTop != Vector3.zero ? TowerTop + Vector3.down * 12f : Village.Centre + Vector3.up * 15f;
                Vector3 start = (pc != null ? pc.transform.position : Village.Centre) + Vector3.up * 1.7f + Vector3.forward * 3.5f;
                Vector3 end = start + Vector3.up * 9f + Vector3.forward * 4f;
                Quaternion r0 = Quaternion.LookRotation(Vector3.back + Vector3.down * 0.15f);
                for (float t = 0f; t < 4.2f; t += Time.deltaTime)
                {
                    float k = Mathf.SmoothStep(0f, 1f, t / 4.2f);
                    cam.transform.position = Vector3.Lerp(start, end, k);
                    cam.transform.rotation = Quaternion.Slerp(r0, Quaternion.LookRotation(look - cam.transform.position), Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 2.4f)));
                    if (t > 1.4f && t - Time.deltaTime <= 1.4f) { ObjectiveBanner.Show("NEW LOCATION DISCOVERED", "JAGANNATH TEMPLE", 3.5f); AudioManager.Instance?.PlaySFX("sfx_bell_temple", 0.6f); }
                    yield return null;
                }
                yield return new WaitForSeconds(0.8f);
            }
            InputGate.CinematicActive = false;
            if (orbit != null) orbit.enabled = true; if (pc != null) pc.enabled = true;
            done?.Invoke();
        }

        private void CheckArrival()
        {
            var story = StoryDialogueSystem.Instance; if (story == null || story.Current == null || discovered) return;
            if (story.Current.id != "obj_c0_reach") return;
            story.RegisterMinigame("reach_temple");   // registered first, so the story's skip prompt never shows over the intro
            if (IntroCinematic.Playing) return;
            var pc = FindAnyObjectByType<PlayerController>(); var jeep = Vehicles.Jeep.Instance;
            Vector3 p = jeep != null && jeep.Occupied ? jeep.transform.position : pc != null ? pc.transform.position : Vector3.zero;
            Vector3 gate = Village.GatePos;
            bool there = gate != Vector3.zero && (Vector3.Distance(Flat(p), Flat(gate)) < 7f || (Mathf.Abs(p.x - gate.x) < 28f && p.z < gate.z - 1f && p.z > Village.ZMin));
            if (!there) return;
            discovered = true; StartCoroutine(Discover(pc, jeep));
        }
        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        private IEnumerator Discover(PlayerController pc, Vehicles.Jeep jeep)
        {
            if (jeep != null && jeep.Occupied) yield return jeep.GetOut();
            ObjectiveBanner.Show("YOU HAVE ARRIVED", "JAGANNATH TEMPLE", 4f);
            AudioManager.Instance?.PlaySFX("sfx_bell_temple", 0.55f);
            // a short camera moment: up over the courtyard to the temple tower, then straight back to play
            var cam = Camera.main; var orbit = FindAnyObjectByType<OrbitCamera>();
            if (cam != null && pc != null)
            {
                pc.enabled = false; if (orbit != null) orbit.enabled = false;
                Vector3 from = cam.transform.position; Quaternion fromR = cam.transform.rotation;
                Vector3 eye = Village.GatePos + new Vector3(3f, 6f, 6f); Vector3 look = TowerTop != Vector3.zero ? TowerTop : Village.Centre + Vector3.up * 15f;
                for (float t = 0f; t < 3.2f; t += Time.deltaTime)
                {
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 1.2f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((3.2f - t) / 0.9f));
                    cam.transform.position = Vector3.Lerp(from, eye + Vector3.up * t * 0.5f, k);
                    cam.transform.rotation = Quaternion.Slerp(fromR, Quaternion.LookRotation(look - cam.transform.position), k);
                    yield return null;
                }
                if (orbit != null) orbit.enabled = true; pc.enabled = true;
            }
            StoryDialogueSystem.Instance?.CompleteMinigame("reach_temple");
        }
    }

    /// <summary>A red temple flag: a cloth strip whose vertices ripple in the wind (vertex motion, not a character animation).</summary>
    public sealed class WavingFlag : MonoBehaviour
    {
        private Mesh mesh; private Vector3[] rest, cur; private const int W = 14, H = 5;
        private void Start()
        {
            mesh = new Mesh { name = "Flag" }; rest = new Vector3[(W + 1) * (H + 1)]; var uv = new Vector2[rest.Length]; var tri = new int[W * H * 6];
            for (int y = 0; y <= H; y++) for (int x = 0; x <= W; x++) { int i = y * (W + 1) + x; float u = x / (float)W, v = y / (float)H; rest[i] = new Vector3(u * 4.2f, -v * 1.6f * (1f - 0.55f * u), 0f); uv[i] = new Vector2(u, v); }
            int k = 0; for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { int i = y * (W + 1) + x; tri[k++] = i; tri[k++] = i + W + 1; tri[k++] = i + 1; tri[k++] = i + 1; tri[k++] = i + W + 1; tri[k++] = i + W + 2; }
            cur = (Vector3[])rest.Clone(); mesh.vertices = cur; mesh.uv = uv; mesh.triangles = tri; mesh.RecalculateNormals();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = gameObject.AddComponent<MeshRenderer>(); var m = Fx.Lit(new Color(0.75f, 0.06f, 0.03f), 0.15f); m.SetFloat("_Cull", 0f); r.sharedMaterial = m;
        }
        private void Update()
        {
            float t = Time.time;
            for (int i = 0; i < rest.Length; i++) { var p = rest[i]; float u = p.x / 4.2f; cur[i] = p + new Vector3(0f, Mathf.Sin(t * 2.1f + p.x * 1.4f) * 0.08f * u, Mathf.Sin(t * 3.4f + p.x * 1.9f + p.y) * 0.35f * u); }
            mesh.vertices = cur; mesh.RecalculateNormals();
        }
    }

    /// <summary>A few dark birds wheeling around the tower.</summary>
    public sealed class CirclingBirds : MonoBehaviour
    {
        private Transform[] birds; private float[] r, h, s, o;
        private void Start()
        {
            int n = 9; birds = new Transform[n]; r = new float[n]; h = new float[n]; s = new float[n]; o = new float[n];
            var mat = Fx.Lit(new Color(0.05f, 0.05f, 0.06f), 0.1f);
            for (int i = 0; i < n; i++)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(b.GetComponent<Collider>()); b.name = "Bird"; b.transform.SetParent(transform, false);
                b.transform.localScale = new Vector3(0.9f, 0.25f, 1f); b.GetComponent<Renderer>().sharedMaterial = mat; birds[i] = b.transform;
                r[i] = Random.Range(10f, 24f); h[i] = Random.Range(-4f, 8f); s[i] = Random.Range(0.18f, 0.32f) * (i % 3 == 0 ? -1f : 1f); o[i] = Random.Range(0f, 6.28f);
            }
        }
        private void Update()
        {
            var cam = Camera.main;
            for (int i = 0; i < birds.Length; i++)
            {
                float a = o[i] + Time.time * s[i];
                birds[i].localPosition = new Vector3(Mathf.Cos(a) * r[i], h[i] + Mathf.Sin(Time.time * 0.7f + i) * 1.5f, Mathf.Sin(a) * r[i]);
                if (cam != null) birds[i].rotation = Quaternion.LookRotation(birds[i].position - cam.transform.position);
                birds[i].localScale = new Vector3(0.9f * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 9f + i))), 0.25f, 1f);   // wing beat
            }
        }
    }
}
