using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// V3 zone 1 (Village Bazaar): a courier with a ringing bag walks the village road. Follow him without a marker:
    /// he stops and looks back at set points; if he sees the player close, in the open and not crouching, he is "spotted".
    /// Three times and he runs. Fall too far behind and he waits, ringing. Either way he leads to a walled yard where he
    /// and a helper fight. The Dawn Bell and the courier's list (evidence for the inside man) are in his bag.
    /// </summary>
    public sealed class BazaarTail : MonoBehaviour
    {
        public const string ZoneId = "bazaar";
        public enum Phase { Waiting, Walking, LookingBack, Running, Arrived, Fight, Done }
        public static BazaarTail Instance { get; private set; }
        public bool Built { get; private set; }
        public Phase Now { get; private set; }
        public int Spotted { get; private set; }
        public Transform Courier { get; private set; }
        public Transform Yard { get; private set; }
        public TreasurePickup Bell { get; private set; }
        public float SightRange => GameOptions.SightRange + 2f;
        public readonly List<Vector3> Route = new List<Vector3>();
        readonly HashSet<int> stops = new HashSet<int>();
        int leg; float lookT, lostT, ringT; AudioSource ring; bool active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded += (s, m) => { if (s.name == Constants.Courtyard) Spawn(); }; if (SceneManager.GetActiveScene().name == Constants.Courtyard) Spawn(); }
        static void Spawn() { if (Instance == null) new GameObject("V3_Zone1_Bazaar").AddComponent<BazaarTail>(); }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            float wait = 0f; while (Village.ZMax == 0f && wait < 20f) { wait += Time.deltaTime; yield return null; }
            yield return null;
            Build();
        }

        static Vector3 Road(float x, float z) => new Vector3(Village.Centre.x + Village.RoadOffset(z) + x, Village.Top, z);

        public void Build()
        {
            float z0 = Village.ZMax;
            // the yard: first clear spot beside the far end of the bazaar road
            var cands = new List<(Vector3, Quaternion)>();
            foreach (float dz in new[] { 92f, 84f, 100f, 76f, 108f }) foreach (float side in new[] { 1f, -1f }) cands.Add((Road(side * 15f, z0 + dz), Quaternion.LookRotation(Vector3.right * -side)));
            if (!ZoneKit.ClearGround(cands, new Vector3(6.5f, 2f, 6.5f), out var yardAt, out var yardFace)) Debug.LogWarning("[Bazaar] no clear yard spot; using the last candidate");
            var y = new GameObject("DyersYard").transform; y.SetParent(transform, false); y.SetPositionAndRotation(yardAt, yardFace); Yard = y;
            const string wall = "yellow_plaster";
            Village.Box(y, "Yard_Back", new Vector3(0f, 1.6f, 6f), new Vector3(12f, 3.2f, 0.4f), wall, 2f);
            Village.Box(y, "Yard_L", new Vector3(-6f, 1.6f, 0.5f), new Vector3(0.4f, 3.2f, 11f), wall, 2f);
            Village.Box(y, "Yard_R", new Vector3(6f, 1.6f, 0.5f), new Vector3(0.4f, 3.2f, 11f), wall, 2f);
            Village.Box(y, "Yard_Floor", new Vector3(0f, 0.03f, 0.5f), new Vector3(12f, 0.06f, 11f), "stony_dirt_path", 2.5f, false);
            var cloth = new[] { new Color(0.85f, 0.55f, 0.1f), new Color(0.2f, 0.3f, 0.7f), new Color(0.7f, 0.12f, 0.15f) };
            for (int i = 0; i < 3; i++) ZoneKit.Part(y, "DyedCloth_" + i, new Vector3(-3f + i * 3f, 2.1f, 3.5f), new Vector3(2.2f, 1.6f, 0.03f), ZoneKit.Tinted("clay_plaster", cloth[i], 0.1f), false);
            for (int i = 0; i < 3; i++) ArtUpgrade.Place(y, i == 1 ? "Barrel_01" : "ceramic_pot", y.TransformPoint(new Vector3(-4.6f + i * 0.9f, 0f, 4.8f)), 1.2f, i * 40f);
            // art pass: coping on the yard walls, a lamp stand, and market stalls along the courier's road
            ZoneDress.Trim(y, "Yard_"); ZoneDress.Lamp(y, new Vector3(4.6f, 0.06f, 4.6f), 7f, 3f);
            var stalls = new[] { "WoodenTable_01", "brass_pot_01", "folding_wooden_stool", "Barrel_01", "brass_pot_02", "planter_box_01", "wooden_bowl_02", "antique_ceramic_vase_01" };
            for (int i = 0; i < 12; i++) ArtUpgrade.Place(transform, stalls[i % stalls.Length], Road(i % 2 == 0 ? 7.5f : -8.5f, z0 + 30f + i * 5.5f), 1.15f, i * 37f);

            // the route: from the tea stall along the road, three look-back stops, then into the yard
            Route.Clear(); stops.Clear();
            Route.Add(Road(-4.5f, z0 + 26f));
            foreach (float dz in new[] { 40f, 56f, 72f }) { Route.Add(Road(-4f, z0 + dz)); stops.Add(Route.Count - 1); }
            Vector3 front = yardAt + yardFace * new Vector3(0f, 0f, -9f); Route.Add(Road(-4f, front.z)); Route.Add(front); Route.Add(yardAt + yardFace * new Vector3(0f, 0f, 2.5f));

            // the courier: a robber, dressed for the bazaar (tinted), with a cloth bag that rings
            var pf = Resources.Load<GameObject>("NPC/Robber1_Cine");
            if (pf != null)
            {
                var g = Instantiate(pf, Route[0], Quaternion.LookRotation(Route[1] - Route[0])); g.name = "Courier";
                foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
                foreach (var cd in g.GetComponentsInChildren<Collider>()) Destroy(cd);
                var mpb = new MaterialPropertyBlock(); foreach (var r in g.GetComponentsInChildren<Renderer>()) { r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", new Color(0.72f, 0.6f, 0.45f)); r.SetPropertyBlock(mpb); }
                var bag = ZoneKit.Part(g.transform, "BellBag", new Vector3(0.28f, 0.95f, -0.12f), new Vector3(0.28f, 0.34f, 0.18f), ZoneKit.Tinted("clay_plaster", new Color(0.55f, 0.45f, 0.3f), 0.1f), false);
                ZoneKit.Part(bag.transform, "RedThread", new Vector3(0f, 0.45f, 0f), new Vector3(1.05f, 0.08f, 1.05f), ZoneKit.Tinted("clay_plaster", new Color(0.8f, 0.05f, 0.05f), 0.2f, new Color(0.4f, 0f, 0f)), false);
                g.AddComponent<NpcAnimDriver>();
                ring = g.AddComponent<AudioSource>(); ring.clip = BellClip(); ring.spatialBlend = 1f; ring.minDistance = 3f; ring.maxDistance = 45f; ring.rolloffMode = AudioRolloffMode.Linear; ring.playOnAwake = false;
                AudioManager.Route(ring, "SFX");
                Courier = g.transform;
            }
            // the treasure (in his bag) waits in the yard until he is beaten
            var bellGo = ArtUpgrade.Place(y, "brass_goblets", y.TransformPoint(new Vector3(1.2f, 0f, 3.6f)), 1.4f, 0f);
            if (bellGo == null) bellGo = ZoneKit.Round(y, "DawnBell", new Vector3(1.2f, 0.2f, 3.6f), new Vector3(0.25f, 0.2f, 0.25f), ZoneKit.Tinted("clay_plaster", new Color(0.8f, 0.6f, 0.25f), 0.8f));
            bellGo.name = "DawnBell"; foreach (var cd in bellGo.GetComponentsInChildren<Collider>()) Destroy(cd); ZoneKit.Reach(bellGo, 2.2f);
            foreach (var r in bellGo.GetComponentsInChildren<Renderer>()) r.enabled = false;
            Bell = bellGo.AddComponent<TreasurePickup>(); Bell.treasureName = "the Dawn Bell"; Bell.zoneId = ZoneId; Bell.enabled = false;

            var lookGo = new GameObject("ZoneLook_Bazaar"); lookGo.transform.SetParent(transform, false); lookGo.transform.position = Road(0f, z0 + 60f) + Vector3.up * 8f;
            var look = lookGo.AddComponent<ZoneLook>(); look.zone = ZoneLook.Zone.Bazaar; look.size = new Vector3(60f, 30f, 90f); look.blendDistance = 10f; look.Build();
            ZoneAtmosphere.Register(look, Quaternion.Euler(18f, -60f, 0f));
            Built = true;
            if (Brawl.WonBefore(ZoneId)) { Now = Phase.Done; if (Courier != null) Courier.gameObject.SetActive(false); }
            Debug.Log($"[Bazaar] zone 1 built: yard at {yardAt:0.0}, route {Route.Count} points");
        }

        /// <summary>A small brass bell, made in code (two struck partials with a fast decay).</summary>
        static AudioClip BellClip()
        {
            int sr = 44100, n = sr; var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)sr; d[i] = (Mathf.Sin(2f * Mathf.PI * 1480f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 2210f * t) * 0.3f + Mathf.Sin(2f * Mathf.PI * 3720f * t) * 0.1f) * Mathf.Exp(-t * 5.5f) * 0.5f; }
            var c = AudioClip.Create("courier_bell", n, 1, sr, false); c.SetData(d, 0); return c;
        }

        bool StoryWantsMe()
        {
            var st = StoryDialogueSystem.Instance;
            return st != null && st.Current != null && st.Current.id == "obj_v3_bazaar";
        }

        /// <summary>Can the courier see the player right now?</summary>
        public bool Sees(PlayerController pc)
        {
            if (Courier == null || pc == null) return false;
            Vector3 eye = Courier.position + Vector3.up * 1.6f, head = pc.transform.position + Vector3.up * 1.5f, to = head - eye;
            float range = pc.IsCrouching ? SightRange * 0.55f : SightRange;
            if (to.magnitude > range || Vector3.Angle(Courier.forward, new Vector3(to.x, 0f, to.z)) > GameOptions.SightHalfAngle + 20f) return false;
            return !Physics.Raycast(eye, to.normalized, to.magnitude - 0.4f, ~0, QueryTriggerInteraction.Ignore);
        }

        void Update()
        {
            if (!Built || Courier == null || Now == Phase.Done) return;
            var pc = ZoneKit.Player; if (pc == null) return;
            if (!active)
            {   // the tail starts when the story points here and the player comes near the tea stall
                if (!StoryWantsMe() || Vector3.Distance(pc.transform.position, Courier.position) > 32f) return;
                active = true; Now = Phase.Walking; leg = 1; Checkpoints.Save(ZoneId, "tail", pc.transform.position, pc.transform.eulerAngles.y, DawnClock.Instance != null ? DawnClock.Instance.Minutes * 60f : 0f);
                StoryDialogueSystem.Instance?.Say("v3:bazaar_begin"); Debug.Log("[Bazaar] the courier sets off");
            }
            ScoreSystem.AddTime(ZoneId, Time.deltaTime);
            float dist = Vector3.Distance(pc.transform.position, Courier.position);
            ringT -= Time.deltaTime; if (ringT <= 0f && ring != null && Now != Phase.Fight) { ringT = Now == Phase.Running ? 0.7f : lostT > 0f ? 1.6f : 3.2f; ring.pitch = Random.Range(0.97f, 1.03f); ring.volume = lostT > 0f ? 1f : 0.7f; ring.Play(); }
            switch (Now)
            {
                case Phase.Walking:
                case Phase.Running:
                    {
                        bool far = dist > 42f && Now == Phase.Walking;
                        lostT = far ? lostT + Time.deltaTime : 0f;
                        if (lostT > 4f) break;   // fallen behind: he waits for the player, ringing louder (a soft fail, no restart)
                        float speed = Now == Phase.Running ? 5.2f : 1.7f;
                        Vector3 target = Route[leg]; Vector3 d = target - Courier.position; d.y = 0f;
                        if (d.magnitude < 0.25f)
                        {
                            if (stops.Contains(leg) && Now == Phase.Walking) { Now = Phase.LookingBack; lookT = 0f; }
                            leg++; if (leg >= Route.Count) { Now = Phase.Arrived; break; }
                        }
                        else { Courier.position += d.normalized * Mathf.Min(speed * Time.deltaTime, d.magnitude); Courier.rotation = Quaternion.Slerp(Courier.rotation, Quaternion.LookRotation(d), Time.deltaTime * 6f); }
                        break;
                    }
                case Phase.LookingBack:
                    {
                        lookT += Time.deltaTime;
                        Vector3 back = Route[Mathf.Max(0, leg - 2)] - Courier.position; back.y = 0f;
                        if (back.sqrMagnitude > 0.01f) Courier.rotation = Quaternion.Slerp(Courier.rotation, Quaternion.LookRotation(back), Time.deltaTime * 4f);
                        if (lookT > 0.6f && lookT < 2.2f && Sees(pc))
                        {
                            Spotted++; ScoreSystem.Spotted(ZoneId); lookT = 2.2f; Debug.Log($"[Bazaar] spotted ({Spotted} of 3) at {dist:0.0} m");
                            ObjectiveBanner.Show(Spotted >= 3 ? "HE SAW YOU" : "HE LOOKED BACK", Spotted >= 3 ? "He is running." : "Keep your distance. Stay out of sight.", 2.5f);
                            if (Spotted >= 3) { Now = Phase.Running; StoryDialogueSystem.Instance?.Say("v3:bazaar_run"); break; }
                        }
                        if (lookT > 2.6f) Now = Phase.Walking;
                        break;
                    }
                case Phase.Arrived:
                    if (dist < 13f)
                    {   // cornered in the yard: the courier and a helper fight
                        Now = Phase.Fight; Courier.gameObject.SetActive(false);
                        StoryDialogueSystem.Instance?.Say("v3:bazaar_cornered");
                        Brawl.Begin(ZoneId, Yard.TransformPoint(new Vector3(0f, 0f, 0.5f)), 5f, new[] { Brawl.Robber("the courier", 1, BrawlType.Knife, 0.95f, new Color(0.72f, 0.6f, 0.45f)), Brawl.Robber("his helper", 2, BrawlType.Brawler, 1f, new Color(0.55f, 0.55f, 0.6f)) }, _ => Won());
                    }
                    break;
            }
        }

        void Won()
        {
            Now = Phase.Done; Bell.enabled = true; foreach (var r in Bell.GetComponentsInChildren<Renderer>()) r.enabled = true;
            if (PuzzleSave.Get("evidence.list") == 0) { PuzzleSave.Set("evidence.list", 1); ObjectiveBanner.Show("NOTED", "A list of five items. Neat handwriting, ticks of two strokes.", 4f); }
            ScoreSystem.PuzzleSolved(ZoneId, 0, 0f);   // the tail counts as this zone's puzzle
        }
    }
}
