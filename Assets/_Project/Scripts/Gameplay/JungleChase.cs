using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.Gameplay.Vehicles;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// V3 zone 6 (Jungle Road and Ruins): north of the temple a jungle road runs to a ruined court above the river.
    /// Bhaskar runs for it; the player follows by jeep (or on foot). Catching up earns points but never fails the chase.
    /// In the court: a first wave of robbers, a second wave whose size depends on whether the inside man was named,
    /// then Bhaskar himself. He carries the fifth treasure, the Chariot Finial. Madhav, the key-keeper, gives himself up.
    /// </summary>
    public sealed class JungleChase : MonoBehaviour
    {
        public const string ZoneId = "jungle";
        public enum Phase { Waiting, Chase, Court, Wave1, Wave2, Boss, Done }
        public static JungleChase Instance { get; private set; }
        public bool Built { get; private set; }
        public Phase Now { get; private set; }
        public Transform Runner { get; private set; }
        public Transform Court { get; private set; }
        public Vector3 RoadStart { get; private set; }
        public TreasurePickup Finial { get; private set; }
        public float ClosestGap { get; private set; } = 999f;
        public bool Caught { get; private set; }
        public readonly List<Vector3> Road = new List<Vector3>();
        int leg; Transform madhav;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded += (s, m) => { if (s.name == Constants.Courtyard) Spawn(); }; if (SceneManager.GetActiveScene().name == Constants.Courtyard) Spawn(); }
        static void Spawn() { if (Instance == null) new GameObject("V3_Zone6_Jungle").AddComponent<JungleChase>(); }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            float wait = 0f; while ((TempleSite.Instance == null || TempleSite.Footprint.size.sqrMagnitude < 1f) && wait < 20f) { wait += Time.deltaTime; yield return null; }
            if (TempleSite.Footprint.size.sqrMagnitude < 1f) yield break;
            yield return null; Build();
        }

        public void Build()
        {
            var b = TempleSite.Footprint; float top = Village.Top;
            float x0 = b.center.x + 26f, zWall = b.min.z - 28f;
            // open a gap in the temple's north boundary (an invisible wall) for the road
            foreach (var t in FindObjectsByType<Transform>())
            {
                if (t.name != "TempleBoundary") continue; var bc = t.GetComponent<BoxCollider>(); if (bc == null || bc.size.x < 50f || Mathf.Abs(t.position.z - zWall) > 2f) continue;
                float half = bc.size.x * 0.5f, left = t.position.x - half, right = t.position.x + half, g0 = x0 - 7f, g1 = x0 + 7f;
                bc.size = new Vector3(g0 - left, bc.size.y, bc.size.z); bc.center = new Vector3((left + g0) * 0.5f - t.position.x, bc.center.y, bc.center.z);
                var r = new GameObject("TempleBoundary_NE"); r.transform.SetParent(t.parent, false); r.transform.position = new Vector3((g1 + right) * 0.5f, t.position.y, t.position.z); r.AddComponent<BoxCollider>().size = new Vector3(right - g1, bc.size.y, bc.size.z);
            }
            // the road: 230 m with two bends, ground and walls of jungle on both sides
            var root = transform; Road.Clear();
            for (int i = 0; i <= 23; i++) { float z = zWall + 6f - i * 10f; Road.Add(new Vector3(x0 + Mathf.Sin(i * 0.33f) * 14f, top, z)); }
            RoadStart = Road[1];
            var ground = Village.Box(root, "Jungle_Ground", Vector3.zero, new Vector3(140f, 0.2f, 280f), "brown_mud_leaves_01", 5f); ground.transform.position = new Vector3(x0, top - 0.14f, zWall - 125f);
            for (int i = 0; i < Road.Count - 1; i++)
            {
                Vector3 a = Road[i], c = Road[i + 1], mid = (a + c) * 0.5f; var seg = Village.Box(root, "JungleRoad", Vector3.zero, new Vector3(9f, 0.2f, Vector3.Distance(a, c) + 1.5f), "stony_dirt_path", 4f, false);   // visual only: overlapping segment edges would catch the wheels
                seg.transform.position = mid + Vector3.down * 0.125f; seg.transform.rotation = Quaternion.LookRotation(c - a);
            }
            var rng = new System.Random(606); float R(float lo, float hi) => lo + (float)rng.NextDouble() * (hi - lo);
            for (int i = 1; i < Road.Count - 4; i++)
                for (int k = 0; k < 3; k++) foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 p = Road[i] + new Vector3(side * R(10f, 26f), 0f, R(-5f, 5f));
                    ArtUpgrade.Tree(root, p, R(9f, 15f), rng.Next());
                    if (k == 0 && i % 2 == 0) ArtUpgrade.Place(root, rng.NextDouble() > 0.5 ? "fern_02" : "rock_moss_set_01", p + new Vector3(-side * 3f, 0f, 1f), R(1.2f, 2.2f), R(0f, 360f));
                }
            // two fallen logs that slow the jeep (driven around or bumped over)
            foreach (int i in new[] { 7, 14 }) { var log = ZoneKit.Round(root, "FallenLog", Road[i] + new Vector3(i == 7 ? -2.5f : 2.5f, 0.35f, 0f), new Vector3(0.7f, 2.4f, 0.7f), ZoneKit.Tinted("bark_brown_02", new Color(0.4f, 0.3f, 0.22f), 0.1f), true); log.transform.rotation = Quaternion.Euler(0f, 0f, 90f); }

            // the ruined court at the end of the road
            Vector3 end = Road[Road.Count - 1]; var court = new GameObject("RuinCourt").transform; court.SetParent(root, false); court.position = end + Vector3.back * 16f; court.rotation = Quaternion.identity; Court = court;
            const string stone = "mossy_sandstone";
            Village.Box(court, "Court_Floor", new Vector3(0f, 0.04f, 0f), new Vector3(30f, 0.08f, 30f), "red_sandstone_pavement", 3f, false);
            Village.Box(court, "Court_Back", new Vector3(0f, 2f, -15f), new Vector3(30f, 4f, 1.2f), stone, 2.4f);
            Village.Box(court, "Court_L", new Vector3(-15f, 2f, 0f), new Vector3(1.2f, 4f, 30f), stone, 2.4f);
            Village.Box(court, "Court_R", new Vector3(15f, 2f, 0f), new Vector3(1.2f, 4f, 30f), stone, 2.4f);
            Village.Box(court, "Court_FrontL", new Vector3(-9.5f, 2f, 15f), new Vector3(11f, 4f, 1.2f), stone, 2.4f);
            Village.Box(court, "Court_FrontR", new Vector3(9.5f, 2f, 15f), new Vector3(11f, 4f, 1.2f), stone, 2.4f);
            Village.Box(court, "Arch_Top", new Vector3(0f, 5.2f, 15f), new Vector3(8f, 1.2f, 1.6f), stone, 2f);
            Village.Box(court, "Arch_L", new Vector3(-3.6f, 2.6f, 15f), new Vector3(0.9f, 5.2f, 1.6f), stone, 2f); Village.Box(court, "Arch_R", new Vector3(3.6f, 2.6f, 15f), new Vector3(0.9f, 5.2f, 1.6f), stone, 2f);
            for (int i = 0; i < 6; i++) { float a = i * 60f * Mathf.Deg2Rad; var col = Village.Box(court, "BrokenPillar", new Vector3(Mathf.Cos(a) * 9f, 1f + (i % 3) * 0.6f, Mathf.Sin(a) * 9f), new Vector3(0.9f, 2f + (i % 3) * 1.2f, 0.9f), stone, 1.4f); }
            Village.Box(court, "Ghat_Steps", new Vector3(0f, -0.6f, -19f), new Vector3(20f, 1.2f, 6f), "red_sandstone_pavement", 2f);
            var river = ZoneKit.Part(court, "River", new Vector3(0f, -1.1f, -40f), new Vector3(160f, 0.05f, 36f), ZoneKit.Glass(new Color(0.12f, 0.2f, 0.2f, 0.8f), 0.95f), false);
            foreach (var p in new[] { new Vector3(-4.5f, 0f, 16.5f), new Vector3(4.5f, 0f, 16.5f), new Vector3(-13f, 0f, -13f), new Vector3(13f, 0f, -13f), new Vector3(-13f, 0f, 8f), new Vector3(13f, 0f, 8f) })
            {
                ZoneKit.Round(court, "TorchPole", p + Vector3.up * 1.1f, new Vector3(0.12f, 1.1f, 0.12f), ZoneKit.Tinted("medieval_wood", Color.white, 0.2f), false);
                var lg = new GameObject("Torch"); lg.transform.SetParent(court, false); lg.transform.localPosition = p + Vector3.up * 2.4f;
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.22f); l.range = 12f; l.intensity = 9f; l.shadows = LightShadows.None; lg.AddComponent<LightFlicker>().Configure(7f, 11f, 12f);
            }
            // art pass: coping on the court walls, carved (broken) pillars, a stone arch at the entrance, lamp stands at the ghat
            ZoneDress.Trim(court, "Court_");
            foreach (var t in court.GetComponentsInChildren<Transform>()) if (t.name == "BrokenPillar") ZoneDress.PillarOver(t);
            foreach (var t in new[] { "Arch_Top", "Arch_L", "Arch_R" }) { var a = court.Find(t); if (a != null) a.GetComponent<MeshRenderer>().enabled = false; }
            ZoneDress.Prop(court, "SM_V3_Arch", new Vector3(0f, 0f, 15f), 0f, new Vector3(1.45f, 0.95f, 1.6f), null);
            ZoneDress.Lamp(court, new Vector3(-6f, 0.08f, -15.9f), 9f, 4f); ZoneDress.Lamp(court, new Vector3(6f, 0.08f, -15.9f), 9f, 4f);
            // the finial, in Bhaskar's satchel until he is beaten
            var fin = ArtUpgrade.Place(court, "brass_vase_03", court.TransformPoint(new Vector3(0f, 0f, -6f)), 1.3f, 0f);
            if (fin == null) fin = ZoneKit.Round(court, "Finial", new Vector3(0f, 0.3f, -6f), new Vector3(0.3f, 0.3f, 0.3f), ZoneKit.Tinted("clay_plaster", new Color(0.85f, 0.65f, 0.25f), 0.9f));
            fin.name = "ChariotFinial"; foreach (var cd in fin.GetComponentsInChildren<Collider>()) Destroy(cd); foreach (var r in fin.GetComponentsInChildren<Renderer>()) r.enabled = false; ZoneKit.Reach(fin, 2.2f);
            Finial = fin.AddComponent<TreasurePickup>(); Finial.treasureName = "the Chariot Finial"; Finial.zoneId = ZoneId; Finial.enabled = false;

            // Bhaskar, waiting at the start of the road
            var pf = Resources.Load<GameObject>("NPC/Robber1_Cine");
            if (pf != null)
            {
                var g = Instantiate(pf, Road[3], Quaternion.LookRotation(Road[4] - Road[3])); g.name = "Bhaskar_Runner";
                foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false; foreach (var cd in g.GetComponentsInChildren<Collider>()) Destroy(cd);
                var mpb = new MaterialPropertyBlock(); foreach (var r in g.GetComponentsInChildren<Renderer>()) { r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", new Color(0.85f, 0.45f, 0.4f)); r.SetPropertyBlock(mpb); }
                g.transform.localScale *= 1.12f; g.AddComponent<NpcAnimDriver>(); var fs = g.AddComponent<NpcFootsteps>(); fs.volume = 0.7f; fs.stride = 1.4f; Runner = g.transform; leg = 4;
            }
            var lookGo = new GameObject("ZoneLook_Jungle"); lookGo.transform.SetParent(root, false); lookGo.transform.position = new Vector3(x0, top + 10f, zWall - 125f);
            var look = lookGo.AddComponent<ZoneLook>(); look.zone = ZoneLook.Zone.Jungle; look.size = new Vector3(140f, 40f, 260f); look.blendDistance = 14f; look.Build();
            ZoneAtmosphere.Register(look, Quaternion.Euler(9f, 150f, 0f));
            Built = true;
            if (Brawl.WonBefore(ZoneId + "_boss")) { Now = Phase.Done; if (Runner != null) Runner.gameObject.SetActive(false); Finial.enabled = true; foreach (var r in Finial.GetComponentsInChildren<Renderer>()) r.enabled = true; }
            Debug.Log($"[Jungle] zone 6 built: road {Road.Count} points from {RoadStart:0.0}, court at {court.position:0.0}");
        }

        bool StoryWantsMe() { var st = StoryDialogueSystem.Instance; return st != null && st.Current != null && st.Current.id == "obj_v3_jungle"; }
        Transform Pursuer() { var j = Jeep.Instance; if (j != null && j.Occupied) return j.transform; var pc = ZoneKit.Player; return pc != null ? pc.transform : null; }

        void Update()
        {
            if (!Built || Now == Phase.Done) return;
            var who = Pursuer(); if (who == null) return;
            switch (Now)
            {
                case Phase.Waiting:
                    if (!StoryWantsMe()) return;
                    var jeep = Jeep.Instance;   // the jeep is waiting at the road start (Meera brought it round)
                    if (jeep != null && !jeep.Occupied && Vector3.Distance(jeep.transform.position, RoadStart) > 20f)
                    { var rb = jeep.GetComponent<Rigidbody>(); jeep.transform.SetPositionAndRotation(RoadStart + Vector3.up * 0.05f, Quaternion.LookRotation(Road[2] - Road[1])); /* in the middle of the road, clear of the trees */ if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; } Physics.SyncTransforms(); }
                    if (Vector3.Distance(who.position, RoadStart) < 22f)
                    { Now = Phase.Chase; StoryDialogueSystem.Instance?.Say("v3:jungle_run"); Checkpoints.Save(ZoneId, "road", ZoneKit.Player.transform.position, 180f, DawnClock.Instance != null ? DawnClock.Instance.Minutes * 60f : 0f); Debug.Log("[Jungle] Bhaskar runs"); }
                    break;
                case Phase.Chase:
                    {
                        ScoreSystem.AddTime(ZoneId, Time.deltaTime);
                        float gap = Vector3.Distance(who.position, Runner.position); ClosestGap = Mathf.Min(ClosestGap, gap);
                        if (!Caught && gap < 6f) { Caught = true; ScoreSystem.Combo(ZoneId, 10); ObjectiveBanner.Show("CAUGHT UP", "He makes for the ruins.", 2f); }
                        float speed = gap > 55f ? 3.2f : gap < 10f ? 8f : 6.3f;   // he slows if you fall far behind: losing him is never a dead end
                        Vector3 d = Road[leg] - Runner.position; d.y = 0f;
                        if (d.magnitude < 0.4f) { leg++; if (leg >= Road.Count) { leg = Road.Count - 1; Now = Phase.Court; } }
                        else { Runner.position += d.normalized * Mathf.Min(speed * Time.deltaTime, d.magnitude); Runner.rotation = Quaternion.Slerp(Runner.rotation, Quaternion.LookRotation(d), Time.deltaTime * 6f); }
                        break;
                    }
                case Phase.Court:
                    {
                        var pc = ZoneKit.Player; if (pc == null || Vector3.Distance(pc.transform.position, Court.position) > 16f) return;
                        if (Jeep.Instance != null && Jeep.Instance.Occupied) return;   // the fight starts on foot
                        Runner.gameObject.SetActive(false); Now = Phase.Wave1; StoryDialogueSystem.Instance?.Say("v3:jungle_court");
                        Brawl.Begin(ZoneId, Court.position, 11f, new[] { Brawl.Robber("a brawler", 1, BrawlType.Brawler, 1f, new Color(0.55f, 0.5f, 0.45f)), Brawl.Robber("a knife man", 2, BrawlType.Knife, 0.95f, new Color(0.5f, 0.55f, 0.7f)), Brawl.Robber("a brawler", 1, BrawlType.Brawler, 1.05f, new Color(0.6f, 0.5f, 0.4f)) }, _ => StartCoroutine(Wave2()), ZoneId + "_wave1");
                        break;
                    }
            }
        }

        IEnumerator Wave2()
        {
            yield return new WaitForSeconds(2.5f);
            Now = Phase.Wave2; bool named = EvidenceBoard.NamedCorrectly;
            Madhav(named);
            var specs = new List<Brawl.Spec> { Brawl.Robber("a heavy", 1, BrawlType.Heavy, 1.15f, new Color(0.45f, 0.4f, 0.38f)) };
            if (!named) { specs.Add(Brawl.Robber("a knife man", 2, BrawlType.Knife, 0.95f, new Color(0.5f, 0.55f, 0.7f))); specs.Add(Brawl.Robber("a brawler", 1, BrawlType.Brawler, 1f, new Color(0.55f, 0.5f, 0.45f))); }
            Debug.Log($"[Jungle] wave 2: inside man named correctly={named}, {specs.Count} robber(s)");
            Brawl.Begin(ZoneId, Court.position, 11f, specs, _ => StartCoroutine(Boss()), ZoneId + "_wave2");
        }

        IEnumerator Boss()
        {
            yield return new WaitForSeconds(2.5f);
            Now = Phase.Boss; StoryDialogueSystem.Instance?.Say("v3:jungle_boss");
            var boss = Brawl.Robber("Bhaskar", 1, BrawlType.Boss, 1.15f, new Color(0.85f, 0.45f, 0.4f)); boss.threePhases = true;
            var f = Brawl.Begin(ZoneId, Court.position, 10f, new[] { boss }, _ => Done(), ZoneId + "_boss");
            if (f != null) f.OnBossPhase = (e, n) => { if (n == 3) { StoryDialogueSystem.Instance?.Say("v3:jungle_phase3"); ObjectiveBanner.Show("HIS SWORD IS GONE", "He fights with his hands now.", 2.2f); } };
        }

        void Done()
        {
            Now = Phase.Done; Finial.enabled = true; foreach (var r in Finial.GetComponentsInChildren<Renderer>()) r.enabled = true;
            ScoreSystem.PuzzleSolved(ZoneId, 0, 0f);
            StartCoroutine(MadhavGivesUp());
        }

        /// <summary>Madhav, the key-keeper (the villager model; he does not fight). Named: he is waiting at the arch. Not named: he let the second wave in.</summary>
        void Madhav(bool named)
        {
            if (madhav != null) return;
            var pf = Resources.Load<GameObject>("NPC/Villager_Male_NPC"); if (pf == null) return;
            Vector3 at = Court.TransformPoint(new Vector3(named ? -2f : 9f, 0f, named ? 13.5f : -12f));
            var g = Instantiate(pf, at, Quaternion.LookRotation(Court.position - at)); g.name = "Madhav";
            foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false; foreach (var cd in g.GetComponentsInChildren<Collider>()) Destroy(cd);
            var mpb = new MaterialPropertyBlock(); foreach (var r in g.GetComponentsInChildren<Renderer>()) { r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", new Color(0.42f, 0.48f, 0.78f)); r.SetPropertyBlock(mpb); }   // same indigo kurta as his earlier appearances
            madhav = g.transform;
            StoryDialogueSystem.Instance?.Say(named ? "v3:madhav_named" : "v3:madhav_ambush");
        }
        IEnumerator MadhavGivesUp()
        {
            yield return new WaitForSeconds(3f);
            if (madhav == null) Madhav(EvidenceBoard.NamedCorrectly);
            StoryDialogueSystem.Instance?.Say("v3:madhav_caught");
        }
    }
}
