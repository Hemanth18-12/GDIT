using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// V3 zone 5 (Sun Wall and Chariot-Wheel Plaza): carry two sunbeams to two carvings on the wall with four bronze mirrors.
    /// One beam must pass between the spokes of the great stone wheel, so the wheel has to be pushed until a gap lines up.
    /// When both carvings are lit the hidden door in the wall slides down: the finial's empty case and a boat receipt.
    /// </summary>
    public sealed class SunWallPuzzle : MonoBehaviour
    {
        public const string ZoneId = "sunwall";
        public static SunWallPuzzle Instance { get; private set; }
        public bool Built { get; private set; }
        public PuzzleGroup Group { get; private set; }
        public LightMirror[] Mirrors { get; private set; }
        public RotatingDisc Wheel { get; private set; }
        public LightSocket[] Sockets { get; private set; }
        public LightBeamEmitter[] Beams { get; private set; }
        public TreasurePickup Case { get; private set; }
        Transform door; float doorOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded += (s, m) => { if (s.name == Constants.Courtyard) Spawn(); }; if (SceneManager.GetActiveScene().name == Constants.Courtyard) Spawn(); }
        static void Spawn() { if (Instance == null) new GameObject("V3_Zone5_SunWall").AddComponent<SunWallPuzzle>(); }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        IEnumerator Start()
        {
            float wait = 0f; while ((TempleSite.Instance == null || TempleSite.Footprint.size.sqrMagnitude < 1f) && wait < 20f) { wait += Time.deltaTime; yield return null; }
            var b = TempleSite.Footprint; if (b.size.sqrMagnitude < 1f) yield break;
            var cands = new System.Collections.Generic.List<(Vector3, Quaternion)>();
            foreach (float dz in new[] { 0f, 28f, -28f, 14f, -14f }) cands.Add((new Vector3(b.min.x - 14f, Village.Top, b.center.z + dz), Quaternion.identity));
            if (!ZoneKit.ClearGround(cands, new Vector3(12.6f, 3f, 17.5f), out var at, out var face)) Debug.LogWarning("[SunWall] no clear ground west of the temple; using the last candidate");
            Build(at, face);
        }

        LightMirror Mirror(Transform root, string name, Vector3 pos, float yaw, Material bronze)
        {
            Village.Box(root, name + "_Post", new Vector3(pos.x, 0.5f, pos.z), new Vector3(0.3f, 1.0f, 0.3f), "sandstone_blocks_08", 1f);
            var g = new GameObject(name); g.transform.SetParent(root, false); g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var box = g.AddComponent<BoxCollider>(); box.size = new Vector3(1.3f, 1.3f, 0.08f);
            var disc = ZoneKit.Round(g.transform, "Bronze", Vector3.zero, new Vector3(1.3f, 0.03f, 1.3f), bronze); disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var m = g.AddComponent<LightMirror>(); m.elementId = "z5." + name; m.steps = 16; m.verb = "Turn mirror"; m.radius = 2.4f; m.turnSeconds = 0.3f; return m;
        }
        LightBeamEmitter Beam(Transform root, string name, Vector3 pos)
        {
            Village.Box(root, name + "_GateL", new Vector3(pos.x - 0.55f, 1.6f, pos.z - 0.6f), new Vector3(0.7f, 3.2f, 0.7f), "sandstone_blocks_08", 1.4f);
            Village.Box(root, name + "_GateR", new Vector3(pos.x + 0.55f, 1.6f, pos.z - 0.6f), new Vector3(0.7f, 3.2f, 0.7f), "sandstone_blocks_08", 1.4f);
            Village.Box(root, name + "_GateTop", new Vector3(pos.x, 3.4f, pos.z - 0.6f), new Vector3(1.8f, 0.5f, 0.7f), "sandstone_blocks_08", 1.4f);
            var g = new GameObject(name); g.transform.SetParent(root, false); g.transform.localPosition = pos; g.transform.localRotation = Quaternion.identity;
            var lr = g.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.widthMultiplier = 0.2f; lr.numCapVertices = 4; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var gm = Fx.Res("Glow"); lr.sharedMaterial = gm != null ? gm : ZoneKit.Tinted("clay_plaster", Color.white, 0f, new Color(3f, 2.4f, 1.2f)); lr.startColor = lr.endColor = new Color(1f, 0.9f, 0.6f, 1f);
            var e = g.AddComponent<LightBeamEmitter>(); e.line = lr; e.maxDistance = 60f; return e;
        }
        LightSocket Socket(Transform root, string name, Vector3 pos)
        {
            var g = ZoneKit.Part(root, name, pos, new Vector3(0.95f, 0.95f, 0.1f), ZoneKit.Tinted("sandstone_blocks_08", new Color(0.5f, 0.42f, 0.3f), 0.35f, Color.black));
            ZoneKit.Round(g.transform, "Ring", new Vector3(0f, 0f, -0.6f), new Vector3(0.75f, 0.2f, 0.75f), ZoneKit.Tinted("clay_plaster", new Color(0.72f, 0.5f, 0.2f), 0.8f)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var s = g.AddComponent<LightSocket>(); s.elementId = "z5." + name; s.glow = g.GetComponent<Renderer>(); s.litColor = new Color(3f, 2.2f, 0.9f); return s;
        }

        public void Build(Vector3 at, Quaternion facing)
        {
            transform.SetPositionAndRotation(at, facing); var root = transform;
            var bronze = ZoneKit.Tinted("clay_plaster", new Color(0.78f, 0.55f, 0.22f), 0.9f); bronze.SetFloat("_Metallic", 1f);
            Village.Box(root, "Plaza", new Vector3(0f, 0.03f, 1.5f), new Vector3(25f, 0.06f, 33f), "red_sandstone_pavement", 3f, false);
            // the carved wall with a hidden door and a small chamber behind it
            Village.Box(root, "SunWall_L", new Vector3(-5.6f, 3f, 15f), new Vector3(8.8f, 6f, 1f), "sandstone_blocks_08", 2.4f);
            Village.Box(root, "SunWall_R", new Vector3(5.6f, 3f, 15f), new Vector3(8.8f, 6f, 1f), "sandstone_blocks_08", 2.4f);
            Village.Box(root, "SunWall_Top", new Vector3(0f, 4.4f, 15f), new Vector3(2.4f, 3.2f, 1f), "sandstone_blocks_08", 2.4f);
            Village.Box(root, "SunWall_Cap", new Vector3(0f, 6.2f, 15f), new Vector3(20.8f, 0.4f, 1.4f), "red_sandstone_wall", 2.4f, false);
            Village.Box(root, "Chamber_Back", new Vector3(0f, 1.5f, 18.4f), new Vector3(4.4f, 3f, 0.3f), "red_sandstone_wall", 2f);
            Village.Box(root, "Chamber_L", new Vector3(-2.05f, 1.5f, 16.9f), new Vector3(0.3f, 3f, 2.8f), "red_sandstone_wall", 2f);
            Village.Box(root, "Chamber_R", new Vector3(2.05f, 1.5f, 16.9f), new Vector3(0.3f, 3f, 2.8f), "red_sandstone_wall", 2f);
            Village.Box(root, "Chamber_Roof", new Vector3(0f, 3.1f, 16.9f), new Vector3(4.4f, 0.2f, 3.2f), "red_sandstone_wall", 2f, false);
            door = Village.Box(root, "HiddenDoor", new Vector3(0f, 1.4f, 14.7f), new Vector3(2.4f, 2.8f, 0.3f), "sandstone_blocks_08", 2.4f).transform;
            ArtUpgrade.Place(root, "lion_head", root.TransformPoint(new Vector3(-1.9f, 3.3f, 14.4f)), 1.4f, 180f, false);
            ArtUpgrade.Place(root, "lion_head", root.TransformPoint(new Vector3(1.9f, 3.3f, 14.4f)), 1.4f, 180f, false);
            Village.Box(root, "Sundial", new Vector3(-10.5f, 1.1f, 9f), new Vector3(0.5f, 2.2f, 0.5f), "sandstone_blocks_08", 1.2f);

            // art pass: coping on the wall, carved pillars along its face, an arch framing the hidden door, lamp stands
            ZoneDress.Trim(root, "SunWall_L", "SunWall_R", "Chamber_");
            foreach (float x in new[] { -9.6f, -4.6f, 4.6f, 9.6f }) ZoneDress.Prop(root, "SM_V3_Pillar", new Vector3(x, 0f, 14.1f), 0f, new Vector3(1f, 1.9f, 1f));
            ZoneDress.Prop(root, "SM_V3_Arch", new Vector3(0f, 0f, 14.25f), 0f, new Vector3(0.62f, 0.7f, 0.6f));
            ZoneDress.Lamp(root, new Vector3(-11.5f, 0.06f, 12f), 7f, 2.5f); ZoneDress.Lamp(root, new Vector3(11.5f, 0.06f, 12f), 7f, 2.5f);
            Sockets = new[] { Socket(root, "socket_right", new Vector3(8f, 1.6f, 14.45f)), Socket(root, "socket_left", new Vector3(-2f, 1.6f, 14.45f)) };
            Beams = new[] { Beam(root, "beam_west", new Vector3(-8f, 1.6f, -13f)), Beam(root, "beam_east", new Vector3(8f, 1.6f, -13f)) };
            Mirrors = new[] { Mirror(root, "mirror_1", new Vector3(-8f, 1.6f, 0f), 90f, bronze), Mirror(root, "mirror_2", new Vector3(8f, 1.6f, 0f), 270f, bronze),
                              Mirror(root, "mirror_3", new Vector3(8f, 1.6f, -6f), 180f, bronze), Mirror(root, "mirror_4", new Vector3(-2f, 1.6f, -6f), 0f, bronze) };

            // the chariot wheel: four spokes; a spoke pointing straight down blocks the west beam
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
            { var leg = Village.Box(root, "WheelLeg", new Vector3(sx * 0.75f, 1.5f, sz * 0.62f), new Vector3(0.22f, 3.3f, 0.22f), "medieval_wood", 1f); leg.transform.localRotation = Quaternion.Euler(-sz * 22f, 0f, 0f); }
            var wg = new GameObject("ChariotWheel"); wg.transform.SetParent(root, false); wg.transform.localPosition = new Vector3(0f, 3f, 0f);
            var stone = ZoneKit.Tinted("sandstone_blocks_08", new Color(0.8f, 0.72f, 0.6f), 0.3f);
            for (int k = 0; k < 4; k++) { var pivot = new GameObject("SpokePivot" + k).transform; pivot.SetParent(wg.transform, false); pivot.localRotation = Quaternion.Euler(k * 90f, 0f, 0f); ZoneKit.Part(pivot, "Spoke", new Vector3(0f, -1.45f, 0f), new Vector3(0.34f, 2.8f, 0.36f), stone); }
            for (int k = 0; k < 24; k++) { var pivot = new GameObject("RimPivot" + k).transform; pivot.SetParent(wg.transform, false); pivot.localRotation = Quaternion.Euler(k * 15f, 0f, 0f); ZoneKit.Part(pivot, "Rim", new Vector3(0f, -2.95f, 0f), new Vector3(0.42f, 0.3f, 0.82f), stone, false); }
            ZoneKit.Round(wg.transform, "Hub", Vector3.zero, new Vector3(0.9f, 0.3f, 0.9f), stone).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var reach = new GameObject("WheelReach"); reach.transform.SetParent(wg.transform, false); reach.transform.localPosition = new Vector3(0f, -2.2f, 0f); ZoneKit.Reach(reach, 2.2f);
            Wheel = wg.AddComponent<RotatingDisc>(); Wheel.elementId = "z5.wheel"; Wheel.soundVolume = 1f; Wheel.steps = 8; Wheel.axis = Vector3.right; Wheel.verb = "Push the wheel"; Wheel.radius = 5f; Wheel.turnSeconds = 1.1f;

            // behind the door: the finial's empty case and the boat receipt
            var caseGo = Village.Box(root, "FinialCase", new Vector3(0f, 0.3f, 17.3f), new Vector3(0.8f, 0.5f, 0.55f), "medieval_wood", 0.8f, false); ZoneKit.Reach(caseGo, 2.4f);
            caseGo.GetComponent<Renderer>().sharedMaterial = ZoneKit.Tinted("medieval_wood", new Color(0.45f, 0.2f, 0.12f), 0.4f);
            Case = caseGo.AddComponent<TreasurePickup>(); Case.treasureName = "the finial's empty case and a boat receipt"; Case.zoneId = ZoneId; Case.enabled = false;

            var gg = new GameObject("Puzzle_SunWall"); gg.transform.SetParent(root, false); gg.transform.localPosition = new Vector3(0f, 0f, 2f);
            Group = gg.AddComponent<PuzzleGroup>(); Group.puzzleId = "z5_sunwall"; Group.zoneId = ZoneId; Group.hintRadius = 24f;
            Group.hints = new[] { "Light has to reach two carvings on the wall.", "One beam must pass between the spokes of the great wheel.", "Turn each mirror two stops so the beams bend toward the wall, then push the wheel one stop so no spoke hangs straight down." };
            foreach (var s in Sockets) Group.conditions.Add(new PuzzleGroup.Condition { element = s, state = 1 });
            Group.OnSolved += _ => { foreach (var m in Mirrors) m.locked = true; Wheel.locked = true; ObjectiveBanner.Show("THE WALL OPENS", "", 3f); if (door != null) GameSfx.At("sfx_wall_open", door.position, 1f, 0.02f, 50f); };

            var lookGo = new GameObject("ZoneLook_SunWall"); lookGo.transform.SetParent(root, false); lookGo.transform.localPosition = new Vector3(0f, 10f, 2f);
            var look = lookGo.AddComponent<ZoneLook>(); look.zone = ZoneLook.Zone.SunWall; look.size = new Vector3(27f, 30f, 38f); look.blendDistance = 6f; look.Build();
            ZoneAtmosphere.Register(look, facing * Quaternion.Euler(64f, 0f, 0f));
            Physics.SyncTransforms(); Built = true;
            Debug.Log($"[SunWall] zone 5 puzzle built at {at:0.0}");
        }

        void Update()
        {
            if (!Built || door == null) return;
            bool open = Group.Solved;
            if (open && !Case.enabled && !Case.Taken)
            {   // Kishore steps out as the wall opens: a two-phase fight in the plaza
                if (Brawl.WonBefore(ZoneId)) Case.enabled = true;
                else if (Brawl.Active == null) Brawl.Begin(ZoneId, transform.TransformPoint(new Vector3(0f, 0f, 8f)), 6f, new[] { Brawl.Robber("Kishore", 2, BrawlType.Boss, 1.1f, new Color(0.75f, 0.7f, 0.85f)) }, null);
            }
            doorOpen = Mathf.MoveTowards(doorOpen, open ? 1f : 0f, Time.deltaTime * 0.5f);
            door.localPosition = new Vector3(0f, 1.4f - 2.75f * Mathf.SmoothStep(0f, 1f, doorOpen), 14.7f);
        }
    }
}
