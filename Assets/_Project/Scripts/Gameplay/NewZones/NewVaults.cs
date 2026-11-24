using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.World;
using LegacyOfTemples.Gameplay.Puzzles;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Replacement Zone 3: Underground Storage Vaults (near dark).
    /// 1. Pushable stone blocks onto two pressure plates to open the heavy iron bar gate. Lever resets blocks if stuck.
    /// 2. Gear train: place two loose gears onto gear pegs to connect the hand crank to the disc mechanism.
    /// 3. Three rotating stone discs matching the light arcs cast from the ceiling grate (Answer = 2, 5, 3).
    /// 4. Heavy vault guard combat in the treasure chamber.
    /// 5. On defeat, recovers Treasure 3: Carved Sandstone Rosette + vault ledger signed "M.".
    /// </summary>
    public sealed class NewVaults : MonoBehaviour
    {
        public const string Zone = "new_vaults";
        public static NewVaults Instance { get; private set; }
        public static readonly int[] DiscAnswer = { 2, 5, 3 };

        public enum Stage { Plates, Gears, Discs, Fight, Recover, Complete }
        public Stage Now { get; private set; }
        public bool Built { get; private set; }

        public PushableBlock BlockA { get; private set; }
        public PushableBlock BlockB { get; private set; }
        public PressurePlate[] Plates { get; private set; }
        public PuzzleLever ResetLever { get; private set; }

        public GearTrain Gears { get; private set; }
        public PlacementSocket[] GearPegs { get; private set; }
        public Carryable[] LooseGears { get; private set; }
        public RotatingDisc Crank { get; private set; }

        public RotatingDisc[] Discs { get; private set; }
        public TreasurePickup Rosette { get; private set; }
        public PuzzleGroup Group { get; private set; }

        public Vector3 ArenaCenter => new Vector3(0f, -5.8f, 130f);
        GameObject vaultGate;
        GameObject chestLid;

        void Awake() { Instance = this; Build(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Build()
        {
            var r = transform;
            // Underground level at y = -6 to -4
            Vector3 vaultOrigin = new Vector3(0f, -6f, 115f);

            // Vaulted underground hall
            MarketArt.Box(r, "Vault Floor", vaultOrigin + new Vector3(0f, 0f, 0f), new Vector3(36f, 0.4f, 48f), "sandstone_blocks_08");
            MarketArt.Box(r, "Vault Ceiling", vaultOrigin + new Vector3(0f, 6.2f, 0f), new Vector3(36f, 0.5f, 48f), "sandstone_blocks_08");
            MarketArt.Box(r, "Vault Wall North", vaultOrigin + new Vector3(0f, 3.1f, 24f), new Vector3(36f, 6f, 1.2f), "sandstone_blocks_08");
            MarketArt.Box(r, "Vault Wall South", vaultOrigin + new Vector3(0f, 3.1f, -24f), new Vector3(36f, 6f, 1.2f), "sandstone_blocks_08");
            MarketArt.Box(r, "Vault Wall West", vaultOrigin + new Vector3(-18f, 3.1f, 0f), new Vector3(1.2f, 6f, 48f), "sandstone_blocks_08");
            MarketArt.Box(r, "Vault Wall East", vaultOrigin + new Vector3(18f, 3.1f, 0f), new Vector3(1.2f, 6f, 48f), "sandstone_blocks_08");

            // Underground entrance stairs from courtyard
            for (int i = 0; i < 12; i++)
            {
                MarketArt.Box(r, "Stair " + i, new Vector3(25f, -i * 0.5f, 85f - i * 1.2f), new Vector3(3.5f, 0.5f, 1.3f), "sandstone_blocks_08");
            }

            // Heavy pillars supporting the stone vault
            for (int ix = -1; ix <= 1; ix += 2)
            {
                for (int iz = -1; iz <= 1; iz++)
                {
                    Vector3 pPos = vaultOrigin + new Vector3(ix * 8f, 3f, iz * 12f);
                    MarketArt.Box(r, "Vault Pillar", pPos, new Vector3(2f, 6f, 2f), "sandstone_blocks_08");
                    ArtUpgrade.Place(r, "brass_diya_lantern", pPos + Vector3.up * 1.5f + Vector3.forward * 1.1f, 1.1f, 0f, false);
                }
            }

            // Section 1: Pressure Plates and Pushable Blocks
            Plates = new PressurePlate[2];
            Vector3[] platePositions = { vaultOrigin + new Vector3(-6f, 0.25f, -8f), vaultOrigin + new Vector3(6f, 0.25f, -8f) };
            for (int i = 0; i < 2; i++)
            {
                var pObj = ZoneKit.Part(r, "PressurePlate_" + i, platePositions[i], new Vector3(1.8f, 0.1f, 1.8f), ZoneKit.Tinted("red_sandstone_pavement", new Color(0.6f, 0.45f, 0.35f), 0.3f), false);
                var trigObj = new GameObject("PlateTrigger_" + i);
                trigObj.transform.SetParent(r, false);
                trigObj.transform.localPosition = platePositions[i] + Vector3.up * 0.3f;
                var bc = trigObj.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(1.6f, 0.6f, 1.6f);
                var plate = trigObj.AddComponent<PressurePlate>();
                plate.elementId = "nz3.plate." + i;
                plate.plate = pObj.transform;
                Plates[i] = plate;
            }

            // Pushable blocks
            BlockA = MakeBlock(r, "BlockA", vaultOrigin + new Vector3(-6f, 0.75f, -16f));
            BlockB = MakeBlock(r, "BlockB", vaultOrigin + new Vector3(10f, 0.75f, -14f));

            // Reset lever for blocks
            var leverObj = MarketArt.Box(r, "Reset Lever Base", vaultOrigin + new Vector3(-14f, 0.6f, -12f), new Vector3(0.6f, 1.2f, 0.6f), "sandstone_blocks_08");
            ZoneKit.Reach(leverObj, 0.8f);
            ResetLever = leverObj.AddComponent<PuzzleLever>();
            ResetLever.elementId = "nz3.lever_reset";
            ResetLever.handle = leverObj.transform;
            ResetLever.verb = "Reset blocks";
            ResetLever.radius = 2.2f;

            // Vault iron dividing gate
            vaultGate = MarketArt.Box(r, "Iron Vault Bar Gate", vaultOrigin + new Vector3(0f, 2.5f, 0f), new Vector3(16f, 5f, 0.3f), "medieval_wood");

            // Section 2: Gear Train Puzzle
            var gearPanel = MarketArt.Box(r, "Gear Mechanism Bench", vaultOrigin + new Vector3(-10f, 0.8f, 8f), new Vector3(5f, 1.6f, 1.4f), "medieval_wood");
            GearPegs = new PlacementSocket[2];
            LooseGears = new Carryable[2];

            for (int i = 0; i < 2; i++)
            {
                var pegObj = new GameObject("GearPeg_" + i);
                pegObj.transform.SetParent(gearPanel.transform, false);
                pegObj.transform.localPosition = new Vector3(-1.2f + i * 2.4f, 0.5f, 0f);
                ZoneKit.Reach(pegObj, 0.6f);
                var peg = pegObj.AddComponent<PlacementSocket>();
                peg.elementId = "nz3.peg." + i;
                peg.acceptsKey = "nz3.gear." + i;
                peg.radius = 2.2f;
                GearPegs[i] = peg;

                var gearItem = ZoneKit.Round(r, "LooseGear_" + i, vaultOrigin + new Vector3(-12f + i * 4f, 0.4f, 4f), new Vector3(0.8f, 0.15f, 0.8f), ZoneKit.Tinted("clay_plaster", new Color(0.7f, 0.55f, 0.25f), 0.6f), false);
                ZoneKit.Reach(gearItem, 0.7f);
                var c = gearItem.AddComponent<Carryable>();
                c.itemKey = "nz3.gear." + i;
                c.displayName = "Bronze Gear " + (i + 1);
                c.radius = 2.2f;
                LooseGears[i] = c;
            }

            var crankObj = ZoneKit.Round(r, "Gear Crank", vaultOrigin + new Vector3(-7f, 1.2f, 8f), new Vector3(0.4f, 0.2f, 0.4f), ZoneKit.Tinted("clay_plaster", new Color(0.5f, 0.4f, 0.2f), 0.4f), false);
            ZoneKit.Reach(crankObj, 0.6f);
            Crank = crankObj.AddComponent<RotatingDisc>();
            Crank.elementId = "nz3.crank";
            Crank.steps = 4;
            Crank.radius = 2.2f;

            Gears = gearPanel.AddComponent<GearTrain>();
            Gears.elementId = "nz3.geartrain";
            Gears.pegs = GearPegs;
            Gears.crank = Crank;
            Gears.wheels = new[] { crankObj.transform, gearPanel.transform };
            Gears.teeth = new[] { 12, 24 };

            // Section 3: Three Rotating Stone Discs (Answer = 2, 5, 3)
            Discs = new RotatingDisc[3];
            for (int i = 0; i < 3; i++)
            {
                var dObj = ZoneKit.Round(r, "VaultDisc_" + i, vaultOrigin + new Vector3(-4f + i * 4f, 0.22f, 16f), new Vector3(2.2f, 0.18f, 2.2f), ZoneKit.Tinted("sandstone_blocks_08", new Color(0.75f, 0.65f, 0.5f), 0.4f), true);
                ZoneKit.Reach(dObj, 1.2f);
                var disc = dObj.AddComponent<RotatingDisc>();
                disc.elementId = "nz3.disc." + i;
                disc.steps = 8;
                disc.radius = 2.4f;
                Discs[i] = disc;
                MarketArt.Sign(r, vaultOrigin + new Vector3(-4f + i * 4f, 0.8f, 17.5f), "DISC " + (i + 1), 1.5f);
            }

            // Treasure Chest in inner vault
            var chest = MarketArt.Box(r, "Vault Relic Chest", vaultOrigin + new Vector3(0f, 0.6f, 22f), new Vector3(2.4f, 1.2f, 1.6f), "sandstone_blocks_08");
            chestLid = MarketArt.Box(chest.transform, "Chest Lid", new Vector3(0f, 0.65f, 0f), new Vector3(2.5f, 0.25f, 1.7f), "medieval_wood");

            // Treasure: Carved Sandstone Rosette
            var rosObj = ArtUpgrade.Place(r, "ceramic_pot", vaultOrigin + new Vector3(0f, 0.8f, 22f), 1.3f, 0f, false);
            rosObj.name = "Treasure — Sun Rosette";
            ZoneKit.Reach(rosObj, 0.8f);
            Rosette = rosObj.AddComponent<TreasurePickup>();
            Rosette.treasureName = "Sun Rosette (vaults)";
            Rosette.zoneId = Zone;

            // Puzzle Group
            Group = new GameObject("Vaults puzzle group").AddComponent<PuzzleGroup>();
            Group.transform.SetParent(r, false);
            Group.transform.localPosition = vaultOrigin;
            Group.puzzleId = "nz3.vault_puzzles";
            Group.zoneId = Zone;
            Group.hintRadius = 150f;
            Group.conditions.Add(new PuzzleGroup.Condition { element = Plates[0], state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Plates[1], state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Gears, state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Discs[0], state = DiscAnswer[0] });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Discs[1], state = DiscAnswer[1] });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Discs[2], state = DiscAnswer[2] });
            Group.hints = new[] {
                "Push stone blocks onto both floor pressure plates to lift the iron bar gate.",
                "Place the two bronze gears onto the drive bench pegs and turn the crank.",
                "Look at the light arcs cast from the ceiling grate: align the three discs to 2, 5, and 3."
            };

            // Atmosphere & Audio
            var look = new GameObject("Vaults near dark volume").AddComponent<ZoneLook>();
            look.transform.SetParent(r, false);
            look.transform.localPosition = vaultOrigin + Vector3.up * 5f;
            look.zone = ZoneLook.Zone.Vaults;
            look.size = new Vector3(80f, 30f, 80f);
            look.Build();
            ZoneAtmosphere.Register(look, Quaternion.Euler(90f, 0f, 0f));

            Built = true;
            Debug.Log("[NewVaults] built: underground hall, pressure plates, gear train, light-arc discs & heavy guard fight");
        }

        PushableBlock MakeBlock(Transform root, string name, Vector3 pos)
        {
            var g = MarketArt.Box(root, name, pos, new Vector3(1.3f, 1.3f, 1.3f), "sandstone_blocks_08", true);
            var rb = g.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var bl = g.AddComponent<PushableBlock>();
            bl.elementId = "nz3." + name;
            bl.step = 1.2f;
            bl.radius = 2.2f;
            return bl;
        }

        IEnumerator Start()
        {
            yield return null;
            if (Brawl.WonBefore(Zone)) { Now = Stage.Recover; ShowRosette(true); }
            if (PuzzleSave.Get(Rosette.Id) == 1) { Now = Stage.Complete; ShowRosette(false); }
            if (Now != Stage.Recover && Now != Stage.Complete) ShowRosette(false);
        }

        void ShowRosette(bool show)
        {
            if (Rosette == null) return;
            Rosette.enabled = show;
            foreach (var rr in Rosette.GetComponentsInChildren<Renderer>()) rr.enabled = show;
        }

        void Update()
        {
            if (!Built) return;

            bool platesSolved = Plates[0].State == 1 && Plates[1].State == 1;
            if (vaultGate != null) vaultGate.SetActive(!platesSolved);

            if (Now == Stage.Plates && platesSolved) Now = Stage.Gears;
            if (Now == Stage.Gears && Gears.State == 1) Now = Stage.Discs;

            bool discsSolved = Discs[0].State == DiscAnswer[0] && Discs[1].State == DiscAnswer[1] && Discs[2].State == DiscAnswer[2];
            if (Now == Stage.Discs && discsSolved)
            {
                chestLid.SetActive(false);
                GameSfx.At("sfx_wall_open", chestLid.transform.position, 0.8f);
                Now = Stage.Fight;
                StoryDialogueSystem.Instance?.Say("v3:vault_guards_alert");
                if (Brawl.WonBefore(Zone)) Won();
                else Brawl.Begin(Zone, ArenaCenter, 8f, new[] {
                    Brawl.Robber("Heavy Vault Sentinel", 1, BrawlType.Heavy, 1.1f, new Color(0.4f, 0.45f, 0.5f)),
                    Brawl.Robber("Corridor Enforcer", 2, BrawlType.Brawler, 1.0f, new Color(0.6f, 0.55f, 0.5f))
                }, _ => Won());
            }

            if (Now == Stage.Recover && Rosette.Taken)
            {
                Now = Stage.Complete;
                PuzzleSave.Set("evidence.ledger", 1);
                PuzzleSave.Set("nz3.complete", 1);
                ScoreSystem.FinishZone(Zone);
                Debug.Log("[NewVaults] COMPLETE: Sun Rosette + Ledger recovered! Score=" + ScoreSystem.Points(ScoreSystem.Zone(Zone)));
            }
        }

        void Won()
        {
            Now = Stage.Recover;
            ShowRosette(true);
            GameSfx.At("sfx_item_recovered", Rosette.transform.position, 1f);
        }
    }
}
