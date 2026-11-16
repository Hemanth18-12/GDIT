using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// V3 zone 2 (Temple Gate and Courtyard): rebuild the break-in with physical evidence. See Docs/LEVEL_DESIGN_V3.md.
    /// 1. Three shards of the broken oil pot lie apart. Fitted back on the plinth, the pot holds oil again and the lantern lights.
    /// 2. The lit lantern, stood on the oil stain where the robber's lamp stood, throws the grille's shadow and lights one loose flagstone.
    /// 3. The lock plate turns in the hand: the scratches are on the inside (evidence for the inside man).
    /// 4. The loose flagstone lifts: the rope, and the Palm-Leaf Chronicle the lookout hid.
    /// Nothing is "examined" for an answer; every step is grab, carry, place, turn or lift. H gives three hints.
    /// </summary>
    public sealed class BreakInPuzzle : MonoBehaviour
    {
        public const string ZoneId = "courtyard";
        public static BreakInPuzzle Instance { get; private set; }
        public bool Built { get; private set; }
        public PuzzleGroup Group { get; private set; }
        public Carryable[] Shards { get; private set; }
        public PlacementSocket[] Sockets { get; private set; }
        public Carryable Lantern { get; private set; }
        public ProximityMatch LampSpot { get; private set; }
        public RotatingDisc LockPlate { get; private set; }
        public PuzzleLever Flagstone { get; private set; }
        public TreasurePickup Chronicle { get; private set; }
        public Transform Plinth { get; private set; }
        public Transform Stain { get; private set; }
        public bool PotWhole { get; private set; }
        Light lanternLight; GameObject wholePot; bool checkpointed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded += (s, m) => { if (s.name == Constants.Courtyard) Spawn(); };
            if (SceneManager.GetActiveScene().name == Constants.Courtyard) Spawn();
        }
        static void Spawn() { if (Instance == null) new GameObject("V3_Zone2_BreakIn").AddComponent<BreakInPuzzle>(); }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            float wait = 0f;
            while ((TempleSite.Instance == null || TempleSite.Footprint.size.sqrMagnitude < 1f) && wait < 20f) { wait += Time.deltaTime; yield return null; }
            if (TempleSite.Footprint.size.sqrMagnitude < 1f) { Debug.LogWarning("[BreakIn] temple site not found; puzzle not built"); yield break; }
            // stand beside the temple road on the first piece of clear ground (never inside a house, a tree or the temple)
            float z0 = TempleSite.Footprint.max.z; Vector3 at = Vector3.zero; Quaternion face = Quaternion.identity; bool found = false;
            foreach (var (dx, dz) in new[] { (17f, 7f), (-17f, 7f), (24f, 5f), (-24f, 5f), (17f, 14f), (-17f, 14f), (32f, 8f), (-32f, 8f) })
            {
                at = new Vector3(Village.Centre.x + dx, Village.Top, z0 + dz); face = Quaternion.LookRotation(dx > 0f ? Vector3.left : Vector3.right);
                Vector3 centre = at + face * new Vector3(0f, 2.6f, 1.5f);
                if (!Physics.CheckBox(centre, new Vector3(6.2f, 2.2f, 6.2f), face, ~0, QueryTriggerInteraction.Ignore)) { found = true; break; }
            }
            if (!found) Debug.LogWarning("[BreakIn] no clear ground found beside the temple road; using the last candidate");
            Build(at, face);
        }

        static Material Tinted(string source, Color c, float smooth)
        {
            var src = ArtUpgrade.Mat(source); var m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); return m;
        }
        static void Hit(GameObject g, float r) { var s = g.AddComponent<SphereCollider>(); s.isTrigger = true; s.radius = r; }

        /// <summary>Build the storeroom front and every puzzle part at a place. Public so a test can build it on empty ground.</summary>
        public void Build(Vector3 at, Quaternion facing)
        {
            transform.SetPositionAndRotation(at, facing); var root = transform;
            // ---- the storeroom front (textured stone, tiled roof, barred doorway)
            Village.Box(root, "Store_Back", new Vector3(0f, 2f, -3f), new Vector3(9f, 4f, 0.4f), "red_sandstone_wall", 2.4f);
            Village.Box(root, "Store_SideL", new Vector3(-4.5f, 2f, -1.5f), new Vector3(0.4f, 4f, 3.4f), "red_sandstone_wall", 2.4f);
            Village.Box(root, "Store_SideR", new Vector3(4.5f, 2f, -1.5f), new Vector3(0.4f, 4f, 3.4f), "red_sandstone_wall", 2.4f);
            Village.Box(root, "Store_FrontL", new Vector3(-2.95f, 2f, 0f), new Vector3(3.3f, 4f, 0.4f), "sandstone_blocks_08", 2.2f);
            Village.Box(root, "Store_FrontR", new Vector3(2.95f, 2f, 0f), new Vector3(3.3f, 4f, 0.4f), "sandstone_blocks_08", 2.2f);
            Village.Box(root, "Store_Lintel", new Vector3(0f, 3.55f, 0f), new Vector3(2.6f, 0.9f, 0.4f), "sandstone_blocks_08", 2.2f);
            Village.Box(root, "Store_Roof", new Vector3(0f, 4.15f, -1.3f), new Vector3(10f, 0.3f, 4.4f), "clay_roof_tiles_02", 2f);
            Village.Box(root, "Store_Floor", new Vector3(0f, 0.02f, -1.5f), new Vector3(8.6f, 0.04f, 3f), "red_sandstone_pavement", 2.6f, false);
            Village.Box(root, "Store_Apron", new Vector3(0f, 0.03f, 3.2f), new Vector3(11f, 0.06f, 6.4f), "red_sandstone_pavement", 2.6f, false);
            for (int i = 0; i < 7; i++) Village.Box(root, "Grille_Bar_" + i, new Vector3(-1.2f + i * 0.4f, 1.55f, 0f), new Vector3(0.07f, 3.1f, 0.07f), "weathered_planks", 1f);
            Village.Box(root, "Grille_Rail", new Vector3(0f, 1.2f, 0f), new Vector3(2.6f, 0.08f, 0.09f), "weathered_planks", 1f, false);
            // five empty cloth-lined stands inside, seen through the bars
            for (int i = 0; i < 5; i++) { Village.Box(root, "Stand_" + i, new Vector3(-3.2f + i * 1.6f, 0.45f, -2.3f), new Vector3(0.7f, 0.9f, 0.7f), "medieval_wood", 1.2f, false); Village.Box(root, "StandCloth_" + i, new Vector3(-3.2f + i * 1.6f, 0.92f, -2.3f), new Vector3(0.62f, 0.04f, 0.62f), "clay_plaster", 0.8f, false).GetComponent<Renderer>().sharedMaterial = Tinted("clay_plaster", new Color(0.45f, 0.08f, 0.07f), 0.15f); }
            ArtUpgrade.Place(root, "brass_pot_01", root.TransformPoint(new Vector3(3.6f, 0f, 1.3f)), 1.3f, 40f);
            ArtUpgrade.Place(root, "brass_pot_02", root.TransformPoint(new Vector3(-4.3f, 0f, 3.6f)), 1.2f, 110f);
            ArtUpgrade.Place(root, "ceramic_pot", root.TransformPoint(new Vector3(4.2f, 0f, 3.9f)), 1.2f, 0f);

            // ---- 1. the broken pot: plinth, three sockets, three shards
            var plinthGo = Village.Box(root, "Plinth", new Vector3(-3.2f, 0.4f, 1.3f), new Vector3(0.8f, 0.8f, 0.8f), "sandstone_blocks_08", 1.6f); Plinth = plinthGo.transform;
            Vector3 potBase = new Vector3(-3.2f, 0.81f, 1.3f);
            var terracotta = Tinted("clay_plaster", new Color(0.62f, 0.3f, 0.17f), 0.25f);
            var potPf = Resources.Load<GameObject>("Props/V3/BreakInPot");
            Shards = new Carryable[3]; Sockets = new PlacementSocket[3];
            Vector3[] lie = { new Vector3(-1.3f, 0.12f, 2.7f), new Vector3(2.5f, 0.12f, 4.6f), new Vector3(3.95f, 0.12f, 0.75f) };
            if (potPf != null)
            {
                var pot = Instantiate(potPf, root); pot.name = "BrokenPot"; pot.transform.localPosition = potBase; pot.transform.localRotation = Quaternion.identity;
                foreach (var r in pot.GetComponentsInChildren<Renderer>()) r.sharedMaterial = terracotta;
                wholePot = pot.transform.Find("SM_Pot_Whole") != null ? pot.transform.Find("SM_Pot_Whole").gameObject : null; if (wholePot != null) wholePot.SetActive(false);
                for (int i = 0; i < 3; i++)
                {
                    var sh = pot.transform.Find("SM_Pot_Shard_" + (i + 1)); if (sh == null) continue;
                    var seat = new GameObject("Seat_" + (i + 1)).transform; seat.SetParent(root, false); seat.SetPositionAndRotation(sh.position, sh.rotation);
                    var so = new GameObject("PotSocket_" + (i + 1)); so.transform.SetParent(root, false); so.transform.localPosition = potBase; Hit(so, 0.7f);
                    var socket = so.AddComponent<PlacementSocket>(); socket.elementId = "z2.socket" + (i + 1); socket.acceptsKey = "shard" + (i + 1); socket.seat = seat; socket.radius = 2.2f; Sockets[i] = socket;
                    sh.SetParent(root, true); sh.localPosition = lie[i]; sh.localRotation = Quaternion.Euler(70f + i * 25f, i * 130f, 15f); Hit(sh.gameObject, 0.45f);
                    var c = sh.gameObject.AddComponent<Carryable>(); c.itemKey = "shard" + (i + 1); c.displayName = "pot shard"; Shards[i] = c;
                }
            }
            else Debug.LogWarning("[BreakIn] Resources/Props/V3/BreakInPot missing");

            // ---- 2. the lantern and the oil stain
            var stainGo = Village.Box(root, "OilStain", new Vector3(0.95f, 0.066f, 1.25f), new Vector3(1.3f, 0.012f, 0.8f), "red_sandstone_pavement", 2.6f, false);
            stainGo.GetComponent<Renderer>().sharedMaterial = Tinted("red_sandstone_pavement", new Color(0.08f, 0.06f, 0.04f), 0.93f); Stain = stainGo.transform;
            Village.Box(root, "OilTrail", new Vector3(-1.1f, 0.064f, 1.3f), new Vector3(2.9f, 0.01f, 0.22f), "red_sandstone_pavement", 2.6f, false).GetComponent<Renderer>().sharedMaterial = stainGo.GetComponent<Renderer>().sharedMaterial;
            var lanternGo = ArtUpgrade.Place(root, "wooden_lantern_01", root.TransformPoint(new Vector3(-3.3f, 0f, 2.5f)), 1.6f, 0f);
            if (lanternGo == null) { lanternGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder); lanternGo.transform.SetParent(root, false); lanternGo.transform.localPosition = new Vector3(-3.3f, 0.25f, 2.5f); lanternGo.transform.localScale = new Vector3(0.2f, 0.25f, 0.2f); }
            lanternGo.name = "Lantern"; foreach (var col in lanternGo.GetComponentsInChildren<Collider>()) Destroy(col); Hit(lanternGo, 0.5f);
            Lantern = lanternGo.AddComponent<Carryable>(); Lantern.itemKey = "lantern"; Lantern.displayName = "lantern"; Lantern.holdOffset = new Vector3(0.25f, 0.75f, 0.6f); Lantern.enabled = false;   // no oil yet
            var lg = new GameObject("LanternLight"); lg.transform.SetParent(lanternGo.transform, false); lg.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            lanternLight = lg.AddComponent<Light>(); lanternLight.type = LightType.Point; lanternLight.color = new Color(1f, 0.72f, 0.38f); lanternLight.range = 11f; lanternLight.intensity = 9f; lanternLight.shadows = LightShadows.Soft; lanternLight.enabled = false;
            lg.AddComponent<LightFlicker>();
            var matchGo = new GameObject("LampSpot"); matchGo.transform.SetParent(root, false);
            LampSpot = matchGo.AddComponent<ProximityMatch>(); LampSpot.elementId = "z2.lampspot"; LampSpot.subject = lanternGo.transform; LampSpot.target = Stain; LampSpot.tolerance = 0.95f;

            // ---- 3. the lock plate on the door jamb (turn it: the scratches are inside)
            var plateGo = Village.Box(root, "LockPlate", new Vector3(-1.48f, 1.25f, 0.24f), new Vector3(0.34f, 0.46f, 0.05f), "medieval_wood", 0.6f, false); Hit(plateGo, 2.2f);
            plateGo.GetComponent<Renderer>().sharedMaterial = Tinted("medieval_wood", new Color(0.5f, 0.38f, 0.16f), 0.6f);
            var scratch = Village.Box(plateGo.transform, "Scratches", new Vector3(0f, 0f, -0.55f), new Vector3(0.7f, 0.5f, 0.08f), "medieval_wood", 0.6f, false);
            scratch.GetComponent<Renderer>().sharedMaterial = Tinted("weathered_planks", new Color(0.85f, 0.8f, 0.7f), 0.2f);
            LockPlate = plateGo.AddComponent<RotatingDisc>(); LockPlate.elementId = "z2.lockplate"; LockPlate.steps = 4; LockPlate.verb = "Turn lock plate"; LockPlate.radius = 2f;

            // ---- 4. the loose flagstone, the rope and the hidden Chronicle
            Village.Box(root, "Pit", new Vector3(0.95f, -0.16f, 3.3f), new Vector3(1.0f, 0.4f, 1.0f), "red_laterite_soil_stones", 1.2f, false);
            var hinge = new GameObject("FlagstoneHinge").transform; hinge.SetParent(root, false); hinge.localPosition = new Vector3(0.95f, 0.07f, 3.85f);
            var slab = Village.Box(hinge, "Flagstone", new Vector3(0f, 0.02f, -0.55f), new Vector3(1.12f, 0.12f, 1.12f), "red_sandstone_pavement", 1.4f, false); Hit(slab, 0.6f);
            slab.GetComponent<Renderer>().sharedMaterial = Tinted("red_sandstone_pavement", new Color(0.86f, 0.8f, 0.76f), 0.3f);
            Flagstone = slab.AddComponent<PuzzleLever>(); Flagstone.elementId = "z2.flagstone"; Flagstone.handle = hinge; Flagstone.pulledEuler = new Vector3(72f, 0f, 0f); Flagstone.verb = "Lift flagstone"; Flagstone.radius = 2f; Flagstone.locked = true;
            var rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(rope.GetComponent<Collider>()); rope.name = "RopeCoil"; rope.transform.SetParent(root, false); rope.transform.localPosition = new Vector3(0.75f, 0.03f, 3.15f); rope.transform.localScale = new Vector3(0.42f, 0.035f, 0.42f); rope.GetComponent<Renderer>().sharedMaterial = Tinted("thatch_roof_angled", new Color(0.7f, 0.6f, 0.4f), 0.1f);
            var book = Village.Box(root, "PalmLeafChronicle", new Vector3(1.15f, 0.09f, 3.4f), new Vector3(0.42f, 0.07f, 0.13f), "weathered_planks", 0.5f, false); Hit(book, 3.5f);
            book.GetComponent<Renderer>().sharedMaterial = Tinted("weathered_planks", new Color(0.78f, 0.66f, 0.4f), 0.2f);
            Chronicle = book.AddComponent<TreasurePickup>(); Chronicle.treasureName = "the Palm-Leaf Chronicle"; Chronicle.zoneId = ZoneId; Chronicle.enabled = false;

            // ---- the puzzle itself
            var gg = new GameObject("Puzzle_BreakIn"); gg.transform.SetParent(root, false); gg.transform.localPosition = new Vector3(0f, 0f, 2f);
            Group = gg.AddComponent<PuzzleGroup>(); Group.puzzleId = "z2_breakin"; Group.zoneId = ZoneId; Group.hintRadius = 22f; Group.lockWhenSolved = false;
            Group.hints = new[] { "The pot broke here. Its pieces did not stay here.", "A lit lantern on the oil stain shows which stone is loose.", "Fit the three shards on the plinth, stand the lantern on the dark stain by the bars, then lift the pale flagstone in front of it." };
            foreach (var s in Sockets) if (s != null) { Group.conditions.Add(new PuzzleGroup.Condition { element = s, state = 1 }); s.Changed += _ => Refresh(); }
            Group.conditions.Add(new PuzzleGroup.Condition { element = Flagstone, state = 1 });
            LampSpot.Changed += _ => Refresh(); LockPlate.Changed += _ => Refresh(); Flagstone.Changed += _ => Refresh();

            // ---- the zone's look (dawn-gold grade) around the site
            var lookGo = new GameObject("ZoneLook_Courtyard"); lookGo.transform.SetParent(root, false); lookGo.transform.localPosition = new Vector3(0f, 10f, 3f);
            var look = lookGo.AddComponent<ZoneLook>(); look.zone = ZoneLook.Zone.Courtyard; look.size = new Vector3(70f, 40f, 70f); look.blendDistance = 14f; look.Build();
            // art pass: carved coping and plinths, pillars at the door, brass lamp stands at the corners
            ZoneDress.Trim(root, "Store_");
            ZoneDress.Prop(root, "SM_V3_Pillar", new Vector3(-1.55f, 0f, 0.45f)); ZoneDress.Prop(root, "SM_V3_Pillar", new Vector3(1.55f, 0f, 0.45f));
            ZoneDress.Lamp(root, new Vector3(-4.9f, 0.06f, 1.2f), 7f, 4f); ZoneDress.Lamp(root, new Vector3(4.9f, 0.06f, 1.2f), 7f, 4f);
            Physics.SyncTransforms(); Built = true;
            if (PuzzleSave.Get("solved.z2_breakin") == 1)
            {   // loading a save where this puzzle is already solved: show the solved scene, not a half-reset one
                for (int i = 0; i < 3; i++) if (Sockets[i] != null && Shards[i] != null) Sockets[i].Place(Shards[i]);
                Lantern.transform.position = Stain.position + Vector3.up * 0.05f; LampSpot.Check();
            }
            Refresh();
            Debug.Log($"[BreakIn] zone 2 puzzle built at {at:0.0}");
        }

        /// <summary>React to any part changing: oil in the pot lights the lantern; the lantern on the stain frees the flagstone; the lifted stone shows the Chronicle.</summary>
        void Refresh()
        {
            if (!Built) return;
            bool whole = true; foreach (var s in Sockets) if (s == null || s.State != 1) whole = false;
            if (whole && !PotWhole)
            {
                PotWhole = true; if (wholePot != null) wholePot.SetActive(true);
                foreach (var sh in Shards) if (sh != null) { foreach (var r in sh.GetComponentsInChildren<Renderer>()) r.enabled = false; sh.enabled = false; }
                foreach (var s in Sockets) s.locked = true;
                Lantern.enabled = true; lanternLight.enabled = true;
                ObjectiveBanner.Show("THE POT HOLDS OIL AGAIN", "The lantern is lit.", 3.5f);
            }
            Flagstone.locked = !(PotWhole && LampSpot.State == 1) && Flagstone.State == 0;
            if (LockPlate.State == 2 && PuzzleSave.Get("evidence.inside_lock") == 0) { PuzzleSave.Set("evidence.inside_lock", 1); ObjectiveBanner.Show("NOTED", "Scratches on the inside. Opened, not forced.", 4f); }
            if (Flagstone.State == 1 && !Chronicle.enabled && !Chronicle.Taken)
            {   // the lookout was hiding on the ledge: the Chronicle is his until he is beaten
                if (Brawl.WonBefore(ZoneId)) Chronicle.enabled = true;
                else if (Brawl.Active == null) Brawl.Begin(ZoneId, transform.TransformPoint(new Vector3(0f, 0f, 6.5f)), 8f, new[] { Brawl.Robber("the lookout", 1, BrawlType.Knife, 0.95f, new Color(0.55f, 0.6f, 0.7f)) }, _ => Refresh());
            }
        }

        void Update()
        {
            if (!Built || checkpointed) return;
            var pc = FindAnyObjectByType<PlayerController>(); if (pc == null || Vector3.Distance(pc.transform.position, transform.position) > 16f) return;
            checkpointed = true; Checkpoints.Save(ZoneId, "storeroom", pc.transform.position, pc.transform.eulerAngles.y, DawnClock.Instance != null ? DawnClock.Instance.Minutes * 60f : 0f);
        }
    }
}
