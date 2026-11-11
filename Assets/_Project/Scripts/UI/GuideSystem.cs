using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// "? GUIDE" button (bottom-right, H / Tab / gamepad D-pad left). Opens a panel with the current goal,
    /// what to do (keys), where to go (area, direction, distance), missing clues and three progressive hints.
    /// "SHOW ME THE WAY" turns on a glowing beacon at the target, an on-screen edge arrow and a path line
    /// (NavMesh path when a NavMesh exists, otherwise a ground-following line). The button pulses after 60 s
    /// without progress. All content comes from Resources/Story/story.json via StoryDialogueSystem.
    /// Also draws the sun-wall puzzle panel and the ending screen.
    /// </summary>
    public sealed class GuideSystem : MonoBehaviour
    {
        public static GuideSystem Instance { get; private set; }
        public bool IsOpen { get; private set; }
        public bool WayShown { get; private set; }
        public int HintLevel { get; private set; }

        private Canvas canvas;
        private RectTransform buttonRt;
        private Image buttonBg;
        private TextMeshProUGUI buttonText;
        private RectTransform arrowRt;
        private GameObject beacon;
        private LineRenderer line;
        private string hintObjectiveId;
        private bool ending;
        private float endingTime;

        private static readonly HashSet<string> GameplayScenes = new HashSet<string> {
            Constants.Courtyard, Constants.Sanctum, Constants.ShrineCrime, Constants.PuzzleWall, Constants.JunglePath };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("GuideSystem");
            DontDestroyOnLoad(go);
            go.AddComponent<GuideSystem>();
        }

        private void Awake()
        {
            Instance = this;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // Guide button
            buttonBg = ScreenTextRenderer.MakePanel(transform, "GuideButton", out buttonRt);
            buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(1, 0); buttonRt.pivot = new Vector2(1, 0);
            buttonRt.anchoredPosition = new Vector2(-24, 16); buttonRt.sizeDelta = new Vector2(300, 84);
            var btn = buttonRt.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(Toggle);
            buttonText = ScreenTextRenderer.MakeText(buttonRt, "GuideButtonKeyHint", 26, ScreenTextRenderer.Gold, true, TextAlignmentOptions.Center);
            var trt = buttonText.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            buttonText.text = "? GUIDE";

            // Edge arrow (triangle sprite)
            var a = new GameObject("GuideArrow", typeof(RectTransform));
            a.transform.SetParent(transform, false);
            arrowRt = (RectTransform)a.transform; arrowRt.sizeDelta = new Vector2(72, 72);
            arrowRt.anchorMin = arrowRt.anchorMax = Vector2.zero;
            var img = a.AddComponent<Image>(); img.sprite = TriangleSprite(); img.color = ScreenTextRenderer.Gold; img.raycastTarget = false;
            a.AddComponent<Outline>().effectColor = Color.black;
            a.SetActive(false);

            EnsureEventSystem();
            SceneManager.sceneLoaded += (s, m) => { ClearWorldMarkers(); IsOpen = false; EnsureEventSystem(); };
            EventBus.OnGameState += st => { if (st == GameState.Ending) { ending = true; endingTime = Time.unscaledTime; } };
            StoryDialogueSystem.OnObjectiveChanged += o => { HintLevel = 0; if (WayShown && o == null) WayShown = false; };
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        public void Toggle() { IsOpen = !IsOpen; if (IsOpen) WayShown = true; }
        public void RevealHint(int level)
        {
            var o = StoryDialogueSystem.Instance?.Current;
            if (o == null) return;
            if (level > HintLevel) { HintLevel = Mathf.Clamp(level, 0, 3); StoryDialogueSystem.Instance.RegisterHintUsed(); }
        }
        public void ShowTheWay(bool on) { WayShown = on; if (!on) ClearWorldMarkers(); }

        private void Update()
        {
            string scene = SceneManager.GetActiveScene().name;
            bool gameplay = GameplayScenes.Contains(scene) && !EventBus.IsPhotoModeActive && !EventBus.IsShowcaseActive;
            var story = StoryDialogueSystem.Instance;

            if (ending && story != null) { DrawEnding(story); buttonRt.gameObject.SetActive(false); arrowRt.gameObject.SetActive(false); ClearWorldMarkers(); return; }

            buttonRt.gameObject.SetActive(gameplay);
            if (!gameplay || story == null) { arrowRt.gameObject.SetActive(false); return; }

            var kb = Keyboard.current; var gp = Gamepad.current;
            if ((kb != null && (kb.hKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)) || (gp != null && gp.dpad.left.wasPressedThisFrame)) Toggle();
            if (IsOpen && kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) RevealHint(1);
                if (kb.digit2Key.wasPressedThisFrame) RevealHint(2);
                if (kb.digit3Key.wasPressedThisFrame) RevealHint(3);
                if (kb.enterKey.wasPressedThisFrame || kb.gKey.wasPressedThisFrame) ShowTheWay(!WayShown);
            }
            if (IsOpen && gp != null && gp.buttonNorth.wasPressedThisFrame) RevealHint(HintLevel + 1);
            if (IsOpen && gp != null && gp.buttonSouth.wasPressedThisFrame) ShowTheWay(!WayShown);

            // Pulse after 60 s without progress
            float pulse = story.IdleTime > 60f ? 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 5f) : 1f;
            buttonRt.localScale = Vector3.one * pulse;
            buttonText.text = story.IdleTime > 60f ? "? GUIDE (H)" : "? GUIDE";
            buttonRt.sizeDelta = new Vector2(Mathf.Ceil(buttonText.GetPreferredValues(buttonText.text, 2000f, 0f).x) + 36f, 56f * ScreenTextRenderer.Scale);
            buttonText.fontSize = 26 * ScreenTextRenderer.Scale;

            var player = FindAnyObjectByType<PlayerController>();
            Transform target = player != null ? story.ResolveTarget(player.transform.position) : null;

            bool talking = UIManager.Instance != null && UIManager.Instance.IsDialogueActive;
            if (talking) IsOpen = false; // never draw the guide on top of a conversation
            if (IsOpen) DrawPanel(story, player, target);
            if (WayShown && !IsOpen && !talking && target != null && player != null) DrawWay(player.transform, target);
            else { arrowRt.gameObject.SetActive(false); ClearWorldMarkers(); if (diamondRt != null) { diamondRt.gameObject.SetActive(false); distanceText.gameObject.SetActive(false); } }

            DrawPuzzle();
        }

        private void DrawPanel(StoryDialogueSystem story, PlayerController player, Transform target)
        {
            var o = story.Current;
            var sb = new StringBuilder();
            if (o == null) { ScreenText.Panel("guide", "CASE CLOSED", "All three pieces are home.", PanelAnchor.Center, 1100, ScreenText.Key("H") + " Close", true, 50); return; }

            sb.Append($"<color={ScreenText.Gold}><b>WHAT TO DO</b></color>\n{o.instruction}\nKeys: {o.keys}\n\n");
            sb.Append($"<color={ScreenText.Gold}><b>WHERE TO GO</b></color>\n");
            string scene = SceneManager.GetActiveScene().name;
            if (target != null && player != null)
                sb.Append($"{o.area}: {Direction(player.transform, target.position)}, {Vector3.Distance(player.transform.position, target.position):0} m away.\n\n");
            else if (o.scene != scene)
                sb.Append($"This goal is in the {Nice(o.scene)}. Use the exit toward it.\n\n");
            else sb.Append(o.area + ".\n\n");

            var missing = story.MissingClues();
            if (missing.Count > 0)
            {
                sb.Append($"<color={ScreenText.Gold}><b>MISSING CLUES</b></color>\n");
                foreach (var m in missing) sb.Append($"<color={ScreenText.Cyan}>• {m}</color>\n");
                sb.Append('\n');
            }
            sb.Append($"<color={ScreenText.Gold}><b>HINTS</b></color>\n");
            string[] names = { "General", "Specific", "Direct answer" };
            for (int i = 0; i < 3; i++)
            {
                if (o.hints != null && i < o.hints.Length && HintLevel > i) sb.Append($"{i + 1}. {names[i]}: {o.hints[i]}\n");
                else sb.Append($"{ScreenText.Key((i + 1).ToString())} Show {names[i].ToLowerInvariant()} hint\n");
            }
            string footer = $"{ScreenText.Key("Enter")} {(WayShown ? "Hide the way" : "SHOW ME THE WAY")}    {ScreenText.Key("H")} / {ScreenText.Key("Tab")} Close";
            ScreenText.Panel("guide", o.title, $"<color={ScreenText.Cyan}>CURRENT GOAL  •  {story.Counter(o)}</color>\n\n" + sb.ToString(), PanelAnchor.Center, 1200, footer, true, 50);
        }

        private static string Nice(string scene)
        {
            switch (scene)
            {
                case Constants.Sanctum: return "Storage Hall";
                case Constants.ShrineCrime: return "Outer Shrine";
                case Constants.PuzzleWall: return "Sun Wall chamber";
                case Constants.JunglePath: return "Jungle Path";
                default: return "Courtyard";
            }
        }

        private static string Direction(Transform from, Vector3 to)
        {
            var cam = Camera.main;
            Vector3 fwd = cam != null ? cam.transform.forward : from.forward; fwd.y = 0; fwd.Normalize();
            Vector3 d = to - from.position; d.y = 0;
            if (d.sqrMagnitude < 0.5f) return "right here";
            float ang = Vector3.SignedAngle(fwd, d.normalized, Vector3.up);
            if (Mathf.Abs(ang) < 25f) return "straight ahead";
            if (Mathf.Abs(ang) > 150f) return "behind you";
            if (ang > 0) return ang < 70f ? "ahead to your right" : "to your right";
            return ang > -70f ? "ahead to your left" : "to your left";
        }

        // ------------------------------------------------------------ in-world guidance
        private RectTransform diamondRt;
        private TextMeshProUGUI distanceText;

        /// <summary>Small static marker: a diamond above the target with the distance, or a small edge arrow when off-screen.</summary>
        private void DrawWay(Transform player, Transform target)
        {
            ClearWorldMarkers();
            var cam = Camera.main;
            if (cam == null || DialogueOpen()) { arrowRt.gameObject.SetActive(false); if (diamondRt != null) diamondRt.gameObject.SetActive(false); return; }
            if (diamondRt == null)
            {
                var d = new GameObject("GuideDiamond", typeof(RectTransform));
                d.transform.SetParent(transform, false);
                diamondRt = (RectTransform)d.transform; diamondRt.sizeDelta = new Vector2(22, 22);
                diamondRt.anchorMin = diamondRt.anchorMax = Vector2.zero; diamondRt.localRotation = Quaternion.Euler(0, 0, 45);
                var im = d.AddComponent<Image>(); im.color = ScreenTextRenderer.Gold; im.raycastTarget = false;
                distanceText = ScreenTextRenderer.MakeText(transform, "GuideDistanceKeyHint", 24, ScreenTextRenderer.Ivory, false, TextAlignmentOptions.Center);
                distanceText.textWrappingMode = TextWrappingModes.NoWrap;
                distanceText.rectTransform.sizeDelta = new Vector2(140, 34);
                distanceText.rectTransform.anchorMin = distanceText.rectTransform.anchorMax = Vector2.zero;
            }
            Vector3 tp = target.position + Vector3.up * 2.2f;
            float dist = Vector3.Distance(player.position, target.position);
            distanceText.text = $"{dist:0} m";
            var rootRt = (RectTransform)transform;
            float k = rootRt.rect.height / Mathf.Max(1f, Screen.height);
            Vector3 sp = cam.WorldToScreenPoint(tp);
            bool behind = sp.z < 0f;
            if (behind) sp = -sp;
            bool onScreen = !behind && sp.x > 40 && sp.x < Screen.width - 40 && sp.y > 140 && sp.y < Screen.height - 40;
            diamondRt.gameObject.SetActive(onScreen);
            arrowRt.gameObject.SetActive(!onScreen);
            arrowRt.sizeDelta = new Vector2(30, 30);
            Vector2 pos;
            if (onScreen) { pos = new Vector2(sp.x, sp.y) * k; diamondRt.anchoredPosition = pos; }
            else
            {
                Vector2 c = new Vector2(Screen.width, Screen.height) * 0.5f;
                Vector2 dir = ((Vector2)sp - c).normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
                float mx = c.x - 60f, my = c.y - 150f;
                float t = Mathf.Min(mx / Mathf.Max(0.001f, Mathf.Abs(dir.x)), my / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
                pos = (c + dir * t) * k;
                arrowRt.anchoredPosition = pos;
                arrowRt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
            }
            distanceText.gameObject.SetActive(true);
            distanceText.rectTransform.anchoredPosition = pos + new Vector2(0, -32);
        }

        private GameObject CreateBeacon()
        {
            var go = new GameObject("GuideBeacon");
            DontDestroyOnLoad(go);
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(pillar.GetComponent<Collider>());
            pillar.transform.SetParent(go.transform, false);
            pillar.transform.localPosition = new Vector3(0, 3f, 0); pillar.transform.localScale = new Vector3(0.35f, 3f, 0.35f);
            var r = pillar.GetComponent<MeshRenderer>(); r.sharedMaterial = GlowMaterial(new Color(1f, 0.8f, 0.35f, 0.35f));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(go.transform, false);
            ring.transform.localPosition = new Vector3(0, 0.03f, 0); ring.transform.localScale = new Vector3(1.4f, 0.01f, 1.4f);
            var rr = ring.GetComponent<MeshRenderer>(); rr.sharedMaterial = GlowMaterial(new Color(1f, 0.8f, 0.35f, 0.6f));
            rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var light = new GameObject("BeaconLight").AddComponent<Light>();
            light.transform.SetParent(go.transform, false); light.transform.localPosition = new Vector3(0, 1.5f, 0);
            light.type = LightType.Point; light.color = new Color(1f, 0.8f, 0.4f); light.intensity = 3f; light.range = 6f;
            return go;
        }

        private static Material GlowMaterial(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var m = new Material(sh);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.color = c; m.SetColor("_BaseColor", c);
            return m;
        }

        private static bool DialogueOpen()
        {
            var ui = UIManager.Instance;
            if (ui == null) return false;
            var f = typeof(UIManager).GetField("isDialogueActive", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return f != null && (bool)f.GetValue(ui);
        }

        private void ClearWorldMarkers()
        {
            if (beacon != null) beacon.SetActive(false);
            if (line != null) line.gameObject.SetActive(false);
        }

        private static Sprite TriangleSprite()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float half = (n - y) * 0.5f;
                    bool inside = Mathf.Abs(x - n / 2f) < half * 0.9f && y > 4;
                    px[y * n + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            // flip so the tip points up
            var flipped = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) flipped[y * n + x] = px[(n - 1 - y) * n + x];
            tex.SetPixels32(flipped); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        // ------------------------------------------------------------ puzzle + ending
        private static void DrawPuzzle()
        {
            var pz = ChakraPuzzleController.Active;
            if (pz == null || !pz.IsInteracting) return;
            var sb = new StringBuilder("Scroll order: Lotus, Conch, Wheel, Kalash, then the centre wheel.\n");
            if (pz.ResetLeft > 0f) sb.Append($"<color={(pz.ResetLeft < 20f ? ScreenText.Coral : ScreenText.Gold)}><b>The wall resets in {pz.ResetLeft:0} s</b></color>\n");
            sb.Append('\n');
            if (pz.Cranking)
                sb.Append("<b>HAND-WHEEL</b>  ").Append(Fx.Bar(pz.Crank)).Append($"\nAlternate {ScreenText.Key("A")} and {ScreenText.Key("D")}. If you stop, it rolls back.\n");
            else
            {
                for (int i = 0; i < 4; i++)
                    sb.Append(ScreenText.Key((i + 1).ToString())).Append(' ').Append(ChakraPuzzleController.Symbols[i])
                      .Append(pz.IsTurned(i) ? $"  <color={ScreenText.Green}>faces the sun</color>" : "").Append('\n');
                sb.Append(ScreenText.Key("Enter")).Append(" Free the hand-wheel at the centre\n");
            }
            if (!string.IsNullOrEmpty(pz.Message)) sb.Append($"\n<color={ScreenText.Cyan}>{pz.Message}</color>");
            ScreenText.Panel("puzzle", "THE SUN WALL", sb.ToString(), PanelAnchor.BottomCenter, 1100, ScreenText.Key("Esc") + " Step away", false, 40);
        }

        private void DrawEnding(StoryDialogueSystem story)
        {
            var file = StoryData.File;
            var end = StoryData.Ending(DawnClock.EndingId());
            var sb = new StringBuilder();
            sb.Append($"<color={ScreenText.Gold}><b>ENDING: {end.title}</b></color>\n{end.text}\n\n");
            sb.Append($"<i>\"{file.ending?.text}\"</i>\n\n");
            sb.Append($"<color={ScreenText.Gold}><b>EVIDENCE</b></color>\n");
            foreach (var c in file.clues ?? new StoryClue[0]) sb.Append(story.HasClue(c.id) ? $"<color={ScreenText.Cyan}>• {c.title}</color>\n" : $"• {c.title}: not found\n");
            int t = Mathf.RoundToInt(story.Elapsed);
            float clock = DawnClock.Instance != null ? DawnClock.Instance.Minutes : 0f;
            sb.Append($"\n<color={ScreenText.Gold}><b>CLOCK</b></color>  {DawnClock.ClockText(clock)} (dawn 05:45)     <color={ScreenText.Gold}><b>PLAY TIME</b></color>  {t / 60:00}:{t % 60:00}     <color={ScreenText.Gold}><b>DIFFICULTY</b></color>  {GameOptions.DifficultyNames[GameOptions.Difficulty]}\n");
            sb.Append($"<color={ScreenText.Gold}><b>SCORE</b></color>  {story.Score}     <color={ScreenText.Gold}><b>HINTS USED</b></color>  {story.HintsUsed}     <color={ScreenText.Gold}><b>MEERA'S TRUST</b></color>  {StoryFlags.Get("trust_meera")}     <color={ScreenText.Gold}><b>RETRIES</b></color>  {(DawnClock.Instance != null ? DawnClock.Instance.Retries : 0)}");
            ScreenText.Panel("ending", file.ending?.title ?? "MISSION COMPLETED", sb.ToString(), PanelAnchor.Center, 1400, ScreenText.Key("Enter") + " Return to main menu", true, 100);
            var kb = Keyboard.current; var gp = Gamepad.current;
            if (Time.unscaledTime - endingTime > 2f && ((kb != null && kb.enterKey.wasPressedThisFrame) || (gp != null && gp.buttonSouth.wasPressedThisFrame)))
            {
                ending = false;
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
