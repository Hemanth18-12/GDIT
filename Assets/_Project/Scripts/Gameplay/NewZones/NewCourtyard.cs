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
    /// Replacement Zone 2: Temple Gate & Courtyard (dawn gold). Reconstruct the break-in with physical evidence.
    /// 1. Three shards of the broken ceremonial oil pot lie scattered. Fitted back on the plinth, the pot is whole and lights.
    /// 2. Carry the lit lantern onto the oil stain; its shadow casts the grille outline matching scratch marks and points to a loose flagstone.
    /// 3. Inspect the lock plate: rotating it reveals scratches are on the inside (evidence of inside man).
    /// 4. Lift the loose flagstone: reveals the lookout's escape rope. The lookout drops down for a fight.
    /// 5. On defeat, recovers Treasure 2: Palm-Leaf Chronicle, and opens access to Underground Vaults (Zone 3).
    /// </summary>
    public sealed class NewCourtyard : MonoBehaviour
    {
        public const string Zone = "new_courtyard";
        public static NewCourtyard Instance { get; private set; }
        public enum Stage { Reconstruct, ShadowMatch, InspectLock, LiftFlagstone, Fight, Recover, Complete }
        public Stage Now { get; private set; }
        public bool Built { get; private set; }

        public Carryable[] Shards { get; private set; }
        public PlacementSocket[] Sockets { get; private set; }
        public Carryable Lantern { get; private set; }
        public ProximityMatch LampSpot { get; private set; }
        public RotatingDisc LockPlate { get; private set; }
        public PuzzleLever Flagstone { get; private set; }
        public TreasurePickup Chronicle { get; private set; }
        public PuzzleGroup Group { get; private set; }

        public Transform Plinth { get; private set; }
        public Transform Stain { get; private set; }
        public Transform Gate { get; private set; }
        public Vector3 ArenaCenter => new Vector3(15f, 0f, 100f);

        GameObject wholePot;
        Light lanternLight;
        GameObject flagstoneObj;
        GameObject vaultStairs;
        bool checkpointed;

        void Awake() { Instance = this; Build(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Build()
        {
            var r = transform;

            // Paved temple courtyard ground
            MarketArt.Box(r, "Courtyard flagstone pavement", new Vector3(0f, -0.05f, 115f), new Vector3(90f, 0.3f, 120f), "red_sandstone_pavement");
            MarketArt.Box(r, "Inner sanctum raised platform", new Vector3(0f, 0.4f, 155f), new Vector3(70f, 0.6f, 35f), "mossy_sandstone");

            // Perimeter stone boundary walls
            MarketArt.Box(r, "Courtyard West Wall", new Vector3(-45f, 2.5f, 115f), new Vector3(1.2f, 5f, 120f), "sandstone_blocks_08");
            MarketArt.Box(r, "Courtyard East Wall", new Vector3(45f, 2.5f, 115f), new Vector3(1.2f, 5f, 120f), "sandstone_blocks_08");
            MarketArt.Box(r, "Courtyard North Wall", new Vector3(0f, 3f, 175f), new Vector3(90f, 6f, 1.5f), "sandstone_blocks_08");

            // Simhadwara (Lion Gate) at South entrance (z = 60-65)
            Gate = new GameObject("Simhadwara Gate").transform;
            Gate.SetParent(r, false);
            Gate.localPosition = new Vector3(0f, 0f, 62f);

            MarketArt.Box(Gate, "Pillar West", new Vector3(-5f, 3.5f, 0f), new Vector3(1.8f, 7f, 1.8f), "sandstone_blocks_08");
            MarketArt.Box(Gate, "Pillar East", new Vector3(5f, 3.5f, 0f), new Vector3(1.8f, 7f, 1.8f), "sandstone_blocks_08");
            MarketArt.Box(Gate, "Gate Lintel Arch", new Vector3(0f, 7.2f, 0f), new Vector3(12.5f, 1.2f, 2f), "sandstone_blocks_08");
            MarketArt.Sign(Gate, new Vector3(0f, 6.2f, -1.1f), "SRI MANDIRA COURTYARD", 5.5f);

            ArtUpgrade.Place(Gate, "lion_head", Gate.TransformPoint(new Vector3(-5f, 5.2f, -1f)), 1.4f, 0f, false);
            ArtUpgrade.Place(Gate, "lion_head", Gate.TransformPoint(new Vector3(5f, 5.2f, -1f)), 1.4f, 0f, false);

            // Courtyard colonnade along west side with grounded pedestals
            for (int i = 0; i < 6; i++)
            {
                float z = 75f + i * 14f;
                MarketArt.Box(r, "Colonnade pillar", new Vector3(-35f, 3f, z), new Vector3(1.2f, 6f, 1.2f), "sandstone_blocks_08");
                MarketArt.Box(r, "Colonnade lamp pedestal", new Vector3(-35f, 0.6f, z + 1.2f), new Vector3(0.6f, 1.2f, 0.6f), "sandstone_blocks_08");
                ArtUpgrade.Place(r, "brass_diya_lantern", new Vector3(-35f, 1.2f, z + 1.2f), 1.1f, 0f, false);
            }

            // East Storage Hall (Crime Scene) at x = 18 to 36, z = 90 to 120
            var hall = new GameObject("Storage Hall").transform;
            hall.SetParent(r, false);
            hall.localPosition = new Vector3(25f, 0f, 105f);

            MarketArt.Box(hall, "Hall Floor", new Vector3(0f, 0.15f, 0f), new Vector3(18f, 0.3f, 24f), "sandstone_blocks_08");
            MarketArt.Box(hall, "Hall Ceiling", new Vector3(0f, 5.15f, 0f), new Vector3(18.6f, 0.3f, 24.6f), "red_sandstone_wall");
            var hallLight = new GameObject("StorageHallWarmLight").AddComponent<Light>();
            hallLight.transform.SetParent(hall, false); hallLight.transform.localPosition = new Vector3(-2f, 4.2f, 2f);
            hallLight.type = LightType.Point; hallLight.range = 24f; hallLight.intensity = 2.6f; hallLight.color = new Color(1f, 0.78f, 0.48f); hallLight.shadows = LightShadows.None;
            MarketArt.Box(hall, "Hall Wall North", new Vector3(0f, 2.5f, 12f), new Vector3(18f, 5f, 0.8f), "yellow_plaster");
            MarketArt.Box(hall, "Hall Wall East", new Vector3(9f, 2.5f, 0f), new Vector3(0.8f, 5f, 24f), "yellow_plaster");
            MarketArt.Box(hall, "Hall Wall South", new Vector3(0f, 2.5f, -12f), new Vector3(18f, 5f, 0.8f), "yellow_plaster");
            MarketArt.Box(hall, "Hall Wall West (Front)", new Vector3(-9f, 2.5f, -6f), new Vector3(0.8f, 5f, 12f), "yellow_plaster");
            MarketArt.Box(hall, "Hall Wall West High", new Vector3(-9f, 4.2f, 6f), new Vector3(0.8f, 1.6f, 12f), "yellow_plaster");

            // Carved grille window
            var grille = ZoneKit.Part(hall, "Timber Grille Window", new Vector3(-9f, 2f, 5f), new Vector3(0.15f, 2.2f, 3.2f), ZoneKit.Tinted("medieval_wood", new Color(0.4f, 0.28f, 0.18f), 0.3f), false);
            grille.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Broken storage door frame
            MarketArt.Box(hall, "Door Jamb Left", new Vector3(-9f, 1.6f, 0f), new Vector3(0.6f, 3.2f, 0.3f), "medieval_wood");
            MarketArt.Box(hall, "Door Jamb Right", new Vector3(-9f, 1.6f, 2.4f), new Vector3(0.6f, 3.2f, 0.3f), "medieval_wood");
            MarketArt.Box(hall, "Door Lintel", new Vector3(-9f, 3.3f, 1.2f), new Vector3(0.6f, 0.4f, 2.8f), "medieval_wood");

            // Wall scratch marks matching grille shadow
            MarketArt.Box(hall, "Scratch marks on east wall", new Vector3(8.55f, 2.2f, 5f), new Vector3(0.04f, 1.8f, 2.8f), "weathered_planks", false);

            // Plinth for the broken ceremonial pot
            Plinth = MarketArt.Box(hall, "Pot plinth", new Vector3(-2f, 0.5f, 4f), new Vector3(1.4f, 0.8f, 1.4f), "sandstone_blocks_08").transform;
            MarketArt.Sign(hall, new Vector3(-2f, 1.15f, 3.25f), "RESTORE CEREMONIAL POT", 1.8f);

            // Shards scattered in courtyard
            Shards = new Carryable[3];
            Sockets = new PlacementSocket[3];
            string[] shardNames = { "Base shard", "Handle shard", "Spout shard" };
            Vector3[] shardSpawns = {
                new Vector3(10f, 0.1f, 82f),
                new Vector3(-12f, 0.1f, 98f),
                new Vector3(28f, 0.1f, 76f)
            };

            for (int i = 0; i < 3; i++)
            {
                var sObj = ArtUpgrade.Place(r, "ceramic_vase_01", shardSpawns[i], 0.7f, i * 65f, false);
                sObj.name = shardNames[i];
                ZoneKit.Reach(sObj, 0.6f);
                var c = sObj.AddComponent<Carryable>();
                c.itemKey = "nz2.shard." + i;
                c.displayName = shardNames[i];
                c.radius = 2.2f;
                Shards[i] = c;

                var sockObj = new GameObject("Socket " + i);
                sockObj.transform.SetParent(Plinth, false);
                sockObj.transform.localPosition = new Vector3(-0.35f + i * 0.35f, 0.5f, 0f);
                ZoneKit.Reach(sockObj, 0.5f);
                var sock = sockObj.AddComponent<PlacementSocket>();
                sock.elementId = "nz2.socket." + i;
                sock.acceptsKey = "nz2.shard." + i;
                sock.radius = 2.2f;
                Sockets[i] = sock;
            }

            // Whole pot (appears when 3 shards placed)
            wholePot = ArtUpgrade.Place(Plinth, "brass_pot_01", Plinth.TransformPoint(new Vector3(0f, 0.6f, 0f)), 1.2f, 0f, false);
            wholePot.name = "Restored Pot";
            wholePot.SetActive(false);

            // Standing lantern
            var lanternObj = ArtUpgrade.Place(hall, "brass_diya_lantern", hall.TransformPoint(new Vector3(-4f, 0.1f, -4f)), 1.3f, 0f, false);
            lanternObj.name = "Standing Brass Lantern";
            ZoneKit.Reach(lanternObj, 0.7f);
            Lantern = lanternObj.AddComponent<Carryable>();
            Lantern.itemKey = "nz2.lantern";
            Lantern.displayName = "Lit Brass Lantern";
            Lantern.radius = 2.4f;

            lanternLight = lanternObj.GetComponentInChildren<Light>();
            if (lanternLight == null) lanternLight = lanternObj.AddComponent<Light>();
            lanternLight.type = LightType.Point;
            lanternLight.range = 10f;
            lanternLight.intensity = 1.6f;
            lanternLight.color = new Color(1f, 0.72f, 0.35f);
            lanternLight.enabled = false;

            // Oil stain on floor (where the lantern must be set)
            Stain = ZoneKit.Round(hall, "Oil stain marker", new Vector3(1f, 0.16f, 5f), new Vector3(1.8f, 0.02f, 1.8f), ZoneKit.Tinted("brown_mud_leaves_01", new Color(0.12f, 0.09f, 0.06f), 0.8f)).transform;
            var spotObj = new GameObject("Lamp spot match");
            spotObj.transform.SetParent(hall, false);
            spotObj.transform.localPosition = new Vector3(1f, 0.16f, 5f);
            ZoneKit.Reach(spotObj, 1.2f);
            LampSpot = spotObj.AddComponent<ProximityMatch>();
            LampSpot.elementId = "nz2.lamp_spot";
            LampSpot.subject = Lantern.transform;
            LampSpot.target = Stain;
            LampSpot.tolerance = 1.4f;

            // Rotating lock plate with inside scratches
            var lockObj = ZoneKit.Round(hall, "Broken Lock Plate", new Vector3(-8.9f, 1.6f, 1.2f), new Vector3(0.4f, 0.08f, 0.4f), ZoneKit.Tinted("clay_plaster", new Color(0.7f, 0.6f, 0.3f), 0.5f), true);
            lockObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ZoneKit.Reach(lockObj, 0.6f);
            LockPlate = lockObj.AddComponent<RotatingDisc>();
            LockPlate.elementId = "nz2.lock_plate";
            LockPlate.steps = 2;
            LockPlate.radius = 2.2f;
            MarketArt.Sign(hall, new Vector3(-8.9f, 2.1f, 1.2f), "INSPECT LOCK", 1.2f, 90f);

            // Loose flagstone that lifts to reveal the rope
            flagstoneObj = MarketArt.Box(hall, "Loose flagstone", new Vector3(5f, 0.16f, 5f), new Vector3(1.6f, 0.12f, 1.6f), "sandstone_blocks_08");
            ZoneKit.Reach(flagstoneObj, 0.8f);
            Flagstone = flagstoneObj.AddComponent<PuzzleLever>();
            Flagstone.elementId = "nz2.flagstone";
            Flagstone.handle = flagstoneObj.transform;
            Flagstone.pulledEuler = new Vector3(0f, 0f, 45f);
            Flagstone.verb = "Lift flagstone";
            Flagstone.radius = 2.4f;

            // Vault trapdoor stairs down to Zone 3
            vaultStairs = MarketArt.Box(hall, "Trapdoor to Vaults", new Vector3(5f, -0.4f, 5f), new Vector3(1.4f, 0.2f, 1.4f), "weathered_planks");
            vaultStairs.SetActive(false);

            // Treasure: Palm-Leaf Chronicle
            var chronObj = ArtUpgrade.Place(r, "wooden_bowl_02", new Vector3(15f, 0.1f, 100f), 1.3f, 0f, false);
            chronObj.name = "Treasure — Palm-Leaf Chronicle";
            ZoneKit.Reach(chronObj, 0.7f);
            Chronicle = chronObj.AddComponent<TreasurePickup>();
            Chronicle.saveId = "treasure.Palm-Leaf Chronicle (courtyard)"; Chronicle.treasureName = "Tala-Patra Gita Govinda Palm-Leaf Manuscript (teaching replica)";
            Chronicle.zoneId = Zone;

            // Puzzle group & hint tiers
            Group = new GameObject("Courtyard crime scene puzzle").AddComponent<PuzzleGroup>();
            Group.transform.SetParent(r, false);
            Group.transform.localPosition = new Vector3(25f, 0f, 105f);
            Group.puzzleId = "nz2.reconstruct";
            Group.zoneId = Zone;
            Group.hintRadius = 140f;
            Group.conditions.Add(new PuzzleGroup.Condition { element = Sockets[0], state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Sockets[1], state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Sockets[2], state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = LampSpot, state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = LockPlate, state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = Flagstone, state = 1 });
            Group.hints = new[] {
                "The ceremonial oil lamp broke during the robbery. Fit its pieces back on the storage hall plinth.",
                "Light throws shadows that reveal hidden marks. Set the lit lamp on the dark oil stain by the grille.",
                "Inspect the door lock; turn the plate to see inside scratches. Then lift the flagstone illuminated by the shadow."
            };

            // Zone Atmosphere & Audio
            var look = new GameObject("Courtyard dawn volume").AddComponent<ZoneLook>();
            look.transform.SetParent(r, false);
            look.transform.localPosition = new Vector3(0f, 10f, 115f);
            look.zone = ZoneLook.Zone.Courtyard;
            look.size = new Vector3(140f, 50f, 160f);
            look.Build();
            ZoneAtmosphere.Register(look, Quaternion.Euler(18f, -45f, 0f));

            var src = r.gameObject.AddComponent<AudioSource>();
            src.clip = Resources.Load<AudioClip>("Audio/amb_birds");
            src.volume = 0.25f;
            src.loop = true;
            src.spatialBlend = 0f;
            if (src.clip != null) src.Play();

            Built = true;
            Debug.Log("[NewCourtyard] built: Lion Gate, flagstone courtyard, storage hall crime scene, shadow-matching & lookout fight");
        }

        IEnumerator Start()
        {
            yield return null;
            if (Brawl.WonBefore(Zone)) { Now = Stage.Recover; ShowChronicle(true); }
            if (PuzzleSave.Get(Chronicle.Id) == 1) { Now = Stage.Complete; ShowChronicle(false); }
            if (Now != Stage.Recover && Now != Stage.Complete) ShowChronicle(false);
        }

        void ShowChronicle(bool show)
        {
            if (Chronicle == null) return;
            Chronicle.enabled = show;
            foreach (var rr in Chronicle.GetComponentsInChildren<Renderer>()) rr.enabled = show;
        }

        void Update()
        {
            if (!Built) return;

            bool shardsAllPlaced = Sockets[0].State == 1 && Sockets[1].State == 1 && Sockets[2].State == 1;
            if (shardsAllPlaced && !wholePot.activeSelf)
            {
                wholePot.SetActive(true);
                lanternLight.enabled = true;
                GameSfx.At("sfx_bell_temple", Plinth.position, 0.7f);
                StoryDialogueSystem.Instance?.Say("v3:courtyard_lamp_lit");
            }

            if (Now == Stage.Reconstruct && shardsAllPlaced) Now = Stage.ShadowMatch;
            if (Now == Stage.ShadowMatch && LampSpot.State == 1) Now = Stage.InspectLock;
            if (Now == Stage.InspectLock && LockPlate.State == 1)
            {
                PuzzleSave.Set("evidence.inside_lock", 1);
                StoryDialogueSystem.Instance?.Say("v3:lock_inside_scratches");
                Now = Stage.LiftFlagstone;
            }
            if (Now == Stage.LiftFlagstone && Flagstone.State == 1)
            {
                vaultStairs.SetActive(true);
                GameSfx.At("sfx_wall_open", Flagstone.transform.position, 0.8f);
                Now = Stage.Fight;
                StoryDialogueSystem.Instance?.Say("v3:lookout_ambush");
                if (Brawl.WonBefore(Zone)) Won();
                else Brawl.Begin(Zone, ArenaCenter, 9f, new[] { Brawl.Robber("Lookout — temple roof", 1, BrawlType.Brawler, 1.05f, new Color(0.75f, 0.55f, 0.45f)) }, _ => Won());
            }

            if (Now == Stage.Recover && Chronicle.Taken)
            {
                Now = Stage.Complete;
                PuzzleSave.Set("evidence.chronicle", 1);
                PuzzleSave.Set("nz2.complete", 1);
                ScoreSystem.FinishZone(Zone);
                Debug.Log("[NewCourtyard] COMPLETE: Palm-Leaf Chronicle recovered! Score=" + ScoreSystem.Points(ScoreSystem.Zone(Zone)));
            }
        }

        public void SkipTask()
        {
            if (Chronicle.Taken) return;
            if (Brawl.Active != null && Brawl.Active.zoneId == Zone) Brawl.Active.ResolveSkipped();
            if (Group != null) foreach (var condition in Group.conditions)
                if (condition.element != null) condition.element.SetState(condition.state, true, true);

            Won();
            Chronicle.Interact();
        }

        void Won()
        {
            Now = Stage.Recover;
            ShowChronicle(true);
            GameSfx.At("sfx_clue_found", Chronicle.transform.position, 1f);
        }
    }
}
