using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    public enum EnemyState { Idle, Circle, Telegraph, Recover, Stunned, Staggered, Down, Flee, Ally }
    public enum Move { Slash, DoubleSlash, Kick, WideSwing, Lunge, ScarfFeint, Shove, WildSwing }

    /// <summary>
    /// One robber in the fight. Uses a Guard meter instead of health: at zero he is staggered and a finisher
    /// subdues him (nobody is killed). Every attack is telegraphed: white glint = block or counter, red glint = dodge.
    /// NOTE: attack animations are not in the project yet (Mixamo clips), so attacks are shown by the glint,
    /// the swish and the lunge movement only. Locomotion uses the real walk/run clips.
    /// </summary>
    public sealed class EnemyBrain : MonoBehaviour
    {
        public string who = "bhaskar";
        public float Guard, MaxGuard = 120f;
        public EnemyState State { get; private set; } = EnemyState.Idle;
        public Move Current { get; private set; }
        public bool Unblockable => Current == Move.WideSwing || Current == Move.Lunge;
        public float TelegraphLeft { get; private set; }
        public int phase = 1;
        public bool hasBlade = true;
        public Transform target; public EnemyBrain partner;
        public System.Action<EnemyBrain, Move> onAttackLands;
        public Vector3 arenaCentre; public float arenaRadius = 7f;
        public int AttacksStarted { get; private set; }

        private float stateT, nextAttack, strafe = 1f; private int hitsLeft; private GameObject glint; private Light glintLight;
        private Vector3 lungeDir;

        public bool IsKishore => who == "kishore";

        private void Start()
        {
            glint = Fx.Prim(PrimitiveType.Sphere, "Glint", transform, transform.position + Vector3.up * 1.5f, Vector3.one * 0.01f, Fx.Res("Spark"));
            glintLight = new GameObject("GlintLight").AddComponent<Light>(); glintLight.transform.SetParent(glint.transform, false);
            glintLight.type = LightType.Point; glintLight.range = 4f; glintLight.intensity = 0f;
            nextAttack = Time.time + Random.Range(1.2f, 2.2f);
        }

        public void Begin(float guard) { MaxGuard = Guard = guard; State = EnemyState.Circle; stateT = 0f; nextAttack = Time.time + 1.6f; }
        public void SetState(EnemyState s) { State = s; stateT = 0f; TelegraphLeft = 0f; }

        public static float TelegraphTime(Move m)
        {
            switch (m)
            {
                case Move.Slash: return 0.45f; case Move.DoubleSlash: return 0.45f; case Move.Kick: return 0.4f;
                case Move.WideSwing: return 0.7f; case Move.Lunge: return 0.8f; case Move.ScarfFeint: return 0.9f;
                case Move.Shove: return 0.9f; default: return 0.9f;
            }
        }
        public static float Damage(Move m)
        {
            switch (m) { case Move.Slash: case Move.DoubleSlash: case Move.ScarfFeint: return 18f; case Move.Kick: return 10f; case Move.WideSwing: return 26f; case Move.Lunge: return 30f; case Move.Shove: return 6f; default: return 10f; }
        }

        /// <summary>Guard damage from a hit, counter or environment trick. Returns true if this staggered him.</summary>
        public bool TakeGuard(float amount, float stagger = 0f)
        {
            if (State == EnemyState.Down || State == EnemyState.Ally || State == EnemyState.Flee && amount < 999f) { }
            if (State == EnemyState.Down || State == EnemyState.Ally) return false;
            Guard = Mathf.Max(0f, Guard - amount);
            if (Guard <= 0f) { SetState(EnemyState.Staggered); return true; }
            if (stagger > 0f) { SetState(EnemyState.Stunned); stateT = -stagger + 0.001f; stunFor = stagger; }
            return false;
        }
        private float stunFor = 1f;
        public void Stun(float seconds) { if (State == EnemyState.Down || State == EnemyState.Ally || State == EnemyState.Staggered) return; SetState(EnemyState.Stunned); stunFor = seconds; stateT = 0f; }

        public void StartMove(Move m)
        {
            Current = m; SetState(EnemyState.Telegraph); TelegraphLeft = TelegraphTime(m); hitsLeft = m == Move.DoubleSlash ? 2 : 1; AttacksStarted++;
            AudioManager.Instance?.PlaySFX(Unblockable ? "sfx_swish_wide" : "sfx_swish_quick", 0.8f);
            if (m == Move.Lunge) { lungeDir = Flat(target.position - transform.position).normalized; StoryDialogueSystem.Instance?.Say("boss_lunge"); }
            if (IsKishore) StoryDialogueSystem.Instance?.Say("kishore_attack");
        }

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        private Move Pick()
        {
            if (IsKishore) return Random.value < 0.5f ? Move.Shove : Move.WildSwing;
            var list = new List<Move> { Move.Slash, Move.DoubleSlash, Move.Kick };
            if (phase >= 2) { list.Add(Move.WideSwing); list.Add(Move.Lunge); list.Add(Move.ScarfFeint); list.Add(Move.WideSwing); }
            if (!hasBlade) { list.Clear(); list.Add(Move.Kick); list.Add(Move.Shove); }
            return list[Random.Range(0, list.Count)];
        }

        private void MoveBy(Vector3 delta)
        {
            Vector3 p = transform.position + delta;
            Vector3 fromC = Flat(p - arenaCentre);
            if (State != EnemyState.Flee && fromC.magnitude > arenaRadius) p = arenaCentre + fromC.normalized * arenaRadius;
            // stay on the arena floor: never more than a step above or below it, whatever the ray finds
            p.y = Mathf.Clamp(Fx.Floor(new Vector3(p.x, arenaCentre.y + 0.3f, p.z), transform), arenaCentre.y - 1f, arenaCentre.y + 1f);
            transform.position = p;
        }

        private void Face(Vector3 dir, float speed = 8f)
        {
            dir = Flat(dir); if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
        }

        private void Update()
        {
            if (target == null) return;
            stateT += Time.deltaTime;
            Vector3 to = Flat(target.position - transform.position); float dist = to.magnitude;
            bool tele = State == EnemyState.Telegraph;
            // glint: white = can be blocked, red = must be dodged
            float g = tele ? Mathf.PingPong(stateT * 6f, 1f) * 0.25f + 0.12f : 0.01f;
            glint.transform.localScale = Vector3.one * g / Mathf.Max(0.01f, transform.lossyScale.x);
            glint.transform.position = transform.position + transform.forward * 0.45f + Vector3.up * 1.45f;
            var gc = Unblockable ? new Color(1f, 0.15f, 0.1f) : Color.white;
            glint.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", gc);
            glintLight.color = gc; glintLight.intensity = tele ? 3f : 0f;
            if (tele) ScreenText.WorldLabel("tele_" + who, transform.position + Vector3.up * 2.25f, Unblockable ? "DODGE!" : "BLOCK!", gc);

            switch (State)
            {
                case EnemyState.Circle:
                    Face(to);
                    float want = IsKishore ? 4.2f : 3f;
                    Vector3 side = Vector3.Cross(Vector3.up, to.normalized) * strafe;
                    Vector3 mv = side * 0.9f + to.normalized * Mathf.Clamp(dist - want, -1f, 1f) * 1.6f;
                    MoveBy(mv * Time.deltaTime);
                    if (Random.value < Time.deltaTime * 0.25f) strafe = -strafe;
                    if (dist > 6.5f && !IsKishore && stateT > 6f) { StoryDialogueSystem.Instance?.Say("boss_taunt"); stateT = 0f; }
                    bool partnerBusy = partner != null && partner.State == EnemyState.Telegraph;
                    if (Time.time > nextAttack && dist < (IsKishore ? 5f : 4.5f) && !partnerBusy) StartMove(Pick());
                    break;
                case EnemyState.Telegraph:
                    TelegraphLeft -= Time.deltaTime;
                    if (Current == Move.Lunge) { if (TelegraphLeft < 0.3f) MoveBy(lungeDir * 9f * Time.deltaTime); else Face(to); }
                    else { Face(to); if (dist > 1.9f) MoveBy(to.normalized * 2.2f * Time.deltaTime); }
                    if (TelegraphLeft <= 0f)
                    {
                        onAttackLands?.Invoke(this, Current);
                        hitsLeft--;
                        if (State != EnemyState.Telegraph) break; // countered / stunned by the callback
                        if (hitsLeft > 0) { TelegraphLeft = 0.35f; AudioManager.Instance?.PlaySFX("sfx_swish_quick", 0.8f); }
                        else { SetState(EnemyState.Recover); }
                    }
                    break;
                case EnemyState.Recover:
                    float rec = Current == Move.WideSwing ? 1.2f : Current == Move.Lunge ? 1.0f : 0.55f;
                    if (stateT > rec)
                    {
                        SetState(EnemyState.Circle);
                        float gap = IsKishore ? Random.Range(4f, 5f) : Random.Range(1.6f, 2.4f) - 0.25f * (phase - 1);
                        nextAttack = Time.time + gap;
                    }
                    break;
                case EnemyState.Stunned:
                    if (stateT > stunFor) { SetState(EnemyState.Circle); nextAttack = Time.time + 0.8f; }
                    else ScreenText.WorldLabel("stun_" + who, transform.position + Vector3.up * 2.25f, "STUNNED", new Color(1f, 0.9f, 0.4f));
                    break;
                case EnemyState.Staggered:
                    ScreenText.WorldLabel("stag_" + who, transform.position + Vector3.up * 2.25f, "STAGGERED  [E]", new Color(1f, 0.78f, 0.34f));
                    break;
            }
        }
    }

    /// <summary>
    /// Chapter 5 director: stealth (grass, cover, pebbles, vision cones, detection meter), then the three-phase
    /// fight with Bhaskar (and Kishore, who turns on him if trust_meera is 2 or more), the pursuit and the grapple.
    /// Also the player's combat actions: light / heavy attack, block, counter, dodge, flashlight flash, brazier and rope.
    /// </summary>
    public sealed class FightDirector : MonoBehaviour
    {
        public static FightDirector Instance { get; private set; }
        public static int SavedPhase; // mid-fight checkpoint (survives a retry reload)

        // ---- public state (HUD, tests) ----
        public float Health { get; private set; } = 100f;
        public float Stamina { get; private set; } = 100f;
        public int Phase { get; private set; }             // 0 = not fighting, 1..3
        public bool Blocking { get; private set; }
        public bool Invulnerable => Time.time < dodgeUntil;
        public float DodgeReady01 => Mathf.Clamp01(Stamina / 22f);
        public EnemyBrain Bhaskar { get; private set; }
        public EnemyBrain Kishore { get; private set; }
        public EnemyBrain Target { get; private set; }
        public float Detection { get; private set; }       // 0..1 stealth meter
        public int Spotted { get; private set; }
        public int Counters { get; private set; }
        public int HitsLanded { get; private set; }
        public int HitsTaken { get; private set; }
        public float LastDamageTime { get; private set; } = -9f;
        public Vector3 LastDamageDir { get; private set; }
        public float LastHitMarker { get; private set; } = -9f;
        public bool Hidden { get; private set; }
        public bool Grappling { get; private set; }
        public float Tug { get; private set; }
        public int Phase3Fails { get; private set; }
        public bool StealthRunning => stealth;
        public string Banner { get; private set; } = "";

        private StoryDialogueSystem story; private PlayerController player; private string scene; private bool built; private Transform root;
        private readonly List<Transform> robbers = new List<Transform>(); private readonly List<Vector3> starts = new List<Vector3>();
        private Vector3 dir, playerStart, arena; private float traveled, stealthT, listenT, blockStart = -9f, dodgeUntil, attackUntil, bannerUntil, lastBark = -9f, lastStamUse;
        private bool stealth, fightStarted, intro; private int overheard, pebbles; private float pebbleWindow;
        private readonly List<Vector3> grass = new List<Vector3>(); private readonly List<(Transform t, bool used)> braziers = new List<(Transform, bool)>();
        private Transform poles; private bool ropeUsed; private float investigateUntil; private Vector3 investigatePos; private Transform investigator;
        private float fleeDist, tugTimer; private int tugKey; private float ringPos, ringDir = 1f; private bool ringStage; private float stoneT;
        private Coroutine routine; private float heavyHold = -1f; private int combo; private float comboUntil;
        private const float PathLength = 26f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("FightDirector"); DontDestroyOnLoad(go);
            Instance = go.AddComponent<FightDirector>();
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += (s, m) => { scene = s.name; built = false; ResetRuntime(); };
            scene = SceneManager.GetActiveScene().name;
            StoryDialogueSystem.OnNewGame += () => SavedPhase = 0;
            Flashlight.OnFlash += OnFlash;
        }

        private void Start() { story = StoryDialogueSystem.Instance; story?.RegisterMinigame("stealth"); story?.RegisterMinigame("fight"); }

        private void ResetRuntime()
        {
            CombatState.InCombat = false; Phase = 0; stealth = false; fightStarted = false; intro = false; Grappling = false; Blocking = false;
            Health = 100f; Stamina = 100f; Detection = 0f; robbers.Clear(); starts.Clear(); grass.Clear(); braziers.Clear(); Bhaskar = Kishore = Target = null;
            if (routine != null) { StopCoroutine(routine); routine = null; }
            Time.timeScale = 1f;
        }

        private bool Is(string mg) => story != null && story.Current != null && story.Current.complete.id == mg && story.Current.scene == scene;
        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
        private void Say(string trigger, bool force = false) { if (force || Time.time - lastBark > 6f) { lastBark = Time.time; story.Say(trigger); } }
        private void SetBanner(string t, float seconds = 2.5f) { Banner = t; bannerUntil = Time.unscaledTime + seconds; }

        private bool FindRobbers()
        {
            robbers.Clear(); starts.Clear();
            Transform r1 = null, r2 = null;
            foreach (var n in FindObjectsByType<NPCDialogueTrigger>())
            {
                if (n.Id == "npc_" + Constants.Robber1) r1 = n.transform;
                if (n.Id == "npc_" + Constants.Robber2) r2 = n.transform;
            }
            if (r1 == null) return false;
            robbers.Add(r1); starts.Add(r1.position);
            if (r2 != null) { robbers.Add(r2); starts.Add(r2.position); }
            foreach (var r in robbers) if (r.GetComponent<NpcAnimDriver>() == null) r.gameObject.AddComponent<NpcAnimDriver>();
            return true;
        }

        private void Update()
        {
            if (story == null) story = StoryDialogueSystem.Instance;
            if (story == null || scene != Constants.JunglePath || SceneLoader.IsLoading) return;
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (player == null || Time.timeSinceLevelLoad < 1f) return;
            if (!built)
            {
                if (!FindRobbers()) return;
                built = true; root = new GameObject("FightProps").transform;
                Vector3 d = Flat(robbers[0].position - player.transform.position);
                dir = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward; playerStart = player.transform.position;
                BuildStealthProps();
            }
            if (DawnClock.Instance != null && DawnClock.Instance.OutOfTime) return;
            if (Time.unscaledTime < bannerUntil && Banner.Length > 0) ScreenText.Panel("fight_banner", null, $"<size=130%><b><color={ScreenText.Gold}>{Banner}</color></b></size>", PanelAnchor.TopCenter, 900f, null, false, 2);
            if (Is("stealth")) UpdateStealth();
            else if (Is("fight")) UpdateFight();
            else if (CombatState.InCombat) { CombatState.InCombat = false; Phase = 0; }
        }

        // ================================================================== STEALTH
        private void BuildStealthProps()
        {
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            var stone = Fx.Lit(new Color(0.42f, 0.36f, 0.3f), 0.2f); var grassMat = Fx.Res("Grass");
            Vector3 a = robbers[0].position;
            for (int i = 0; i < 5; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 c = Fx.Ground(playerStart + dir * (3f + i * 6f) + right * side * 3.2f);
                grass.Add(c);
                var patch = new GameObject("TallGrass").transform; patch.SetParent(root, false); patch.position = c;
                for (int k = 0; k < 26; k++)
                {
                    Vector2 r = Random.insideUnitCircle * 2.1f;
                    var q = Fx.Prim(PrimitiveType.Quad, "Blade", patch, Fx.Ground(c + new Vector3(r.x, 0f, r.y)) + Vector3.up * 0.55f, new Vector3(0.5f, 1.1f, 1f), grassMat);
                    q.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    if (k < 4) q.AddComponent<WindSway>().amount = 2f;
                }
                Vector3 s = Fx.Ground(playerStart + dir * (6f + i * 6f) - right * side * 2.6f);
                var rock = Fx.Prim(PrimitiveType.Cube, "CarvedStone", root, s + Vector3.up * 0.75f, new Vector3(1.5f, 1.5f, 0.7f), ArtUpgrade.Mat("mossy_sandstone") ?? stone, true);
                rock.transform.rotation = Quaternion.LookRotation(right);
            }
            if (StoryFlags.Get("porter_path") == 1)
            {   // Meera's chalk marks: one more safe patch
                Vector3 c = Fx.Ground(playerStart + dir * 15f + right * 5.5f); grass.Add(c);
                Fx.Prim(PrimitiveType.Cube, "ChalkMark", root, c + Vector3.up * 0.02f, new Vector3(0.5f, 0.02f, 0.12f), Fx.Lit(Color.white, 0.1f, 0f, new Color(0.6f, 0.6f, 0.6f)));
            }
            _ = a;
        }

        private void UpdateStealth()
        {
            if (!stealth) { stealth = true; traveled = 0f; stealthT = 0f; listenT = 0f; Detection = 0f; overheard = 0; }
            stealthT += Time.deltaTime;
            float cycle = stealthT % 8f; bool pause = cycle > 5f; // walk 5 s, stop and look around 3 s
            bool moving = !pause && traveled < PathLength && Time.time > investigateUntil;
            Vector3 pp = player.transform.position;
            Hidden = false;
            if (player.IsCrouching) foreach (var g in grass) if (Flat(pp - g).magnitude < 2.3f) Hidden = true;
            bool seen = false; float nearest = 999f;
            for (int i = 0; i < robbers.Count; i++)
            {
                var r = robbers[i]; if (r == null) continue;
                Vector3 look = dir;
                if (Time.time < investigateUntil && r == investigator) look = Flat(investigatePos - r.position);
                else if (pause) look = Quaternion.Euler(0f, Mathf.Sin((cycle - 5f) / 3f * Mathf.PI * 2f + i) * 150f, 0f) * dir;
                if (look.sqrMagnitude > 0.01f) r.rotation = Quaternion.Slerp(r.rotation, Quaternion.LookRotation(look), Time.deltaTime * 3.5f);
                if (moving) { Vector3 np = r.position + dir * 1.15f * Time.deltaTime; np.y = Fx.Floor(np, r); r.position = np; }
                Vector3 eye = r.position + Vector3.up * 1.6f, chest = pp + Vector3.up * (player.IsCrouching ? 0.8f : 1.4f);
                Vector3 to = chest - eye; float d = to.magnitude; nearest = Mathf.Min(nearest, d);
                float range = GameOptions.SightRange * (player.IsCrouching ? 0.6f : 1f);
                bool inCone = d < range && Vector3.Angle(r.forward, Flat(to)) < GameOptions.SightHalfAngle;
                if (inCone && Hidden && d > 3f) inCone = false;
                if (inCone && Physics.Raycast(eye, to.normalized, out RaycastHit hit, d, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(player.transform)) inCone = false;
                bool heard = !player.IsCrouching && player.IsMoving && d < (player.IsRunning ? 8f : 3f);
                if (inCone || heard) seen = true;
                DrawCone(r, range, inCone);
            }
            if (moving) traveled += 1.15f * Time.deltaTime;
            Detection = Mathf.Clamp01(Detection + (seen ? Time.deltaTime / GameOptions.DetectSeconds : -Time.deltaTime / (GameOptions.DetectSeconds * 2f)));

            // listening post: close and unseen
            if (nearest < 10f && !seen) { listenT += Time.deltaTime; if (listenT > 3.5f && overheard < 6) { listenT = 0f; overheard++; story.Say("overhear_" + overheard); } }

            // pebble (T): the nearest robber turns to the sound for six seconds
            var kb = Keyboard.current;
            if (((kb != null && kb.tKey.wasPressedThisFrame) || VirtualInput.Down("pebble")) && !InputGate.GameplayBlocked) ThrowPebble();

            if (Detection >= 1f)
            {
                Spotted++; Detection = 0f;
                if (Spotted >= 3) { SetBanner("THEY HAVE SEEN YOU"); EndStealth(); return; }
                DawnClock.Instance?.Penalty(3f, "spotted");
                for (int i = 0; i < robbers.Count; i++) if (robbers[i] != null) robbers[i].position = starts[i];
                player.Teleport(playerStart, Quaternion.LookRotation(dir)); traveled = 0f; stealthT = 0f;
                SetBanner("SPOTTED: back to cover (" + Spotted + "/3)");
                AudioManager.Instance?.PlaySFX("sfx_spotted");
            }
            else if (nearest > 30f) { for (int i = 0; i < robbers.Count; i++) if (robbers[i] != null) robbers[i].position = starts[i]; player.Teleport(playerStart, Quaternion.LookRotation(dir)); traveled = 0f; stealthT = 0f; SetBanner("YOU LOST THEM"); }
            if (traveled >= PathLength && !pause) { EndStealth(); return; }

            string st = pause ? $"<color={ScreenText.Coral}>They stopped to look around. Hide!</color>" : $"<color={ScreenText.Green}>They are moving. Follow quietly.</color>";
            string hid = Hidden ? $"<color={ScreenText.Green}>HIDDEN in the grass</color>" : player.IsCrouching ? "Crouched" : $"<color={ScreenText.Coral}>Standing: easy to see</color>";
            ScreenText.Panel("stealth", null, $"{st}\nSeen {Fx.Bar(Detection, 16)}  {hid}\nPath {traveled / PathLength * 100f:0}%   {ScreenText.Key("C")} Crouch   {ScreenText.Key("T")} Pebble", PanelAnchor.TopRight, 640f, null, false, 4);
        }

        public void ThrowPebble()
        {
            if (Time.time - pebbleWindow > 20f) { pebbles = 0; pebbleWindow = Time.time; }
            pebbles++;
            var cam = Camera.main; Vector3 f = cam != null ? Flat(cam.transform.forward).normalized : player.transform.forward;
            Vector3 land = Fx.Ground(player.transform.position + f * 9f);
            Fx.Burst("Dust", land + Vector3.up * 0.1f, 8, 1f, 0.2f, 0.6f, new Color(0.7f, 0.65f, 0.55f, 0.6f));
            AudioManager.Instance?.PlaySFX("sfx_pebble");
            if (pebbles >= 3) { SetBanner("They are getting suspicious of the noises", 2f); return; }
            Transform best = null; float bd = 14f;
            foreach (var r in robbers) { float d = Flat(r.position - land).magnitude; if (d < bd) { bd = d; best = r; } }
            if (best != null) { investigator = best; investigatePos = land; investigateUntil = Time.time + 6f; }
        }

        private readonly Dictionary<Transform, MeshRenderer> cones = new Dictionary<Transform, MeshRenderer>();
        private void DrawCone(Transform r, float range, bool alert)
        {
            if (!cones.TryGetValue(r, out var mr) || mr == null)
            {
                var go = new GameObject("VisionCone"); go.transform.SetParent(r, false); go.transform.localPosition = Vector3.up * 0.08f;
                var mesh = new Mesh(); int n = 12; var v = new Vector3[n + 2]; var tri = new int[n * 3];
                float half = GameOptions.SightHalfAngle * Mathf.Deg2Rad;
                for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(-half, half, i / (float)n); v[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); }
                for (int i = 0; i < n; i++) { tri[i * 3] = 0; tri[i * 3 + 1] = i + 1; tri[i * 3 + 2] = i + 2; }
                mesh.vertices = v; mesh.triangles = tri; mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh; mr = go.AddComponent<MeshRenderer>(); mr.material = Fx.Res("Glow");
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; cones[r] = mr;
            }
            mr.enabled = stealth;
            float s = range / Mathf.Max(0.01f, r.lossyScale.x);
            mr.transform.localScale = new Vector3(s, 1f, s);
            mr.material.SetColor("_BaseColor", alert ? new Color(1f, 0.2f, 0.1f, 0.22f) : new Color(1f, 0.85f, 0.3f, 0.08f));
        }

        private void EndStealth()
        {
            stealth = false; foreach (var c in cones.Values) if (c != null) c.enabled = false;
            story.CompleteMinigame("stealth");
        }

        // ================================================================== FIGHT
        private void BeginFight()
        {
            fightStarted = true; CombatState.InCombat = true;
            foreach (var c in cones.Values) if (c != null) c.enabled = false;
            if (robbers.Count == 0 && !FindRobbers()) return;
            arena = Fx.Ground(robbers[0].position + dir * 4f);
            Bhaskar = robbers[0].gameObject.GetComponent<EnemyBrain>(); if (Bhaskar == null) Bhaskar = robbers[0].gameObject.AddComponent<EnemyBrain>();
            Bhaskar.who = "bhaskar"; Bhaskar.target = player.transform; Bhaskar.arenaCentre = arena; Bhaskar.onAttackLands = AttackLands;
            if (robbers.Count > 1)
            {
                Kishore = robbers[1].gameObject.GetComponent<EnemyBrain>(); if (Kishore == null) Kishore = robbers[1].gameObject.AddComponent<EnemyBrain>();
                Kishore.who = "kishore"; Kishore.target = player.transform; Kishore.arenaCentre = arena; Kishore.onAttackLands = AttackLands; Kishore.partner = Bhaskar; Bhaskar.partner = Kishore;
            }
            foreach (var r in robbers) foreach (var t in r.GetComponents<NPCDialogueTrigger>()) t.enabled = false;
            BuildArena();
            WeatherManager.Instance?.SetTarget(1f, true);
            Health = 100f; Stamina = 100f;
            routine = StartCoroutine(SavedPhase >= 2 ? ResumeAt(SavedPhase) : Intro());
        }

        private void BuildArena()
        {
            var brass = Fx.Lit(new Color(0.5f, 0.36f, 0.14f), 0.5f, 0.8f); var wood = Fx.Lit(new Color(0.36f, 0.24f, 0.13f), 0.15f);
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            foreach (var o in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            {
                Vector3 p = Fx.Ground(arena + right * o.x * 5f + dir * o.y * 5f);
                var b = Fx.Prim(PrimitiveType.Cylinder, "Brazier", root, p + Vector3.up * 0.5f, new Vector3(0.55f, 0.5f, 0.55f), ArtUpgrade.Mat("red_sandstone_wall") ?? brass, true);
                var pit = ArtUpgrade.Place(b.transform, "stone_fire_pit", p + Vector3.up * 1.0f, 0.75f, 0f, false);
                if (pit != null) pit.transform.localScale = Vector3.one * 0.75f / 0.55f;
                var l = new GameObject("BrazierFire").AddComponent<Light>(); l.transform.SetParent(b.transform, false); l.transform.position = p + Vector3.up * 1.4f;
                l.type = LightType.Point; l.range = 9f; l.intensity = 3f; l.color = new Color(1f, 0.6f, 0.25f); l.shadows = LightShadows.None;
                l.gameObject.AddComponent<LightFlicker>();
                Fx.Prim(PrimitiveType.Sphere, "Coals", b.transform, p + Vector3.up * 1.05f, new Vector3(0.45f, 0.18f, 0.45f), Fx.Lit(new Color(0.2f, 0.05f, 0.02f), 0.1f, 0f, new Color(3f, 0.9f, 0.2f)));
                var t = b.transform; int idx = braziers.Count; braziers.Add((t, false));
                UseProp.Add(b, "brazier_" + idx, "Knock the brazier at him", 2.4f, () => KnockBrazier(idx), () => Phase >= 2 && !braziers[idx].used);
            }
            Vector3 top = arena + Vector3.up * 4.2f;
            poles = new GameObject("PoleBundle").transform; poles.SetParent(root, false); poles.position = top;
            for (int i = 0; i < 5; i++) { var p = Fx.Prim(PrimitiveType.Cylinder, "Pole", poles, top + new Vector3((i - 2) * 0.16f, 0f, 0f), new Vector3(0.12f, 1.6f, 0.12f), wood); p.transform.rotation = Quaternion.Euler(90f, 0f, 0f); }
            Fx.Prim(PrimitiveType.Cylinder, "PoleShadow", root, Fx.Ground(arena) + Vector3.up * 0.02f, new Vector3(2.2f, 0.005f, 2.2f), Fx.Lit(new Color(0.02f, 0.02f, 0.02f), 0f));
            Vector3 cleatPos = Fx.Ground(arena - right * 6.2f);
            var cleat = Fx.Prim(PrimitiveType.Cube, "RopeCleat", root, cleatPos + Vector3.up * 0.6f, new Vector3(0.25f, 1.2f, 0.25f), wood, true);
            UseProp.Add(cleat, "rope", "Pull the rope", 2.4f, PullRope, () => Phase >= 2 && !ropeUsed);
        }

        private IEnumerator Intro()
        {
            intro = true;
            SetBanner("THE BRAZIER COURTYARD", 3f);
            Vector3 stand = Fx.Ground(arena - dir * 4.5f);
            player.Teleport(stand, Quaternion.LookRotation(dir));
            foreach (var r in robbers) r.rotation = Quaternion.LookRotation(-dir);
            robbers[0].position = Fx.Ground(arena + dir * 1.5f);
            if (robbers.Count > 1) robbers[1].position = Fx.Ground(arena + dir * 3f + Vector3.Cross(Vector3.up, dir) * 2.5f);
            yield return new WaitForSeconds(0.6f);
            bool done = false; StoryDialogueSystem.OnBark += _ => { };
            story.PlayConversation(StoryData.Conversation("conv_c5_confront"), null);
            while (InputGate.UiOpen != null && InputGate.UiOpen()) yield return null;
            yield return new WaitForSeconds(0.3f);
            if (StoryFlags.Get("has_message") == 1)
            {   // timed choice: six seconds to speak to Kishore
                float until = Time.unscaledTime + 6f; int pick = -1;
                while (Time.unscaledTime < until && pick < 0)
                {
                    ScreenText.Panel("c5a", "KISHORE IS LISTENING", $"{ScreenText.Key("1")} Deliver Meera's message\n{ScreenText.Key("2")} Threaten him\n\n{Fx.Bar((until - Time.unscaledTime) / 6f)}", PanelAnchor.Center, 900f, null, true, 70);
                    var kb = Keyboard.current;
                    if (kb != null && kb.digit1Key.wasPressedThisFrame || VirtualInput.Down("choice1")) pick = 0;
                    if (kb != null && kb.digit2Key.wasPressedThisFrame || VirtualInput.Down("choice2")) pick = 1;
                    yield return null;
                }
                if (pick == 0) { StoryFlags.Apply("trust_meera+1"); story.Say("message_yes"); story.Say("message_yes_reply"); }
                else if (pick == 1) { StoryFlags.Apply("trust_meera-1"); story.Say("message_no"); story.Say("message_no_reply"); }
            }
            _ = done;
            intro = false;
            StartPhase(1);
        }

        private IEnumerator ResumeAt(int phase)
        {
            player.Teleport(Fx.Ground(arena - dir * 4.5f), Quaternion.LookRotation(dir));
            robbers[0].position = Fx.Ground(arena + dir * 1.5f);
            if (Kishore != null) { if (StoryFlags.Get("kishore_turned") == 1) Kishore.SetState(EnemyState.Ally); else Kishore.SetState(EnemyState.Down); Kishore.transform.position = Fx.Ground(arena + dir * 6.5f); }
            if (phase >= 3) DisarmBlade();
            yield return new WaitForSeconds(0.8f);
            StartPhase(phase);
        }

        public bool KishoreTurns => StoryFlags.Get("trust_meera") >= 2;

        public void StartPhase(int p)
        {
            Phase = p; SavedPhase = p; Health = 100f; Stamina = 100f; Grappling = false;
            Bhaskar.phase = p;
            if (p == 1)
            {
                SetBanner("PHASE 1"); story.Say("phase1"); story.Say("boss_phase");
                Bhaskar.Begin(120f * GameOptions.GuardScale);
                if (Kishore != null)
                {
                    if (KishoreTurns) Kishore.SetState(EnemyState.Idle);
                    else { Kishore.Begin(60f * GameOptions.GuardScale); story.Say("two_on_one"); story.Say("two_on_one_reply"); }
                }
            }
            else if (p == 2)
            {
                SetBanner("PHASE 2"); story.Say("phase2"); story.Say("phase2_reply");
                Bhaskar.Begin(120f * GameOptions.GuardScale);
            }
            else
            {
                SetBanner("PHASE 3: HE RUNS FOR THE FORD"); story.Say("phase3"); story.Say("phase3_reply");
                Bhaskar.Guard = Bhaskar.MaxGuard = 1f; Bhaskar.SetState(EnemyState.Flee); fleeDist = 0f; stoneT = 3f; Grappling = false; ringStage = false; Tug = 0f;
            }
            Target = Bhaskar;
        }

        private void DisarmBlade()
        {
            Bhaskar.hasBlade = false;
            foreach (var t in Bhaskar.GetComponentsInChildren<Transform>(true)) if (t.name == "CurvedBlade") t.gameObject.SetActive(false);
        }

        private void UpdateFight()
        {
            if (!fightStarted) { if (InputGate.UiOpen != null && InputGate.UiOpen()) return; BeginFight(); return; }
            if (intro || Bhaskar == null || Phase == 0) return;
            if (Health <= 0f) return;
            PlayerCombat();
            if (Phase == 3) { UpdatePursuit(); return; }
            // finisher on a staggered enemy
            foreach (var e in new[] { Bhaskar, Kishore })
            {
                if (e == null || e.State != EnemyState.Staggered) continue;
                var kb = Keyboard.current; var gp = Gamepad.current;
                bool press = (kb != null && kb.eKey.wasPressedThisFrame) || (gp != null && gp.buttonSouth.wasPressedThisFrame) || VirtualInput.Down("interact");
                if (press && Flat(e.transform.position - player.transform.position).magnitude < 3.2f) Finisher(e);
            }
        }

        /// <summary>Subdue a staggered enemy (ends a phase for Bhaskar, takes Kishore out of the fight).</summary>
        public void Finisher(EnemyBrain e)
        {
            if (e.State != EnemyState.Staggered) return;
            StartCoroutine(HitStop(0.12f));
            Fx.Burst("Spark", e.transform.position + Vector3.up * 1.2f, 20, 4f, 0.08f, 0.4f, Color.white);
            AudioManager.Instance?.PlaySFX("sfx_hit_heavy");
            Say("stagger");
            if (e == Kishore) { e.SetState(EnemyState.Down); story.Say("kishore_down"); DropManuscript(e.transform.position); Target = Bhaskar; return; }
            e.SetState(EnemyState.Down);
            routine = StartCoroutine(PhaseEnd());
        }

        private IEnumerator PhaseEnd()
        {
            int p = Phase; Phase = 0;
            yield return new WaitForSeconds(1.2f);
            if (p == 1)
            {
                if (Kishore != null && KishoreTurns && Kishore.State == EnemyState.Idle)
                {   // Kishore turns on Bhaskar: throws the manuscript to Aryan and tackles him
                    StoryFlags.Set("kishore_turned", 1);
                    story.Say("turn_1"); story.Say("turn_2"); story.Say("turn_3");
                    SetBanner("KISHORE TURNS ON BHASKAR", 3.5f);
                    Vector3 from = Kishore.transform.position;
                    for (float t = 0; t < 1f; t += Time.deltaTime / 0.8f) { Kishore.transform.position = Vector3.Lerp(from, Bhaskar.transform.position - dir * 0.8f, t); yield return null; }
                    Fx.Burst("Dust", Bhaskar.transform.position + Vector3.up, 16, 2f, 0.3f, 0.8f, new Color(0.7f, 0.65f, 0.55f, 0.6f));
                    AudioManager.Instance?.PlaySFX("sfx_body_fall");
                    Kishore.SetState(EnemyState.Ally);
                    EventBus.RaiseClueFound("relic_manuscript"); // caught: the Tala Patra is safe
                    yield return new WaitForSeconds(1.5f);
                    Kishore.transform.position = Fx.Ground(arena + dir * 6.5f);
                }
                else if (Kishore != null && Kishore.State != EnemyState.Down && Kishore.State != EnemyState.Ally) { /* he keeps fighting into phase 2 */ }
                StartPhase(2);
            }
            else if (p == 2) { DisarmBlade(); AudioManager.Instance?.PlaySFX("sfx_blade_drop"); SetBanner("DISARMED"); yield return new WaitForSeconds(1.2f); if (Kishore != null && Kishore.State != EnemyState.Ally && Kishore.State != EnemyState.Down) { Kishore.SetState(EnemyState.Down); story.Say("kishore_down"); DropManuscript(Kishore.transform.position); } StartPhase(3); }
        }

        private void DropManuscript(Vector3 at)
        {
            foreach (var c in FindObjectsByType<ClueInspectable>(FindObjectsInactive.Include)) if (c.Id == "relic_manuscript") { c.gameObject.SetActive(true); c.transform.position = Fx.Ground(at + dir * 0.8f) + Vector3.up * 0.3f; }
        }

        // ------------------------------------------------------------------ player actions
        private EnemyBrain Nearest(float maxDist, float maxAngle)
        {
            EnemyBrain best = null; float bs = 999f;
            foreach (var e in new[] { Bhaskar, Kishore })
            {
                if (e == null || e.State == EnemyState.Down || e.State == EnemyState.Ally || e.State == EnemyState.Idle) continue;
                Vector3 to = Flat(e.transform.position - player.transform.position); float d = to.magnitude;
                Vector3 f = Camera.main != null ? Flat(Camera.main.transform.forward) : player.transform.forward;
                float ang = Mathf.Min(Vector3.Angle(player.transform.forward, to), Vector3.Angle(f, to));
                if (d > maxDist || ang > maxAngle) continue;
                float score = d + ang * 0.03f; if (score < bs) { bs = score; best = e; }
            }
            return best;
        }

        public void SetBlocking(bool on) { if (on && !Blocking) blockStart = Time.time; Blocking = on && Stamina > 0f; }

        private void PlayerCombat()
        {
            var kb = Keyboard.current; var gp = Gamepad.current; var mouse = Mouse.current;
            bool gate = InputGate.GameplayBlocked;
            Target = Nearest(8f, 120f) ?? Target;
            bool blockHeld = !gate && ((mouse != null && mouse.rightButton.isPressed) || (kb != null && kb.qKey.isPressed) || (gp != null && gp.leftShoulder.isPressed) || VirtualInput.Block);
            SetBlocking(blockHeld);
            player.SpeedScale = Blocking ? 0.45f : Time.time < attackUntil ? 0.35f : 1f;

            if (Time.time - lastStamUse > 1f) Stamina = Mathf.Min(100f, Stamina + 25f * Time.deltaTime);
            if (Health < 40f && Time.time - LastDamageTime > 6f) Health = Mathf.Min(40f, Health + 6f * Time.deltaTime);
            if (gate || Time.time < attackUntil) return;

            bool dodge = (kb != null && kb.vKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame) || VirtualInput.Down("dodge");
            if (dodge) { Dodge(); return; }
            bool lightDown = (mouse != null && mouse.leftButton.wasPressedThisFrame && !PointerOverUi) || (gp != null && gp.buttonWest.wasPressedThisFrame) || VirtualInput.Down("attack");
            bool heavy = (kb != null && kb.rKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame) || VirtualInput.Down("heavy");
            // holding the left button for 0.35 s also gives a heavy attack
            if (mouse != null && !PointerOverUi)
            {
                if (mouse.leftButton.wasPressedThisFrame) { heavyHold = Time.time; lightDown = false; }
                if (heavyHold > 0f && mouse.leftButton.isPressed && Time.time - heavyHold > 0.35f) { heavy = true; heavyHold = -1f; }
                if (heavyHold > 0f && mouse.leftButton.wasReleasedThisFrame) { lightDown = true; heavyHold = -1f; }
            }
            if (heavy) Attack(true); else if (lightDown) Attack(false);
        }

        /// <summary>Set by the on-screen controls so a click on a button is not also read as an attack.</summary>
        public static bool PointerOverUi;

        public void Dodge()
        {
            if (Stamina < 22f || Time.time < dodgeUntil + 0.25f) return;
            Stamina -= 22f; lastStamUse = Time.time; dodgeUntil = Time.time + 0.3f; Blocking = false;
            Vector3 d = player.MoveDirection.sqrMagnitude > 0.01f ? player.MoveDirection : -player.transform.forward;
            player.Push(d.normalized * 11f, 0.3f);
            AudioManager.Instance?.PlaySFX("sfx_dodge");
            // perfect dodge: an attack was about to land
            foreach (var e in new[] { Bhaskar, Kishore }) if (e != null && e.State == EnemyState.Telegraph && e.TelegraphLeft < 0.15f) { StartCoroutine(SlowMo(0.4f)); Say("dodge"); }
        }

        public void Attack(bool heavy)
        {
            if (heavy && Stamina < 20f) heavy = false;
            if (heavy) { Stamina -= 20f; lastStamUse = Time.time; }
            float wind = heavy ? 0.45f : 0.18f;
            attackUntil = Time.time + wind + (heavy ? 0.3f : 0.12f);
            var t = Nearest(3.0f, 80f);
            if (t != null) player.FaceTowards(t.transform.position); // soft lock-on
            player.Push(player.transform.forward * (heavy ? 3.5f : 2.2f), 0.12f);
            AudioManager.Instance?.PlaySFX(heavy ? "sfx_swish_wide" : "sfx_swish_quick", 0.6f, heavy ? 0.8f : 1.2f);
            if (heavy) AudioManager.Instance?.PlaySFX("sfx_grunt_aryan", 0.7f);
            StartCoroutine(ResolveAttack(heavy, wind));
        }

        private IEnumerator ResolveAttack(bool heavy, float wind)
        {
            yield return new WaitForSeconds(wind);
            var t = Nearest(2.6f, 75f);
            if (t == null) yield break;
            if (Time.time > comboUntil) combo = 0;
            combo = heavy ? 0 : combo + 1; comboUntil = Time.time + 0.9f;
            bool open = t.State == EnemyState.Stunned || t.State == EnemyState.Recover || t.State == EnemyState.Staggered;
            float dmg = heavy ? 16f : 6f; if (open) dmg *= 1.5f;
            bool third = combo >= 3; if (third) combo = 0;
            HitsLanded++; LastHitMarker = Time.unscaledTime;
            Vector3 hp = t.transform.position + Vector3.up * 1.25f;
            Fx.Burst("Spark", hp, heavy ? 14 : 7, 3f, 0.07f, 0.3f, new Color(1f, 0.9f, 0.6f));
            ScreenText.WorldLabel("hit", hp + Vector3.up * 0.3f, heavy ? "HEAVY HIT" : third ? "COMBO" : "HIT", new Color(1f, 0.9f, 0.6f));
            AudioManager.Instance?.PlaySFX(heavy ? "sfx_hit_heavy" : "sfx_hit_light");
            if (heavy) StartCoroutine(HitStop(0.06f));
            bool wet = WeatherManager.Instance != null && WeatherManager.Instance.Intensity > 0.5f && heavy;
            if (t.TakeGuard(dmg, third ? 0.5f : wet ? 0.3f : 0f)) OnStaggered(t);
        }

        private void OnStaggered(EnemyBrain e) { Say("stagger"); AudioManager.Instance?.PlaySFX("sfx_stagger"); }

        /// <summary>An enemy attack reaches its hit frame.</summary>
        private void AttackLands(EnemyBrain e, Move m)
        {
            Vector3 to = Flat(player.transform.position - e.transform.position); float d = to.magnitude;
            float reach = m == Move.Lunge ? 2.2f : m == Move.WideSwing ? 3.0f : 2.4f;
            if (d > reach || Vector3.Angle(e.transform.forward, to) > (m == Move.WideSwing ? 110f : 70f)) { if (m == Move.Lunge) LungeMiss(e); return; }
            if (Invulnerable) { if (m == Move.Lunge) LungeMiss(e); return; }
            bool blade = m != Move.Kick && m != Move.Shove && e.hasBlade || m == Move.WildSwing;
            if (Blocking && !e.Unblockable)
            {
                if (Time.time - blockStart <= GameOptions.CounterWindow)
                {   // counter
                    Counters++; StartCoroutine(HitStop(0.08f)); Say("counter");
                    Fx.Burst("Spark", e.transform.position + Vector3.up * 1.3f, 18, 4f, 0.08f, 0.35f, Color.white);
                    AudioManager.Instance?.PlaySFX("sfx_blade_block");
                    ScreenText.WorldLabel("hit", e.transform.position + Vector3.up * 1.6f, "COUNTER!", new Color(0.56f, 0.89f, 0.66f));
                    if (e.TakeGuard(20f, 1.5f)) OnStaggered(e);
                    return;
                }
                Stamina -= 15f; lastStamUse = Time.time;
                Fx.Burst("Spark", player.transform.position + Vector3.up * 1.2f + player.transform.forward * 0.4f, 8, 3f, 0.06f, 0.25f, Color.white);
                AudioManager.Instance?.PlaySFX(blade ? "sfx_blade_block" : "sfx_block");
                if (Stamina <= 0f) { Stamina = 0f; Blocking = false; attackUntil = Time.time + 1f; SetBanner("GUARD BROKEN", 1.2f); }
                if (blade) Hurt(EnemyBrain.Damage(m) * 0.3f, e.transform.position);
                if (m == Move.Kick) player.Push(to.normalized * 5f, 0.2f);
                return;
            }
            Hurt(EnemyBrain.Damage(m), e.transform.position);
            if (m == Move.Kick || m == Move.Shove) player.Push(to.normalized * 5f, 0.2f);
        }

        private void LungeMiss(EnemyBrain e)
        {   // a missed lunge that ends beside a lit brazier makes him stumble into it
            for (int i = 0; i < braziers.Count; i++)
                if (!braziers[i].used && Flat(braziers[i].t.position - e.transform.position).magnitude < 2.2f) { KnockBrazier(i, true); Say("dodge", true); return; }
        }

        public void Hurt(float dmg, Vector3 from)
        {
            dmg *= GameOptions.DamageTaken;
            Health = Mathf.Max(0f, Health - dmg); HitsTaken++;
            LastDamageTime = Time.time; LastDamageDir = Flat(from - player.transform.position).normalized;
            AudioManager.Instance?.PlaySFX("sfx_hit_taken"); AudioManager.Instance?.PlaySFX("sfx_grunt_pain", 0.6f);
            if (Health <= 0f)
            {
                CombatState.InCombat = false; player.SpeedScale = 1f;
                DawnClock.Instance?.Fail("Bhaskar overpowered you. Block the white glint, dodge the red one.");
            }
        }

        private void OnFlash(Vector3 origin, Vector3 fwd)
        {
            if (Phase == 0) return;
            foreach (var e in new[] { Bhaskar, Kishore })
            {
                if (e == null) continue;
                Vector3 to = Flat(e.transform.position - origin);
                if (to.magnitude < 8f && Vector3.Angle(Flat(fwd), to) < 40f) { e.Stun(Phase == 3 ? 1.2f : 2f); Fx.Burst("Spark", e.transform.position + Vector3.up * 1.6f, 10, 2f, 0.1f, 0.4f, Color.white); }
            }
        }

        public void KnockBrazier(int i, bool byLunge = false)
        {
            if (i < 0 || i >= braziers.Count || braziers[i].used) return;
            var b = braziers[i].t; braziers[i] = (b, true);
            Fx.Burst("Ember", b.position + Vector3.up * 0.9f, 60, 5f, 0.09f, 1.4f, new Color(1f, 0.6f, 0.2f), 0.8f);
            AudioManager.Instance?.PlaySFX("sfx_brazier_crash");
            b.rotation = Quaternion.Euler(70f, Random.Range(0f, 360f), 0f); b.position += Vector3.down * 0.25f;
            var l = b.GetComponentInChildren<Light>(); if (l != null) l.intensity = 0.6f;
            foreach (var e in new[] { Bhaskar, Kishore })
                if (e != null && Flat(e.transform.position - b.position).magnitude < 3.2f) { if (e.TakeGuard(25f, 1.5f)) OnStaggered(e); if (!byLunge) Say("stagger"); }
        }

        public void PullRope()
        {
            if (ropeUsed || poles == null) return;
            ropeUsed = true; StartCoroutine(DropPoles());
        }

        private IEnumerator DropPoles()
        {
            AudioManager.Instance?.PlaySFX("sfx_rope");
            Vector3 a = poles.position, b = Fx.Ground(arena) + Vector3.up * 0.2f;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.35f) { poles.position = Vector3.Lerp(a, b, t * t); yield return null; }
            poles.position = b; poles.rotation = Quaternion.Euler(0f, 30f, 8f);
            Fx.Burst("Dust", b + Vector3.up * 0.3f, 40, 3f, 0.5f, 1.2f, new Color(0.7f, 0.65f, 0.55f, 0.6f));
            AudioManager.Instance?.PlaySFX("sfx_pole_crash");
            foreach (var e in new[] { Bhaskar, Kishore })
                if (e != null && Flat(e.transform.position - b).magnitude < 2.4f) { if (e.TakeGuard(40f, 2f)) OnStaggered(e); }
        }

        private IEnumerator HitStop(float seconds) { Time.timeScale = 0.05f; yield return new WaitForSecondsRealtime(seconds); Time.timeScale = 1f; }
        private IEnumerator SlowMo(float seconds) { Time.timeScale = 0.35f; yield return new WaitForSecondsRealtime(seconds); Time.timeScale = 1f; }

        // ------------------------------------------------------------------ phase 3: pursuit and grapple
        private void UpdatePursuit()
        {
            var b = Bhaskar.transform; Vector3 to = Flat(b.position - player.transform.position); float d = to.magnitude;
            if (!Grappling)
            {
                if (Bhaskar.State != EnemyState.Stunned)
                {
                    if (Bhaskar.State != EnemyState.Flee) Bhaskar.SetState(EnemyState.Flee);
                    // he runs laps round the brazier court (never through a wall or off the map), so he can always be cut off
                    float sp = 3.7f; const float lap = 6f; Vector3 rel = Flat(b.position - arena); if (rel.sqrMagnitude < 0.01f) rel = dir;
                    float ang = Mathf.Atan2(rel.x, rel.z) + sp / lap * Time.deltaTime * 1.5f;
                    Vector3 goal = arena + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * lap;
                    Vector3 np = Vector3.MoveTowards(b.position, new Vector3(goal.x, b.position.y, goal.z), sp * Time.deltaTime); Vector3 step = Flat(np - b.position);
                    np.y = Mathf.Clamp(Fx.Floor(new Vector3(np.x, arena.y + 0.3f, np.z), b), arena.y - 1f, arena.y + 1f); b.position = np;
                    if (step.sqrMagnitude > 0.000001f) b.rotation = Quaternion.Slerp(b.rotation, Quaternion.LookRotation(step), Time.deltaTime * 8f);
                    fleeDist += sp * Time.deltaTime;
                }
                stoneT -= Time.deltaTime;
                if (stoneT <= 0f && d > 3f && d < 16f) { stoneT = 4.5f; StartCoroutine(ThrowStone(b.position + Vector3.up * 1.4f)); }
                ScreenText.Panel("pursuit", null, $"<b>CATCH HIM</b>  {d:0} m ahead   Ford in {Mathf.Max(0f, 55f - fleeDist):0} m\n{ScreenText.Key("Shift")} Sprint   sidestep the stones   {ScreenText.Key("G")} Flash", PanelAnchor.TopRight, 640f, null, false, 4);
                if (d < 1.9f) { Grappling = true; Tug = 0.3f; ringStage = false; tugKey = 0; AudioManager.Instance?.PlaySFX("sfx_cloth"); }
                else if (d > 20f || fleeDist > 55f) Phase3Failed(d > 20f ? "He got too far ahead." : "Bhaskar reached the ford.");
                return;
            }
            // grapple: tug of war on the satchel strap, then one timed twist
            player.SpeedScale = 0f;
            var kb = Keyboard.current;
            if (!ringStage)
            {
                Tug -= Time.deltaTime * (0.16f + 0.05f * GameOptions.Difficulty);
                if (kb != null) { if (kb.aKey.wasPressedThisFrame) TugPush(1); if (kb.dKey.wasPressedThisFrame) TugPush(2); }
                if (VirtualInput.Down("attack")) TugPush(tugKey == 1 ? 2 : 1);
                if (Tug <= 0f) { Grappling = false; fleeDist = Mathf.Max(0f, fleeDist - 6f); Bhaskar.Stun(0.2f); SetBanner("HE PULLED FREE", 1.5f); b.position += dir * 2.5f; return; }
                ScreenText.Panel("grapple", "THE SATCHEL STRAP", $"Pull!  {Fx.Bar(Tug)}\nAlternate {ScreenText.Key("A")} and {ScreenText.Key("D")}", PanelAnchor.BottomCenter, 900f, null, false, 40);
                if (Tug >= 1f) { ringStage = true; ringPos = 0f; ringDir = 1f; }
            }
            else
            {
                ringPos += ringDir * (1.1f + 0.2f * GameOptions.Difficulty) * Time.deltaTime;
                if (ringPos > 1f) { ringPos = 1f; ringDir = -1f; } else if (ringPos < 0f) { ringPos = 0f; ringDir = 1f; }
                const int W = 30; var sb = new System.Text.StringBuilder("<mspace=0.55em>");
                int mark = Mathf.Clamp(Mathf.RoundToInt(ringPos * (W - 1)), 0, W - 1);
                for (int i = 0; i < W; i++) sb.Append(i == mark ? "<color=#FFFFFF><b>V</b></color>" : Mathf.Abs((i + 0.5f) / W - 0.5f) <= 0.14f ? $"<color={ScreenText.Green}>=</color>" : "-");
                sb.Append("</mspace>\nTwist the strap when the mark is in the green zone");
                ScreenText.Panel("grapple", "BRING HIM DOWN", sb.ToString(), PanelAnchor.BottomCenter, 900f, ScreenText.Key("E") + " Twist", false, 40);
                if ((kb != null && kb.eKey.wasPressedThisFrame) || VirtualInput.Down("interact")) GrappleTwist(null);
            }
        }

        public void TugPush(int key) { if (!Grappling || ringStage || key == tugKey) return; tugKey = key; Tug = Mathf.Min(1f, Tug + 0.085f); }

        public void GrappleTwist(float? at)
        {
            if (!Grappling) return;
            if (!ringStage && at == null) return;
            float pos = at ?? ringPos;
            if (Mathf.Abs(pos - 0.5f) <= 0.14f) { Grappling = false; routine = StartCoroutine(Victory()); }
            else { Grappling = false; ringStage = false; fleeDist = Mathf.Max(0f, fleeDist - 8f); SetBanner("HE BROKE FREE", 1.5f); Bhaskar.transform.position += dir * 3f; }
        }

        private IEnumerator ThrowStone(Vector3 from)
        {
            var s = Fx.Prim(PrimitiveType.Sphere, "ThrownStone", root, from, Vector3.one * 0.22f, Fx.Lit(new Color(0.4f, 0.37f, 0.33f)));
            Vector3 aim = player.transform.position + Vector3.up * 1.0f; Vector3 v = (aim - from).normalized * 14f;
            AudioManager.Instance?.PlaySFX("sfx_swish_quick", 0.5f, 0.7f);
            for (float t = 0; t < 1.6f && s != null; t += Time.deltaTime)
            {
                s.transform.position += v * Time.deltaTime;
                if ((s.transform.position - (player.transform.position + Vector3.up)).magnitude < 0.7f && !Invulnerable)
                { Hurt(8f, from); player.Stumble(1f); SetBanner("STUMBLED", 1f); break; }
                yield return null;
            }
            if (s != null) Destroy(s);
        }

        private void Phase3Failed(string why)
        {
            Phase3Fails++;
            if (Phase3Fails >= 3) { routine = StartCoroutine(OfferLostDawn(why)); return; }
            SetBanner(why + " Again.", 2f);
            player.Teleport(Fx.Ground(arena - dir * 2f), Quaternion.LookRotation(dir));
            Bhaskar.transform.position = Fx.Ground(arena + dir * 3f);
            StartPhase(3);
        }

        private IEnumerator OfferLostDawn(string why)
        {
            Phase = 0; int pick = -1;
            while (pick < 0)
            {
                ScreenText.Panel("lost", "HE IS GETTING AWAY", $"{why}\n\n{ScreenText.Key("1")} Try the pursuit again\n{ScreenText.Key("2")} Let him go (ending: LOST DAWN)", PanelAnchor.Center, 1000f, null, true, 80);
                var kb = Keyboard.current;
                if (kb != null && kb.digit1Key.wasPressedThisFrame) pick = 0;
                if (kb != null && kb.digit2Key.wasPressedThisFrame) pick = 1;
                yield return null;
            }
            if (pick == 0) { Phase3Fails = 0; Phase3Failed("Once more."); yield break; }
            StoryFlags.Set("lost_dawn", 1);
            CombatState.InCombat = false; SavedPhase = 0; player.SpeedScale = 1f;
            story.CompleteMinigame("fight");
            while (story.Current != null && story.Current.id == "obj_c5_recover") { story.DebugCompleteCurrent(); yield return null; }
        }

        private IEnumerator Victory()
        {
            Phase = 0; player.SpeedScale = 1f;
            StartCoroutine(SlowMo(0.8f));
            Bhaskar.SetState(EnemyState.Down);
            SetBanner("BHASKAR IS SUBDUED", 3f);
            AudioManager.Instance?.PlaySFX("sfx_body_fall");
            Fx.Burst("Dust", Bhaskar.transform.position + Vector3.up * 0.5f, 24, 2f, 0.4f, 1f, new Color(0.7f, 0.65f, 0.55f, 0.6f));
            WeatherManager.Instance?.SetTarget(0.35f, false);
            yield return new WaitForSeconds(1.6f);
            // the satchel falls open: the pieces lie beside him
            Vector3 right = Vector3.Cross(Vector3.up, dir); int k = 0;
            foreach (var c in FindObjectsByType<ClueInspectable>(FindObjectsInactive.Include))
            {
                if (c.Id != "relic_pradip" && c.Id != "relic_rosette" && !(c.Id == "relic_manuscript" && !story.HasClue("relic_manuscript"))) continue;
                c.gameObject.SetActive(true);
                Vector3 rp = Bhaskar.transform.position - dir * 1.4f + right * (k - 1) * 1.1f; rp.y = Mathf.Clamp(Fx.Floor(new Vector3(rp.x, arena.y + 0.5f, rp.z), Bhaskar.transform), arena.y - 1f, arena.y + 1f) + 0.3f; c.transform.position = rp; k++;
            }
            CombatState.InCombat = false; SavedPhase = 0;
            story.PlayConversation(StoryData.Conversation("conv_c5_after"), null);
            story.CompleteMinigame("fight");
        }

        // ---- test / bot hooks ----
        public void DebugSkipIntro() { if (routine != null) StopCoroutine(routine); intro = false; }
        public void DebugSetHealth(float h) { Health = h; }
        public void DebugGrapple() { Grappling = true; ringStage = true; Tug = 1f; }
    }
}
