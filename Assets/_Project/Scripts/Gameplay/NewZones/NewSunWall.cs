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
    /// Replacement Zone 5: Sun Wall and Chariot-Wheel Plaza (harsh midday).
    /// 1. Rotate two stone pedestal mirrors to reflect the sunlight beam from the sun altar to the chariot wheel socket.
    /// 2. Turn the monumental Konark chariot wheel to grind open the hidden cliffside sanctuary door.
    /// 3. Mini-boss fight against Kishore (two-phase combat).
    /// 4. On defeat, recovers the Smuggler Map & Empty 5th Case, unlocking Zone 6 (Jungle Road & Ruins).
    /// </summary>
    public sealed class NewSunWall : MonoBehaviour
    {
        public const string Zone = "new_sunwall";
        public static NewSunWall Instance { get; private set; }

        public enum Stage { LightPuzzle, TurnWheel, Fight, Recover, Complete }
        public Stage Now { get; private set; }
        public bool Built { get; private set; }

        public LightBeamEmitter Emitter { get; private set; }
        public LightMirror MirrorA { get; private set; }
        public LightMirror MirrorB { get; private set; }
        public LightSocket WheelSocket { get; private set; }
        public RotatingDisc ChariotWheel { get; private set; }
        public TreasurePickup SmugglerMap { get; private set; }
        public PuzzleGroup Group { get; private set; }

        public Vector3 ArenaCenter => new Vector3(65f, 0f, 135f);
        GameObject sanctuaryDoor;

        void Awake() { Instance = this; Build(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Build()
        {
            var r = transform;
            Vector3 plazaOrigin = new Vector3(65f, 0f, 125f);

            // Sun Plaza Pavement
            MarketArt.Box(r, "Sun Plaza Sandstone Floor", plazaOrigin + new Vector3(0f, -0.05f, 0f), new Vector3(45f, 0.3f, 45f), "sandstone_blocks_08");

            // Massive Monumental Sun Wall on the East (z = 100-150, x = 86)
            MarketArt.Box(r, "Sun Wall North", plazaOrigin + new Vector3(21f, 6f, 12f), new Vector3(2.5f, 12f, 20f), "sandstone_blocks_08");
            MarketArt.Box(r, "Sun Wall South", plazaOrigin + new Vector3(21f, 6f, -12f), new Vector3(2.5f, 12f, 20f), "sandstone_blocks_08");
            MarketArt.Box(r, "Sun Wall Lintel", plazaOrigin + new Vector3(21f, 9f, 0f), new Vector3(2.5f, 6f, 4f), "sandstone_blocks_08");

            // Four carved relief panels along the wall (Lotus, Conch, Wheel, Kalash)
            string[] symbols = { "LOTUS", "CONCH", "WHEEL", "KALASH" };
            for (int i = 0; i < 4; i++)
            {
                float z = -12f + i * 8f;
                MarketArt.Box(r, "Symbol Panel " + symbols[i], plazaOrigin + new Vector3(19.6f, 4f, z), new Vector3(0.3f, 3.5f, 3.5f), "sandstone_blocks_08", false);
                MarketArt.Sign(r, plazaOrigin + new Vector3(19.3f, 6.2f, z), symbols[i], 2.2f, -90f);
            }

            // High Sun Altar with Light Beam Emitter
            MarketArt.Box(r, "Sun Altar", plazaOrigin + new Vector3(-16f, 1.3f, -16f), new Vector3(3.2f, 2.4f, 3.2f), "sandstone_blocks_08");
            var emitterObj = new GameObject("Sunlight aperture");
            emitterObj.transform.SetParent(r, false);
            emitterObj.transform.localPosition = plazaOrigin + new Vector3(-16f, 2.8f, -14.2f);
            var beam = emitterObj.AddComponent<LineRenderer>();
            beam.useWorldSpace = true; beam.widthMultiplier = 0.12f; beam.numCapVertices = 4;
            beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beam.sharedMaterial = Fx.Res("Glow") ?? ZoneKit.Tinted("clay_plaster", Color.white, 0f, new Color(3f, 2.4f, 1.2f));
            beam.startColor = beam.endColor = new Color(1f, 0.88f, 0.4f);
            Emitter = emitterObj.AddComponent<LightBeamEmitter>(); Emitter.line = beam;
            Emitter.maxDistance = 50f;

            // Mirror A on pedestal at (plazaOrigin + (-16, 1.2, 12))
            Vector3 mirrorA = plazaOrigin + new Vector3(-16f, 2.8f, 12f);
            Vector3 mirrorB = plazaOrigin + new Vector3(12f, 2.8f, 12f);
            Vector3 socketPosition = plazaOrigin + new Vector3(18.9f, 2.8f, 0f);
            MirrorA = BuildMirror(r, "Mirror A", mirrorA, 90f, "nz5.mirror_a");

            // Mirror B on pedestal at (plazaOrigin + (12, 1.2, 12))
            Vector3 incoming = (mirrorB - mirrorA).normalized;
            Vector3 outgoing = (socketPosition - mirrorB).normalized;
            float solvedYaw = Quaternion.LookRotation((outgoing - incoming).normalized).eulerAngles.y;
            MirrorB = BuildMirror(r, "Mirror B", mirrorB, solvedYaw - 90f, "nz5.mirror_b");

            // Light Socket on the Chariot Wheel Hub at (plazaOrigin + (19.4, 3.5, 0))
            var sockObj = new GameObject("Chariot Wheel Light Socket");
            sockObj.transform.SetParent(r, false);
            sockObj.transform.localPosition = socketPosition;
            ZoneKit.Reach(sockObj, 1.2f);
            WheelSocket = sockObj.AddComponent<LightSocket>();
            WheelSocket.elementId = "nz5.wheel_socket";
            WheelSocket.holdSeconds = 0.35f;
            var receiver = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            receiver.name = "Light receiver"; receiver.transform.SetParent(sockObj.transform, false);
            receiver.transform.localScale = Vector3.one * 1.1f;
            WheelSocket.glow = receiver.GetComponent<Renderer>();
            WheelSocket.glow.sharedMaterial = ZoneKit.Tinted("clay_plaster", new Color(0.65f, 0.45f, 0.12f), 0.8f, new Color(1f, 0.6f, 0.1f));

            // Monumental Konark Chariot Wheel (4.5m diameter rotating disc)
            var wheelObj = ZoneKit.Round(r, "Grand Chariot Wheel", plazaOrigin + new Vector3(19.6f, 2.8f, 0f), new Vector3(5.2f, 0.4f, 5.2f), ZoneKit.Tinted("sandstone_blocks_08", new Color(0.82f, 0.75f, 0.6f), 0.35f), false);
            wheelObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            ZoneKit.Reach(wheelObj, 2.2f);
            ChariotWheel = wheelObj.AddComponent<RotatingDisc>();
            ChariotWheel.elementId = "nz5.chariot_wheel";
            ChariotWheel.steps = 4;
            ChariotWheel.radius = 3.0f;

            // Sanctuary Door behind the wall
            sanctuaryDoor = MarketArt.Box(r, "Hidden Sanctuary Door", plazaOrigin + new Vector3(21f, 3f, 0f), new Vector3(2.6f, 6f, 4f), "sandstone_blocks_08");

            // Treasure: Smuggler's River Map & 5th Case
            var mapObj = ArtUpgrade.Place(r, "WoodenTable_01", plazaOrigin + new Vector3(12f, 0f, -4f), 1.2f, 0f, false);
            mapObj.name = "Treasure — Smuggler Map";
            ZoneKit.Reach(mapObj, 0.8f);
            SmugglerMap = mapObj.AddComponent<TreasurePickup>();
            SmugglerMap.treasureName = "Smuggler Map (sun wall)";
            SmugglerMap.zoneId = Zone;

            // Puzzle Group
            Group = new GameObject("Sun Wall puzzle group").AddComponent<PuzzleGroup>();
            Group.transform.SetParent(r, false);
            Group.transform.localPosition = plazaOrigin;
            Group.puzzleId = "nz5.sunwall_puzzle";
            Group.zoneId = Zone;
            Group.hintRadius = 150f;
            Group.conditions.Add(new PuzzleGroup.Condition { element = WheelSocket, state = 1 });
            Group.conditions.Add(new PuzzleGroup.Condition { element = ChariotWheel, state = 1 });
            Group.hints = new[] {
                "The Sun Altar projects a focused beam of sunlight across the plaza.",
                "Turn Mirror A and Mirror B to reflect the light beam into the Chariot Wheel's central hub socket.",
                "With the socket charged, rotate the giant Chariot Wheel to grind open the hidden cliff sanctuary."
            };

            // Zone Atmosphere
            var look = new GameObject("Sun wall midday volume").AddComponent<ZoneLook>();
            look.transform.SetParent(r, false);
            look.transform.localPosition = plazaOrigin + Vector3.up * 8f;
            look.zone = ZoneLook.Zone.SunWall;
            look.size = new Vector3(100f, 40f, 100f);
            look.Build();
            ZoneAtmosphere.Register(look, Quaternion.Euler(65f, 25f, 0f));

            Built = true;
            Debug.Log("[NewSunWall] built: Sun Wall monument, rotatable mirrors, Konark Chariot Wheel & Kishore fight");
        }

        static LightMirror BuildMirror(Transform parent, string name, Vector3 position, float restYaw, string id)
        {
            ZoneKit.Round(parent, name + " pedestal", new Vector3(position.x, 0.8f, position.z), new Vector3(1.2f, 0.7f, 1.2f), ZoneKit.Tinted("sandstone_blocks_08", Color.white, 0.2f), true);
            MarketArt.Box(parent, name + " support", new Vector3(position.x, 1.9f, position.z), new Vector3(0.18f, 1f, 0.18f), "sandstone_blocks_08", false);
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false); pivot.transform.localPosition = position;
            pivot.transform.localRotation = Quaternion.Euler(0f, restYaw, 0f);
            var plate = ZoneKit.Part(pivot.transform, "Bronze mirror face", Vector3.zero, new Vector3(1.8f, 1.8f, 0.12f), ZoneKit.Tinted("clay_plaster", new Color(0.8f, 0.64f, 0.3f), 0.95f), true);
            plate.GetComponent<Renderer>().sharedMaterial.SetFloat("_Metallic", 0.9f);
            ZoneKit.Reach(pivot, 1.4f);
            var mirror = pivot.AddComponent<LightMirror>();
            mirror.elementId = id; mirror.steps = 8; mirror.radius = 3.6f; mirror.verb = "Turn " + name;
            return mirror;
        }

        IEnumerator Start()
        {
            yield return null;
            if (Brawl.WonBefore(Zone)) { Now = Stage.Recover; ShowMap(true); }
            if (PuzzleSave.Get(SmugglerMap.Id) == 1) { Now = Stage.Complete; ShowMap(false); }
            if (Now != Stage.Recover && Now != Stage.Complete) ShowMap(false);
        }

        void ShowMap(bool show)
        {
            if (SmugglerMap == null) return;
            SmugglerMap.enabled = show;
            foreach (var rr in SmugglerMap.GetComponentsInChildren<Renderer>()) rr.enabled = show;
        }

        void Update()
        {
            if (!Built) return;

            // Check if socket received beam
            if (WheelSocket.State == 1 && Now == Stage.LightPuzzle)
            {
                Now = Stage.TurnWheel;
                StoryDialogueSystem.Instance?.Say("v3:sunwall_charged");
                GameSfx.At("sfx_gear_click", ChariotWheel.transform.position, 0.8f);
            }

            if (Now == Stage.TurnWheel && ChariotWheel.State >= 1)
            {
                if (sanctuaryDoor != null) sanctuaryDoor.SetActive(false);
                GameSfx.At("sfx_wall_open", ChariotWheel.transform.position, 0.9f);
                Now = Stage.Fight;
                StoryDialogueSystem.Instance?.Say("v3:kishore_encounter");
                if (Brawl.WonBefore(Zone)) Won();
                else Brawl.Begin(Zone, ArenaCenter, 5f, new[] {
                    Brawl.Robber("Kishore — Winged Smuggler", 2, BrawlType.Boss, 1.25f, new Color(0.2f, 0.2f, 0.25f))
                }, _ => Won());
            }

            if (Now == Stage.Recover && SmugglerMap.Taken)
            {
                Now = Stage.Complete;
                PuzzleSave.Set("evidence.smugglermap", 1);
                PuzzleSave.Set("nz5.complete", 1);
                ScoreSystem.FinishZone(Zone);
                Debug.Log("[NewSunWall] COMPLETE: Smuggler Map recovered! Score=" + ScoreSystem.Points(ScoreSystem.Zone(Zone)));
            }
        }

        void Won()
        {
            Now = Stage.Recover;
            ShowMap(true);
            GameSfx.At("sfx_item_recovered", SmugglerMap.transform.position, 1f);
        }
    }
}
