using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Small helpers for runtime-built props and effects (primitives + the FX materials in Resources/FX).</summary>
    public static class Fx
    {
        private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        public static Material Lit(Color c, float smooth = 0.3f, float metal = 0f, Color? emission = null)
        {
            string key = c + "|" + smooth + "|" + metal + "|" + emission;
            if (mats.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            mats[key] = m; return m;
        }
        public static Material Res(string name) => Resources.Load<Material>("FX/" + name);

        public static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = false)
        {
            var g = GameObject.CreatePrimitive(t); g.name = name;
            if (!collider) Object.Destroy(g.GetComponent<Collider>());
            if (parent != null) g.transform.SetParent(parent, false);
            g.transform.position = pos; g.transform.localScale = scale;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            return g;
        }

        public static Vector3 Ground(Vector3 p, float up = 6f)
        {
            if (Physics.Raycast(p + Vector3.up * up, Vector3.down, out RaycastHit h, up + 12f, ~0, QueryTriggerInteraction.Ignore)) return h.point;
            return p;
        }

        /// <summary>One-shot particle burst (sparks, embers, dust).</summary>
        public static void Burst(string mat, Vector3 pos, int count, float speed, float size, float life, Color color, float gravity = 0.6f)
        {
            var go = new GameObject("FxBurst"); go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed); main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color; main.gravityModifier = gravity; main.maxParticles = count; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Res(mat); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Emit(count);
            Object.Destroy(go, life + 0.5f);
        }

        public static string Bar(float v, int width = 24) { int n = Mathf.Clamp(Mathf.RoundToInt(v * width), 0, width); return "<mspace=0.55em>[<color=" + ScreenText.Gold + ">" + new string('|', n) + "</color>" + new string('.', width - n) + "]</mspace>"; }
    }

    /// <summary>A prop the player can use with E (lamp, trap, brazier...).</summary>
    public sealed class UseProp : MonoBehaviour, IInteractable
    {
        public string id = "prop", prompt = "Use"; public float radius = 2.2f; public System.Action onUse; public System.Func<bool> usable;
        public string Id => id;
        public string Prompt => prompt;
        public Transform Anchor => transform;
        public float Radius => usable == null || usable() ? radius : 0f;
        public void Interact() { if (usable == null || usable()) onUse?.Invoke(); }
        public static UseProp Add(GameObject go, string id, string prompt, float radius, System.Action onUse, System.Func<bool> usable = null)
        {
            if (go.GetComponent<Collider>() == null) { var c = go.AddComponent<SphereCollider>(); c.isTrigger = true; c.radius = 0.6f / Mathf.Max(0.05f, go.transform.lossyScale.x); }
            var u = go.AddComponent<UseProp>(); u.id = id; u.prompt = prompt; u.radius = radius; u.onUse = onUse; u.usable = usable; return u;
        }
    }

    /// <summary>
    /// Builds each chapter's gameplay in the existing scenes at runtime (no scene edits):
    /// Ch1 chase and token, Ch2 battery cells / lamp relight / deduction board, Ch3 tripwire bell traps,
    /// Ch4 hooks for the timed Sun Wall. Chapter 5 lives in Combat*.cs.
    /// </summary>
    public sealed class ChapterDirector : MonoBehaviour
    {
        public static ChapterDirector Instance { get; private set; }
        private StoryDialogueSystem story; private PlayerController player; private string scene; private Transform root;

        // ---- chapter 1: chase ----
        public bool ChaseRunning { get; private set; }
        public float ChaseDistance { get; private set; }
        private Transform runner; private readonly List<Vector3> path = new List<Vector3>(); private int seg; private float lostTimer, chaseTime; private bool barked;
        // ---- chapter 2 ----
        public static readonly string[] LampNames = { "Sunrise", "Morning", "Noon", "Afternoon", "Dusk" };
        public int LampsLit { get; private set; }
        private readonly List<(GameObject go, Light light, int order)> lamps = new List<(GameObject, Light, int)>();
        public bool BoardOpen { get; private set; }
        public int BoardStep { get; private set; }
        private string boardFeedback = "";
        private static readonly string[] BoardClue = { "The wax-cut key", "The oily handprint, low on the stand", "The chalk mark on the pillar" };
        private static readonly string[][] BoardOpt = {
            new[] { "The lock was picked by an expert", "Someone inside let a copy be made", "The key was stolen that same night" },
            new[] { "A careful carrier who knelt to lift", "A struggle at the stands", "A child's hand" },
            new[] { "A mason's old tally", "A guide left for a man who did not know the hall", "A warning to the priest" } };
        private static readonly int[] BoardAnswer = { 1, 0, 1 };
        // ---- chapter 3 ----
        public sealed class Trap { public Vector3 a, b, mid; public GameObject go; public bool disarmed; public float cooldown; }
        public readonly List<Trap> Traps = new List<Trap>();
        public Trap ActiveTrap { get; private set; }
        public int TrapHits { get; private set; }
        public int BellsRung { get; private set; }
        private float ringPos, ringDir = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("ChapterDirector"); DontDestroyOnLoad(go);
            Instance = go.AddComponent<ChapterDirector>();
        }

        private void Awake() { SceneManager.sceneLoaded += (s, m) => { scene = s.name; built = false; }; scene = SceneManager.GetActiveScene().name; }

        private void Start()
        {
            story = StoryDialogueSystem.Instance;
            foreach (string id in new[] { "chase_c1", "lamps", "deduction", "traps" }) story?.RegisterMinigame(id);
        }

        private bool built;
        private void Build()
        {
            built = true;
            ChaseRunning = false; runner = null; lamps.Clear(); LampsLit = 0; Traps.Clear(); ActiveTrap = null; BoardOpen = false; BoardStep = 0;
            root = new GameObject("ChapterProps").transform;
            Vector3 p = player.transform.position; Vector3 f = player.transform.forward;
            if (scene == Constants.Sanctum) BuildHall(p, f);
            if (scene == Constants.ShrineCrime) BuildTraps(p, f);
            if (scene == Constants.Courtyard) BuildCourtyard();
            ApplyWeather();
        }

        private void ApplyWeather()
        {
            var w = WeatherManager.Instance; if (w == null || story == null) return;
            var ch = story.Chapter;
            LookTuner.Dawn = ch != null && ch.id == "ch6" ? 1f : 0f;
            if (ch != null && ch.id == "ch6") w.SetTarget(0f, false, true);          // the storm has cleared at first light
            else if (scene == Constants.JunglePath) w.SetTarget(0.65f, true, true);
        }

        private bool Is(string minigame) => story != null && story.Current != null && story.Current.complete.id == minigame && story.Current.scene == scene;

        private void Update()
        {
            if (story == null) story = StoryDialogueSystem.Instance;
            if (story == null || SceneLoader.IsLoading) return;
            bool gameplay = scene == Constants.Courtyard || scene == Constants.Sanctum || scene == Constants.ShrineCrime || scene == Constants.PuzzleWall || scene == Constants.JunglePath;
            if (!gameplay) return;
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            if (!built) { if (Time.timeSinceLevelLoad < 0.8f) return; Build(); }
            if (scene == Constants.JunglePath && DawnClock.Instance != null) LookTuner.DawnProgress = Mathf.InverseLerp(115f, 155f, DawnClock.Instance.Minutes);
            if (scene == Constants.Courtyard) UpdateChase();
            if (scene == Constants.Sanctum) { UpdateCells(); UpdateBoard(); UpdateMeera(Constants.Sanctum, "obj_c2_pujari"); }
            if (scene == Constants.ShrineCrime) { UpdateTraps(); if (StoryFlags.Get("meera_out") == 0) UpdateMeera(Constants.ShrineCrime, "obj_c3_pujari"); }
        }

        // ------------------------------------------------------------------ Meera walks in for her scenes
        private bool meeraSpawned;
        private void UpdateMeera(string inScene, string objective)
        {
            if (meeraSpawned || story.Current == null || story.Current.id != objective || CharacterManager.Instance == null) return;
            var priest = CharacterManager.Instance.GetNpc(Constants.Priest);
            if (priest == null) return;
            meeraSpawned = true;
            Vector3 pos = Fx.Ground(priest.transform.position + priest.transform.right * 1.6f + priest.transform.forward * 0.4f);
            var m = CharacterManager.Instance.SpawnNpc(Constants.Villager, pos, priest.transform.rotation);
            if (m != null && m.GetComponent<NPCDialogueTrigger>() == null) m.AddComponent<NPCDialogueTrigger>().Setup(Constants.Villager, "Meera", null);
        }

        // ------------------------------------------------------------------ chapter 1: the runner
        private void BuildCourtyard()
        {
            meeraSpawned = false;
            if (story.HasClue("clue_token17") || !(Is("chase_c1") || (story.Current != null && story.Current.id == "obj_c1_token"))) return;
            if (story.Current.id == "obj_c1_token") SpawnToken(Fx.Ground(player.transform.position + player.transform.forward * 3f));
        }

        private void StartChase()
        {
            ChaseRunning = true; seg = 0; lostTimer = 0f; chaseTime = 0f; barked = false;
            path.Clear();
            Vector3 c = player.transform.position; float baseY = c.y;
            for (int i = 0; i <= 20; i++)
            {   // one and a quarter laps of the courtyard, pulled inward wherever a wall or prop is in the way
                float ang = (i / 16f) * Mathf.PI * 2f; float r = 14f;
                Vector3 q = c;
                for (int k = 0; k < 8; k++, r -= 1.3f)
                {
                    q = c + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r;
                    Vector3 g = Fx.Ground(q);
                    if (Mathf.Abs(g.y - baseY) < 0.6f && !Physics.CheckSphere(g + Vector3.up * 1f, 0.45f, ~0, QueryTriggerInteraction.Ignore)) { q = g; break; }
                }
                path.Add(q);
            }
            GameObject go = null;
            if (CharacterManager.Instance != null) go = CharacterManager.Instance.SpawnNpc(Constants.Robber2, path[0], Quaternion.identity);
            if (go == null) go = Fx.Prim(PrimitiveType.Capsule, "Runner", null, path[0] + Vector3.up, new Vector3(0.6f, 0.9f, 0.6f), Fx.Lit(new Color(0.05f, 0.05f, 0.06f)));
            go.name = "Marker_Runner";
            foreach (var t in go.GetComponents<NPCDialogueTrigger>()) Destroy(t);
            foreach (var t in go.GetComponentsInChildren<NPCHeadIndicator>()) Destroy(t);
            foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = false;
            if (go.GetComponent<NpcAnimDriver>() == null) go.AddComponent<NpcAnimDriver>();
            runner = go.transform;
        }

        private void UpdateChase()
        {
            if (!Is("chase_c1")) { if (runner != null && !ChaseRunning) { } return; }
            if (!ChaseRunning) { if (InputGate.UiOpen != null && InputGate.UiOpen()) return; StartChase(); return; }
            if (runner == null || DawnClock.Instance == null || DawnClock.Instance.OutOfTime) return;
            chaseTime += Time.deltaTime;
            Vector3 to = player.transform.position - runner.position; to.y = 0f;
            ChaseDistance = to.magnitude;
            float speed = Mathf.Lerp(4.7f, 3.2f, Mathf.InverseLerp(5f, 16f, ChaseDistance)); // rubber band: he stays just ahead
            Vector3 target = path[Mathf.Min(seg + 1, path.Count - 1)];
            Vector3 d = target - runner.position; d.y = 0f;
            if (d.magnitude < 0.6f) { seg++; if (seg >= path.Count - 1) { EndChase(); return; } }
            else
            {
                Vector3 np = runner.position + d.normalized * speed * Time.deltaTime;
                np.y = Fx.Ground(np, 2f).y;
                runner.position = np;
                runner.rotation = Quaternion.Slerp(runner.rotation, Quaternion.LookRotation(d.normalized), Time.deltaTime * 8f);
            }
            if (StoryFlags.Get("chase_hint") == 1 && chaseTime < 6f) ScreenText.WorldLabel("runner", runner.position + Vector3.up * 2.2f, "THE RUNNER", new Color(1f, 0.78f, 0.34f));
            if (!barked && chaseTime > 8f) { barked = true; story.Say("chase_mid"); }
            lostTimer = ChaseDistance > 25f ? lostTimer + Time.deltaTime : 0f;
            string col = ChaseDistance > 18f ? ScreenText.Coral : ScreenText.Green;
            ScreenText.Panel("chase", null, $"<b>CHASE</b>  He is <color={col}>{ChaseDistance:0} m</color> ahead. Do not let him get 25 m away.\n{ScreenText.Key("Shift")} Sprint", PanelAnchor.TopRight, 600f, null, false, 4);
            if (lostTimer > 1.5f) { ChaseRunning = false; Destroy(runner.gameObject); DawnClock.Instance.Penalty(2f, "lost the runner"); DawnClock.Instance.Fail("He is gone. The runner got away over the wall."); }
        }

        /// <summary>Bot / test hook: finish the chase now (drops the token).</summary>
        public void DebugEndChase() { if (ChaseRunning && runner != null) EndChase(); }

        private void EndChase()
        {
            ChaseRunning = false;
            SpawnToken(Fx.Ground(runner.position - runner.forward * 1.2f));
            Destroy(runner.gameObject, 0.1f);
            story.CompleteMinigame("chase_c1");
        }

        private void SpawnToken(Vector3 pos)
        {
            if (GameObject.Find("Clue_Token17") != null) return;
            var t = Fx.Prim(PrimitiveType.Cylinder, "Clue_Token17", root, pos + Vector3.up * 0.06f, new Vector3(0.22f, 0.015f, 0.22f), Fx.Lit(new Color(0.75f, 0.74f, 0.7f), 0.7f, 0.9f, new Color(0.3f, 0.28f, 0.2f)), true);
            var c = t.AddComponent<ClueInspectable>();
            c.Setup("clue_token17", "Porter token No. 17", "Courtyard", "A tin porter's token stamped 17.", "The runner knew every stone of this courtyard.", 2.6f);
            var l = new GameObject("TokenGlint").AddComponent<Light>(); l.transform.SetParent(t.transform, false); l.transform.localPosition = Vector3.up * 20f;
            l.type = LightType.Point; l.range = 2.5f; l.intensity = 1.5f; l.color = new Color(1f, 0.9f, 0.6f);
        }

        // ------------------------------------------------------------------ chapter 2: cells, lamps, deduction
        private readonly List<GameObject> cells = new List<GameObject>();
        private void BuildHall(Vector3 p, Vector3 f)
        {
            cells.Clear(); meeraSpawned = false;
            Vector3 centre = p + f * 5f; Vector3 right = Vector3.Cross(Vector3.up, f);
            int[] order = { 2, 0, 4, 1, 3 }; // physical position -> place in the sun's order (so the row is not simply left to right)
            for (int i = 0; i < 5; i++)
            {
                float ang = Mathf.Lerp(-80f, 80f, i / 4f) * Mathf.Deg2Rad;
                Vector3 pos = centre + (f * Mathf.Cos(ang) + right * Mathf.Sin(ang)) * 5.5f;
                for (int k = 0; k < 6 && Physics.CheckSphere(Fx.Ground(pos) + Vector3.up, 0.4f, ~0, QueryTriggerInteraction.Ignore); k++) pos = Vector3.Lerp(pos, centre, 0.2f);
                pos = Fx.Ground(pos);
                var stand = Fx.Prim(PrimitiveType.Cylinder, "HallLamp_" + LampNames[order[i]], root, pos + Vector3.up * 0.6f, new Vector3(0.14f, 0.6f, 0.14f), Fx.Lit(new Color(0.72f, 0.54f, 0.18f), 0.7f, 0.9f), true);
                Fx.Prim(PrimitiveType.Sphere, "Bowl", stand.transform, pos + Vector3.up * 1.25f, new Vector3(0.34f, 0.16f, 0.34f), Fx.Lit(new Color(0.72f, 0.54f, 0.18f), 0.7f, 0.9f));
                var light = new GameObject("Flame").AddComponent<Light>(); light.transform.SetParent(stand.transform, false); light.transform.position = pos + Vector3.up * 1.5f;
                light.type = LightType.Point; light.range = 9f; light.intensity = 0f; light.color = new Color(1f, 0.7f, 0.35f); light.shadows = LightShadows.None;
                int ord = order[i]; var entry = (stand, light, ord); lamps.Add(entry);
                UseProp.Add(stand, "lamp_" + ord, "Light the " + LampNames[ord] + " lamp", 2.2f, () => LightLamp(ord), () => Is("lamps") && light.intensity <= 0f);
            }
            if (story.ConversationDone("mg:lamps")) foreach (var l in lamps) l.light.intensity = 2.2f;
            for (int i = 0; i < 2; i++)
            {
                Vector3 pos = Fx.Ground(centre + (i == 0 ? -right * 3f - f * 2f : right * 2.5f + f * 2.5f));
                cells.Add(Fx.Prim(PrimitiveType.Cube, "BatteryCell", root, pos + Vector3.up * 0.12f, new Vector3(0.1f, 0.2f, 0.1f), Fx.Lit(new Color(0.1f, 0.2f, 0.5f), 0.5f, 0.2f, new Color(0.2f, 0.6f, 1.6f))));
            }
            var marker = new GameObject("Marker_Lamps"); marker.transform.SetParent(root, false); marker.transform.position = centre;
        }

        public void LightLamp(int ord)
        {
            if (!Is("lamps")) return;
            if (ord == LampsLit)
            {
                foreach (var l in lamps) if (l.order == ord) { l.light.intensity = 2.2f; Fx.Burst("Ember", l.light.transform.position, 14, 1.2f, 0.06f, 0.8f, new Color(1f, 0.6f, 0.2f)); }
                LampsLit++;
                AudioManager.Instance?.PlaySFX("sfx_lamp_ignite");
                if (LampsLit >= 5) story.CompleteMinigame("lamps");
            }
            else
            {
                foreach (var l in lamps) l.light.intensity = 0f;
                LampsLit = 0;
                AudioManager.Instance?.PlaySFX("sfx_lamp_blown");
                DawnClock.Instance?.Penalty(2f, "wrong lamp, all blown out");
            }
        }

        private void UpdateCells()
        {
            for (int i = cells.Count - 1; i >= 0; i--)
            {
                if (cells[i] == null) { cells.RemoveAt(i); continue; }
                cells[i].transform.Rotate(0f, 60f * Time.deltaTime, 0f);
                if ((cells[i].transform.position - player.transform.position).sqrMagnitude < 1.6f * 1.6f)
                {
                    Flashlight.AddBattery(50f); AudioManager.Instance?.PlaySFX("sfx_battery");
                    Destroy(cells[i]); cells.RemoveAt(i);
                }
            }
            if (Is("lamps"))
            {
                foreach (var l in lamps) if ((l.go.transform.position - player.transform.position).sqrMagnitude < 36f)
                    ScreenText.WorldLabel("lamp" + l.order, l.go.transform.position + Vector3.up * 1.9f, LampNames[l.order] + (l.light.intensity > 0f ? " (lit)" : ""), l.light.intensity > 0f ? new Color(0.56f, 0.89f, 0.66f) : new Color(1f, 0.96f, 0.84f));
                ScreenText.Panel("lamps", null, $"<b>LAMPS</b> {LampsLit}/5 lit. East first, as the sun does.", PanelAnchor.TopRight, 560f, null, false, 5);
            }
        }

        public void BoardAnswerPick(int i)
        {
            if (!BoardOpen || BoardStep >= 3) return;
            if (i == BoardAnswer[BoardStep])
            {
                boardFeedback = $"<color={ScreenText.Green}>That fits.</color>"; BoardStep++;
                if (BoardStep >= 3) { BoardOpen = false; story.CompleteMinigame("deduction"); }
            }
            else { boardFeedback = $"<color={ScreenText.Coral}>That does not fit the evidence.</color>"; DawnClock.Instance?.Penalty(1f, "a wrong deduction"); }
        }
        public void OpenBoard() { if (Is("deduction")) { BoardOpen = true; boardFeedback = ""; } }

        private void UpdateBoard()
        {
            if (!Is("deduction")) { BoardOpen = false; return; }
            var kb = Keyboard.current;
            if ((kb != null && kb.bKey.wasPressedThisFrame) || VirtualInput.Down("board")) { BoardOpen = !BoardOpen; boardFeedback = ""; }
            if (!BoardOpen) { ScreenText.Panel("board_hint", null, $"Press {ScreenText.Key("B")} to open the deduction board.", PanelAnchor.TopRight, 560f, null, false, 4); return; }
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { EventBus.EscapeConsumedFrame = Time.frameCount; BoardOpen = false; return; }
                if (kb.digit1Key.wasPressedThisFrame) BoardAnswerPick(0); else if (kb.digit2Key.wasPressedThisFrame) BoardAnswerPick(1); else if (kb.digit3Key.wasPressedThisFrame) BoardAnswerPick(2);
            }
            if (!BoardOpen || BoardStep >= 3) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 3; i++)
                sb.Append(i < BoardStep ? $"<color={ScreenText.Green}>• {BoardClue[i]}  →  {BoardOpt[i][BoardAnswer[i]]}</color>\n" : i == BoardStep ? $"<color={ScreenText.Gold}><b>• {BoardClue[i]}  →  ?</b></color>\n" : $"<color={ScreenText.Grey}>• {BoardClue[i]}</color>\n");
            sb.Append($"\n<b>{BoardClue[BoardStep]}</b> proves:\n");
            for (int k = 0; k < 3; k++) sb.Append(ScreenText.Key((k + 1).ToString())).Append(' ').Append(BoardOpt[BoardStep][k]).Append('\n');
            if (boardFeedback.Length > 0) sb.Append('\n').Append(boardFeedback);
            ScreenText.Panel("board", $"DEDUCTION BOARD  {BoardStep + 1}/3", sb.ToString(), PanelAnchor.Center, 1200f, ScreenText.Key("1-3") + " Choose    " + ScreenText.Key("Esc") + " Close", true, 60);
        }

        // ------------------------------------------------------------------ chapter 3: tripwire bell traps
        private void BuildTraps(Vector3 p, Vector3 f)
        {
            meeraSpawned = false;
            if (story.ConversationDone("mg:traps")) return;
            Vector3 goal = p + f * 14f;
            if (CharacterManager.Instance != null) { var pr = CharacterManager.Instance.GetNpc(Constants.Priest); if (pr != null) goal = pr.transform.position; }
            Vector3 dir = goal - p; dir.y = 0f; if (dir.magnitude < 6f) dir = f * 12f;
            Vector3 right = Vector3.Cross(Vector3.up, dir.normalized);
            var threadMat = Fx.Lit(new Color(0.8f, 0.8f, 0.85f), 0.8f, 0.2f, new Color(0.25f, 0.25f, 0.3f));
            var brass = Fx.Lit(new Color(0.72f, 0.54f, 0.18f), 0.7f, 0.9f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 mid = Fx.Ground(p + dir * (0.28f + 0.2f * i));
                var t = new Trap { mid = mid, a = mid - right * 1.8f, b = mid + right * 1.8f };
                t.go = new GameObject(i == 0 ? "Marker_Trap" : "Trap_" + i); t.go.transform.SetParent(root, false); t.go.transform.position = mid + Vector3.up * 0.3f;
                var th = Fx.Prim(PrimitiveType.Cube, "Thread", t.go.transform, mid + Vector3.up * 0.28f, new Vector3(3.6f, 0.012f, 0.012f), threadMat);
                th.transform.rotation = Quaternion.LookRotation(dir.normalized);
                Fx.Prim(PrimitiveType.Cylinder, "PostA", t.go.transform, t.a + Vector3.up * 0.2f, new Vector3(0.05f, 0.2f, 0.05f), brass);
                Fx.Prim(PrimitiveType.Cylinder, "PostB", t.go.transform, t.b + Vector3.up * 0.2f, new Vector3(0.05f, 0.2f, 0.05f), brass);
                Fx.Prim(PrimitiveType.Sphere, "Bell", t.go.transform, t.b + Vector3.up * 0.34f, Vector3.one * 0.12f, brass);
                var trap = t;
                UseProp.Add(t.go, "trap_" + i, "Disarm the bell thread (crouch first)", 2.0f, () => BeginDisarm(trap), () => Is("traps") && !trap.disarmed && ActiveTrap == null);
                Traps.Add(t);
            }
        }

        public void BeginDisarm(Trap t, bool force = false)
        {
            if (!force && !player.IsCrouching) { toastUntil = Time.unscaledTime + 2.5f; return; }
            ActiveTrap = t; TrapHits = 0; ringPos = 0f; ringDir = 1f;
        }
        private float toastUntil;

        /// <summary>Press at the current marker position (tests can set the position first).</summary>
        public void DisarmPress(float? at = null)
        {
            if (ActiveTrap == null) return;
            float pos = at ?? ringPos; float half = GameOptions.Difficulty == 0 ? 0.16f : GameOptions.Difficulty == 1 ? 0.12f : 0.08f;
            if (Mathf.Abs(pos - 0.5f) <= half)
            {
                TrapHits++; AudioManager.Instance?.PlaySFX("sfx_ui_select");
                if (TrapHits >= 3)
                {
                    ActiveTrap.disarmed = true; Destroy(ActiveTrap.go.transform.Find("Thread").gameObject); ActiveTrap = null;
                    AudioManager.Instance?.PlaySFX("sfx_bell_muffled");
                    bool all = true; foreach (var t in Traps) if (!t.disarmed) all = false;
                    foreach (var t in Traps) if (!t.disarmed) { t.go.name = "Marker_Trap"; break; } else t.go.name = "Trap_done";
                    if (all) story.CompleteMinigame("traps");
                }
            }
            else { Ring(ActiveTrap); ActiveTrap = null; }
        }

        private void Ring(Trap t)
        {
            BellsRung++; t.cooldown = Time.time + 4f;
            AudioManager.Instance?.PlaySFX("sfx_bell_trip");
            DawnClock.Instance?.Penalty(3f, "a bell rang");
            story.Say("bell_rung");
        }

        private void UpdateTraps()
        {
            if (Traps.Count == 0) return;
            bool flash = Flashlight.On;
            foreach (var t in Traps)
            {
                if (t.disarmed) continue;
                var th = t.go.transform.Find("Thread");
                if (th != null) th.localScale = new Vector3(3.6f, flash ? 0.02f : 0.008f, flash ? 0.02f : 0.008f);
                if (ActiveTrap == t || Time.time < t.cooldown) continue;
                // walking through the thread rings the bell
                Vector3 pp = player.transform.position; Vector3 ab = t.b - t.a; float u = Mathf.Clamp01(Vector3.Dot(pp - t.a, ab) / ab.sqrMagnitude);
                Vector3 closest = t.a + ab * u; closest.y = pp.y;
                if ((pp - closest).magnitude < 0.3f && Is("traps")) Ring(t);
            }
            if (Time.unscaledTime < toastUntil) ScreenText.Panel("trap_toast", null, $"Crouch with {ScreenText.Key("C")} first: your hands must be level with the thread.", PanelAnchor.BottomCenter, 900f, null, false, 2);
            if (ActiveTrap == null) return;
            var kb = Keyboard.current;
            float speed = 0.9f + 0.35f * TrapHits + 0.2f * GameOptions.Difficulty;
            ringPos += ringDir * speed * Time.unscaledDeltaTime;
            if (ringPos > 1f) { ringPos = 1f; ringDir = -1f; } else if (ringPos < 0f) { ringPos = 0f; ringDir = 1f; }
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { EventBus.EscapeConsumedFrame = Time.frameCount; ActiveTrap = null; return; }
            if ((kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) || VirtualInput.Down("interact")) { DisarmPress(); if (ActiveTrap == null) return; }
            const int W = 30; float half = GameOptions.Difficulty == 0 ? 0.16f : GameOptions.Difficulty == 1 ? 0.12f : 0.08f;
            var sb = new System.Text.StringBuilder("<mspace=0.55em>");
            int mark = Mathf.Clamp(Mathf.RoundToInt(ringPos * (W - 1)), 0, W - 1);
            for (int i = 0; i < W; i++)
            {
                bool green = Mathf.Abs((i + 0.5f) / W - 0.5f) <= half;
                sb.Append(i == mark ? "<color=#FFFFFF><b>V</b></color>" : green ? $"<color={ScreenText.Green}>=</color>" : "-");
            }
            sb.Append("</mspace>\n").Append($"Stop the mark in the green zone. Loosened: {TrapHits}/3");
            ScreenText.Panel("trap", "DISARM THE BELL THREAD", sb.ToString(), PanelAnchor.Center, 1000f, ScreenText.Key("E") + " Press    " + ScreenText.Key("Esc") + " Let go", true, 60);
        }
    }
}
