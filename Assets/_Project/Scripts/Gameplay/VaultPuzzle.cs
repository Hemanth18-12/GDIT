using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// V3 zone 3 (Storage Vaults), built as a windowless stone vault hall: near dark.
    /// 1. Two floor plates hold the bar gate open. Two stone blocks must be pushed onto them; one has to go round a pillar. A lever resets the blocks.
    /// 2. Beyond the gate, three stone discs. The answer is not written anywhere: it is the pattern the roof-grate light draws on the floor.
    /// 3. The stone chest opens: the Sun Rosette and the vault duty ledger.
    /// </summary>
    public sealed class VaultPuzzle : MonoBehaviour
    {
        public const string ZoneId = "vaults";
        public static readonly int[] Answer = { 2, 5, 3 };
        public static VaultPuzzle Instance { get; private set; }
        public bool Built { get; private set; }
        public PuzzleGroup Group { get; private set; }
        public PushableBlock BlockA { get; private set; }
        public PushableBlock BlockB { get; private set; }
        public PressurePlate[] Plates { get; private set; }
        public PuzzleLever ResetLever { get; private set; }
        public RotatingDisc[] Discs { get; private set; }
        public TreasurePickup Rosette { get; private set; }
        public bool GateOpen { get; private set; }
        Transform gate, lid; float gateT, lidT;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded += (s, m) => { if (s.name == Constants.Courtyard) Spawn(); }; if (SceneManager.GetActiveScene().name == Constants.Courtyard) Spawn(); }
        static void Spawn() { if (Instance == null) new GameObject("V3_Zone3_Vaults").AddComponent<VaultPuzzle>(); }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            float wait = 0f; while ((TempleSite.Instance == null || TempleSite.Footprint.size.sqrMagnitude < 1f) && wait < 20f) { wait += Time.deltaTime; yield return null; }
            var b = TempleSite.Footprint; if (b.size.sqrMagnitude < 1f) yield break;
            var cands = new System.Collections.Generic.List<(Vector3, Quaternion)>();
            foreach (float dx in new[] { 0f, 40f, -40f, 80f, -80f }) cands.Add((new Vector3(b.center.x + dx, Village.Top, b.min.z - 13.5f), Quaternion.identity));
            if (!ZoneKit.ClearGround(cands, new Vector3(13.4f, 2.6f, 6.6f), out var at, out var face)) Debug.LogWarning("[Vaults] no clear ground behind the temple; using the last candidate");
            Build(at, face);
        }

        PushableBlock Block(Transform root, string name, Vector3 pos)
        {
            var g = Village.Box(root, name, pos, new Vector3(0.9f, 0.9f, 0.9f), "sandstone_blocks_08", 1f);
            var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true;
            var bl = g.AddComponent<PushableBlock>(); bl.elementId = "z3." + name; bl.step = 1f; bl.radius = 1.9f; return bl;
        }
        PressurePlate Plate(Transform root, string name, Vector3 pos)
        {
            var g = ZoneKit.Part(root, name, pos, new Vector3(0.95f, 0.08f, 0.95f), ZoneKit.Tinted("red_sandstone_pavement", new Color(0.55f, 0.4f, 0.28f), 0.3f), false);
            var t = new GameObject(name + "_Trigger"); t.transform.SetParent(root, false); t.transform.localPosition = pos + Vector3.up * 0.35f;
            var bc = t.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.6f, 0.6f, 0.6f);
            var p = t.AddComponent<PressurePlate>(); p.elementId = "z3." + name; p.plate = g.transform; return p;
        }

        public void Build(Vector3 at, Quaternion facing)
        {
            transform.SetPositionAndRotation(at, facing); var root = transform;
            const string wall = "red_sandstone_wall"; const float H = 4.2f;
            Village.Box(root, "Vault_Floor", new Vector3(0f, 0.02f, 0f), new Vector3(24.6f, 0.04f, 10.6f), "red_laterite_soil_stones", 2.2f, false);
            Village.Box(root, "Vault_N", new Vector3(0f, H / 2f, 5.3f), new Vector3(25.2f, H, 0.6f), wall, 2.4f);
            Village.Box(root, "Vault_S", new Vector3(0f, H / 2f, -5.3f), new Vector3(25.2f, H, 0.6f), wall, 2.4f);
            Village.Box(root, "Vault_E", new Vector3(12.3f, H / 2f, 0f), new Vector3(0.6f, H, 10.6f), wall, 2.4f);
            Village.Box(root, "Vault_W_N", new Vector3(-12.3f, H / 2f, 3.1f), new Vector3(0.6f, H, 3.8f), wall, 2.4f);
            Village.Box(root, "Vault_W_S", new Vector3(-12.3f, H / 2f, -3.1f), new Vector3(0.6f, H, 3.8f), wall, 2.4f);
            Village.Box(root, "Vault_W_Lintel", new Vector3(-12.3f, 3.5f, 0f), new Vector3(0.6f, 1.4f, 2.4f), wall, 2.4f);
            Village.Box(root, "Vault_Roof", new Vector3(0f, H + 0.15f, 0f), new Vector3(25.8f, 0.3f, 11.4f), "clay_roof_tiles_02", 2.4f);
            Village.Box(root, "Vault_Mid_N", new Vector3(-1f, H / 2f, 3.2f), new Vector3(0.5f, H, 3.8f), wall, 2.4f);
            Village.Box(root, "Vault_Mid_S", new Vector3(-1f, H / 2f, -3.2f), new Vector3(0.5f, H, 3.8f), wall, 2.4f);
            Village.Box(root, "Vault_Mid_Lintel", new Vector3(-1f, 3.6f, 0f), new Vector3(0.5f, 1.2f, 2.6f), wall, 2.4f);
            Village.Box(root, "Pillar", new Vector3(-5f, H / 2f, -1f), new Vector3(0.9f, H, 0.9f), "sandstone_blocks_08", 1.6f);
            // the bar gate in the middle wall
            gate = new GameObject("BarGate").transform; gate.SetParent(root, false); gate.localPosition = new Vector3(-1f, 0f, 0f);
            for (int i = 0; i < 7; i++) Village.Box(gate, "Bar_" + i, new Vector3(0f, 1.5f, -1.2f + i * 0.4f), new Vector3(0.09f, 3f, 0.09f), "weathered_planks", 1f);
            var blocker = gate.gameObject.AddComponent<BoxCollider>(); blocker.center = new Vector3(0f, 1.5f, 0f); blocker.size = new Vector3(0.3f, 3f, 2.6f);

            BlockA = Block(root, "block_a", new Vector3(-8f, 0.45f, 3f)); BlockB = Block(root, "block_b", new Vector3(-6f, 0.45f, -1f));
            Plates = new[] { Plate(root, "plate_north", new Vector3(-4f, 0.05f, 3f)), Plate(root, "plate_south", new Vector3(-4f, 0.05f, -3f)) };
            var lg = Village.Box(root, "ResetLever", new Vector3(-11.6f, 1.2f, -2.2f), new Vector3(0.12f, 0.7f, 0.12f), "medieval_wood", 1f, false); ZoneKit.Reach(lg, 1f);
            ResetLever = lg.AddComponent<PuzzleLever>(); ResetLever.elementId = "z3.reset"; ResetLever.verb = "Pull the reset lever"; ResetLever.springBackSeconds = 0.6f; ResetLever.radius = 2.2f; ResetLever.pulledEuler = new Vector3(0f, 0f, -50f);
            ResetLever.Changed += e => { if (e.State == 1) { BlockA.ResetState(); BlockB.ResetState(); Debug.Log("[Vaults] blocks reset by the lever"); } };

            // the light from the roof grate and the pattern it draws: three rings, each with one bright mark
            var shaftGo = new GameObject("GrateLight"); shaftGo.transform.SetParent(root, false); shaftGo.transform.localPosition = new Vector3(5f, H - 0.2f, 0f); shaftGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var sl = shaftGo.AddComponent<Light>(); sl.type = LightType.Spot; sl.color = new Color(0.62f, 0.78f, 1f); sl.intensity = 60f; sl.range = 8f; sl.spotAngle = 62f; sl.innerSpotAngle = 40f; sl.shadows = LightShadows.Soft;
            var dim = ZoneKit.Tinted("clay_plaster", new Color(0.22f, 0.26f, 0.3f), 0.2f); var bright = ZoneKit.Tinted("clay_plaster", new Color(0.8f, 0.9f, 1f), 0.4f, new Color(1.2f, 1.6f, 2.4f));
            float[] ringR = { 0.6f, 1.05f, 1.5f };
            for (int r = 0; r < 3; r++)
            {
                for (int k = 0; k < 24; k++) { float a = k * 15f * Mathf.Deg2Rad; var seg = ZoneKit.Part(root, "FloorRing", new Vector3(5f + Mathf.Cos(a) * ringR[r], 0.05f, Mathf.Sin(a) * ringR[r]), new Vector3(0.05f, 0.02f, ringR[r] * 0.24f), dim, false); seg.transform.localRotation = Quaternion.Euler(0f, -k * 15f, 0f); }
                float th = Answer[r] * 60f * Mathf.Deg2Rad;
                ZoneKit.Part(root, "FloorMark_" + r, new Vector3(5f + Mathf.Cos(th) * ringR[r], 0.06f, Mathf.Sin(th) * ringR[r]), new Vector3(0.22f, 0.03f, 0.22f), bright, false);
            }
            // three discs on the east wall: small, middle, large (matching the three rings); each has one notch
            Discs = new RotatingDisc[3]; float[] size = { 1.0f, 1.4f, 1.8f }; float[] dz = { 3f, 0f, -3f };
            var stone = ZoneKit.Tinted("sandstone_blocks_08", new Color(0.75f, 0.68f, 0.58f), 0.3f);
            for (int i = 0; i < 3; i++)
            {
                var g = new GameObject("disc_" + i); g.transform.SetParent(root, false); g.transform.localPosition = new Vector3(11.85f, 1.7f, dz[i]);
                var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(0.25f, size[i], size[i]);
                ZoneKit.Round(g.transform, "Stone", Vector3.zero, new Vector3(size[i], 0.1f, size[i]), stone).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                ZoneKit.Part(g.transform, "Notch", new Vector3(-0.12f, size[i] * 0.4f, 0f), new Vector3(0.08f, size[i] * 0.16f, 0.1f), bright, false);
                var d = g.AddComponent<RotatingDisc>(); d.elementId = "z3.disc" + i; d.steps = 6; d.axis = Vector3.right; d.verb = "Turn the stone disc"; d.radius = 2.4f; d.turnSeconds = 0.4f; Discs[i] = d;
            }
            // the stone chest with the Sun Rosette
            Village.Box(root, "Chest", new Vector3(9f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f), "sandstone_blocks_08", 1f);
            var lh = new GameObject("LidHinge").transform; lh.SetParent(root, false); lh.localPosition = new Vector3(9.6f, 0.72f, 0f); lid = lh;
            Village.Box(lh, "ChestLid", new Vector3(-0.6f, 0.06f, 0f), new Vector3(1.24f, 0.12f, 0.84f), "sandstone_blocks_08", 1f, false);
            var ros = ArtUpgrade.Place(root, "carved_wooden_plate", root.TransformPoint(new Vector3(9f, 0.78f, 0f)), 1.4f, 0f, false);
            if (ros == null) ros = ZoneKit.Round(root, "Rosette", new Vector3(9f, 0.8f, 0f), new Vector3(0.5f, 0.04f, 0.5f), stone);
            foreach (var c in ros.GetComponentsInChildren<Collider>()) Destroy(c); foreach (var r in ros.GetComponentsInChildren<Renderer>()) r.sharedMaterial = ZoneKit.Tinted("sandstone_blocks_08", new Color(0.86f, 0.74f, 0.55f), 0.35f);
            ZoneKit.Reach(ros, 2.2f);
            Rosette = ros.AddComponent<TreasurePickup>(); Rosette.treasureName = "the Sun Rosette"; Rosette.zoneId = ZoneId; Rosette.enabled = false;
            ArtUpgrade.Place(root, "WoodenTable_01", root.TransformPoint(new Vector3(3f, 0f, -4.2f)), 1f, 0f, false);
            ArtUpgrade.Place(root, "vintage_oil_lamp", root.TransformPoint(new Vector3(-10.5f, 0f, 4.2f)), 1.4f, 0f, false);
            var el = new GameObject("EntryLamp"); el.transform.SetParent(root, false); el.transform.localPosition = new Vector3(-10.5f, 0.9f, 4.2f); var ell = el.AddComponent<Light>(); ell.type = LightType.Point; ell.color = new Color(1f, 0.62f, 0.3f); ell.range = 9f; ell.intensity = 5f; el.AddComponent<LightFlicker>();

            // art pass: plinths and coping inside, the plain pillar becomes a carved one, an arch over the entrance, lamp stands
            ZoneDress.Trim(root, "Vault_N", "Vault_S", "Vault_E", "Vault_W_N", "Vault_W_S", "Vault_Mid_N", "Vault_Mid_S");
            ZoneDress.PillarOver(root.Find("Pillar"));
            ZoneDress.Prop(root, "SM_V3_Arch", new Vector3(-12.95f, 0f, 0f), 90f, new Vector3(0.62f, 0.75f, 1f));
            ZoneDress.Lamp(root, new Vector3(-9.5f, 0.04f, -4.3f), 7f, 3.5f); ZoneDress.Lamp(root, new Vector3(10.8f, 0.04f, 4.3f), 6f, 2.5f);
            var gg = new GameObject("Puzzle_Vaults"); gg.transform.SetParent(root, false); gg.transform.localPosition = Vector3.zero;
            Group = gg.AddComponent<PuzzleGroup>(); Group.puzzleId = "z3_vaults"; Group.zoneId = ZoneId; Group.hintRadius = 16f;
            Group.hints = new[] { "Two floor plates hold the gate. Stone is heavier than a man.", "The light from the roof draws three rings on the floor. Each ring has one bright mark.", "Push a block onto each plate (one must go round the pillar; the lever puts them back). Then turn the small, middle and large discs until their notches sit where the bright marks are." };
            for (int i = 0; i < 3; i++) Group.conditions.Add(new PuzzleGroup.Condition { element = Discs[i], state = Answer[i] });
            Group.OnSolved += _ => { GameSfx.At("sfx_door_stone", lid.position, 0.8f, 0.05f, 32f, 1.2f); if (PuzzleSave.Get("evidence.ledger") == 0) { PuzzleSave.Set("evidence.ledger", 1); ObjectiveBanner.Show("NOTED", "The vault duty ledger. Signed \"M.\"", 4f); } };

            var lookGo = new GameObject("ZoneLook_Vaults"); lookGo.transform.SetParent(root, false); lookGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var look = lookGo.AddComponent<ZoneLook>(); look.zone = ZoneLook.Zone.Vaults; look.size = new Vector3(24.4f, 4.4f, 10.4f); look.blendDistance = 2f; look.Build();
            ZoneAtmosphere.Register(look, Quaternion.Euler(50f, 0f, 0f));
            Physics.SyncTransforms(); Built = true;
            Debug.Log($"[Vaults] zone 3 puzzle built at {at:0.0}");
        }

        void Update()
        {
            if (!Built) return;
            bool wasOpen = GateOpen;
            GateOpen = Group.Solved || (Plates[0].State == 1 && Plates[1].State == 1) || PuzzleSave.Get("z3.gate_passed") == 1;
            if (GateOpen && !Group.Solved && PuzzleSave.Get("z3.gate_passed") == 0) { var pc = ZoneKit.Player; if (pc != null && transform.InverseTransformPoint(pc.transform.position).x > 0.5f) PuzzleSave.Set("z3.gate_passed", 1); }   // once through, it never shuts the player in
            if (GateOpen != wasOpen && Time.timeSinceLevelLoad > 5f) GameSfx.At("sfx_door_stone", gate.position + Vector3.up * 1.5f, 1f);
            gateT = Mathf.MoveTowards(gateT, GateOpen ? 1f : 0f, Time.deltaTime * 0.7f); gate.localPosition = new Vector3(-1f, 3.1f * Mathf.SmoothStep(0f, 1f, gateT), 0f);
            bool open = Group.Solved;
            if (open && !Rosette.enabled && !Rosette.Taken)
            {   // the two vault guards come for whoever opened the chest
                if (Brawl.WonBefore(ZoneId)) Rosette.enabled = true;
                else if (Brawl.Active == null) Brawl.Begin(ZoneId, transform.TransformPoint(new Vector3(5.5f, 0f, 0f)), 4.2f, new[] { Brawl.Robber("the heavy guard", 1, BrawlType.Heavy, 1.12f, new Color(0.5f, 0.42f, 0.36f)), Brawl.Robber("the vault guard", 1, BrawlType.Brawler, 1f, new Color(0.7f, 0.62f, 0.5f)) }, null);
            }
            lidT = Mathf.MoveTowards(lidT, open ? 1f : 0f, Time.deltaTime * 0.6f); lid.localRotation = Quaternion.Euler(0f, 0f, -95f * Mathf.SmoothStep(0f, 1f, lidT));
        }
    }
}
