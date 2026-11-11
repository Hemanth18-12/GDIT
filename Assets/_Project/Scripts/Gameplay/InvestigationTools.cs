using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Hand-held flashlight (G). In the dark storage hall it runs on a battery; in a fight it can flash to stun.</summary>
    public sealed class Flashlight : MonoBehaviour
    {
        public static bool On { get; private set; }
        public static float NeedLightUntil;
        public static float Battery { get; private set; } = 100f;
        /// <summary>True when the beam is strong enough to examine a clue.</summary>
        public static bool Usable => On && Battery > 0f;
        public const float FlashCooldown = 12f;
        public static float FlashReadyAt { get; private set; }
        public static float FlashReady01 => Mathf.Clamp01(1f - (FlashReadyAt - Time.time) / FlashCooldown);
        public static event System.Action<Vector3, Vector3> OnFlash; // origin, direction
        private Light spot; private float flashBoost;
        private static Flashlight inst;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("Flashlight");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<Flashlight>();
            StoryDialogueSystem.OnNewGame += () => { Battery = 100f; On = false; };
            DawnClock.OnRetry += () => Battery = Mathf.Max(Battery, 60f);
        }

        public static void Set(bool on) { On = on; }
        public static void AddBattery(float amount) { Battery = Mathf.Min(100f, Battery + amount); }

        /// <summary>Bright burst that stuns enemies in a cone (combat). Returns false while recharging.</summary>
        public static bool Flash()
        {
            if (Time.time < FlashReadyAt || inst == null) return false;
            FlashReadyAt = Time.time + FlashCooldown; inst.flashBoost = 1f;
            var p = FindAnyObjectByType<PlayerController>();
            if (p != null) OnFlash?.Invoke(p.transform.position + Vector3.up * 1.4f, p.transform.forward);
            AudioManager.Instance?.PlaySFX("sfx_flash_burst");
            return true;
        }

        private void Update()
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            bool combat = CombatState.InCombat;
            bool press = (kb != null && kb.gKey.wasPressedThisFrame) || (gp != null && (combat ? gp.rightShoulder.wasPressedThisFrame : gp.dpad.up.wasPressedThisFrame)) || VirtualInput.Down("flash");
            if (press && !InputGate.GameplayBlocked)
            {
                if (combat) Flash(); else { On = !On; AudioManager.Instance?.PlaySFX("sfx_flashlight_click"); }
            }
            var cam = Camera.main;
            if (cam == null) return;
            if (spot == null || spot.transform.parent != cam.transform)
            {
                spot = new GameObject("FlashlightBeam").AddComponent<Light>();
                spot.transform.SetParent(cam.transform, false);
                spot.transform.localPosition = new Vector3(0.25f, -0.2f, 0.3f);
                spot.type = LightType.Spot; spot.spotAngle = 48f; spot.innerSpotAngle = 22f; spot.range = 18f;
                spot.color = new Color(1f, 0.93f, 0.8f); spot.shadows = LightShadows.Soft;
            }
            bool sanctum = SceneManager.GetActiveScene().name == Constants.Sanctum;
            if (On && sanctum && Time.timeScale > 0f) Battery = Mathf.Max(0f, Battery - Time.deltaTime / 1.5f);
            flashBoost = Mathf.MoveTowards(flashBoost, 0f, Time.deltaTime * 2.5f);
            float baseI = !On ? 0f : Battery > 0f ? 6f : 0.9f + 0.4f * Mathf.Sin(Time.time * 23f);
            spot.intensity = baseI + flashBoost * 60f;
            spot.spotAngle = 48f + flashBoost * 40f;
            spot.enabled = On || flashBoost > 0f;
            if (sanctum && !CombatState.InCombat)
            {
                string col = Battery > 25f ? ScreenText.Ivory : ScreenText.Coral;
                string st = !On ? $"It is too dark to search. Press {ScreenText.Key("G")} for your flashlight." :
                    Battery > 0f ? $"Flashlight <color={col}>{Battery:0}%</color>" : $"<color={ScreenText.Coral}>Battery dead.</color> Find a spare cell (blue glow).";
                ScreenText.Panel("flashlight_hint", null, st, PanelAnchor.TopRight, 560f, null, false, 4);
            }
            if (Time.unscaledTime < NeedLightUntil)
                ScreenText.Panel("flashlight_need", null, $"<color={ScreenText.Coral}>You can't see any detail in the dark.</color> You need a working flashlight ({ScreenText.Key("G")}).", PanelAnchor.BottomCenter, 900f, null, false, 2);
        }
    }

    /// <summary>Shared combat flag (set by the fight director) so tools and input know when a fight is on.</summary>
    public static class CombatState
    {
        public static bool InCombat;
    }

    /// <summary>Outer shrine forensic kit (K): measure the two bootprints, test the lamp's warmth, join the torn note.</summary>
    public sealed class ForensicKit : MonoBehaviour
    {
        public static ForensicKit Instance { get; private set; }
        public bool IsOpen { get; private set; }
        public int Task { get; private set; }      // 0 prints, 1 lamp, 2 note, 3 done
        private int sub; private float ruler = 20f, hold; private string feedback = "";
        private static readonly float[] PrintCm = { 26.5f, 30.0f };
        private readonly int[] rot = { 1, 3, 2, 1 };
        private int piece;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("ForensicKit");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ForensicKit>();
        }
        private void Start() { StoryDialogueSystem.Instance?.RegisterMinigame("forensics"); }

        private static bool Active()
        {
            var s = StoryDialogueSystem.Instance;
            return s != null && s.Current != null && s.Current.complete.id == "forensics" && SceneManager.GetActiveScene().name == Constants.ShrineCrime;
        }

        public void ResetKit() { Task = 0; sub = 0; ruler = 20f; hold = 0f; piece = 0; rot[0] = 1; rot[1] = 3; rot[2] = 2; rot[3] = 1; feedback = ""; }

        // ---- actions (also called by tests and the bot) ----
        public void SlideRuler(float cm) { if (Task == 0 && sub < 2) ruler = Mathf.Clamp(ruler + cm, 15f, 36f); }
        public void Confirm()
        {
            if (Task == 0 && sub < 2)
            {
                if (Mathf.Abs(ruler - PrintCm[sub]) <= 0.4f) { feedback = $"<color={ScreenText.Green}>{PrintCm[sub]:0.0} cm.</color>"; sub++; ruler = 20f; }
                else { feedback = $"<color={ScreenText.Coral}>The ruler does not line up with the heel and toe.</color>"; DawnClock.Instance?.Penalty(0.5f, "careless measuring"); }
            }
            else if (Task == 2)
            {
                bool ok = true; foreach (int r in rot) if (r != 0) ok = false;
                if (ok) Finish(); else feedback = $"<color={ScreenText.Coral}>The torn edges do not meet yet.</color>";
            }
        }
        public void Answer(int i)
        {
            if (Task == 0 && sub == 2)
            {
                if (i == 1) { Task = 1; feedback = $"<color={ScreenText.Green}>Yes. The larger print is pressed deeper: he carries the load.</color>"; StoryDialogueSystem.Instance?.Say("forensic_boots"); }
                else { feedback = $"<color={ScreenText.Coral}>Look at the depth again.</color>"; DawnClock.Instance?.Penalty(1f, "a wrong reading"); }
            }
            else if (Task == 2 && i >= 0 && i < 4) piece = i;
        }
        public void RotatePiece() { if (Task == 2) rot[piece] = (rot[piece] + 1) % 4; }
        public void HoldLamp(float dt)
        {
            if (Task != 1) return;
            hold += dt;
            if (hold >= 2f) { Task = 2; hold = 0f; feedback = $"<color={ScreenText.Green}>Still warm at the wick: minutes, not hours.</color>"; StoryDialogueSystem.Instance?.Say("forensic_lamp"); }
        }
        private void Finish()
        {
            Task = 3; IsOpen = false;
            StoryDialogueSystem.Instance?.CompleteMinigame("forensics");
        }

        private void Update()
        {
            if (!Active()) { IsOpen = false; return; }
            var kb = Keyboard.current;
            if ((kb != null && kb.kKey.wasPressedThisFrame) || VirtualInput.Down("kit")) { IsOpen = !IsOpen; feedback = ""; }
            if (!IsOpen)
            {
                ScreenText.Panel("forensic_hint", null, $"Press {ScreenText.Key("K")} to open the forensic kit.", PanelAnchor.TopRight, 560f, null, false, 4);
                return;
            }
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { EventBus.EscapeConsumedFrame = Time.frameCount; IsOpen = false; return; }
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) SlideRuler(-6f * Time.unscaledDeltaTime);
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) SlideRuler(6f * Time.unscaledDeltaTime);
                if (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) Confirm();
                if (kb.digit1Key.wasPressedThisFrame) Answer(0); else if (kb.digit2Key.wasPressedThisFrame) Answer(1);
                else if (kb.digit3Key.wasPressedThisFrame) Answer(2); else if (kb.digit4Key.wasPressedThisFrame) Answer(3);
                if (kb.rKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) RotatePiece();
                if (kb.eKey.isPressed) HoldLamp(Time.unscaledDeltaTime); else if (Task == 1) hold = Mathf.Max(0f, hold - Time.unscaledDeltaTime * 2f);
            }
            if (!IsOpen) return;
            var sb = new System.Text.StringBuilder();
            string footer;
            if (Task == 0 && sub < 2)
            {
                sb.Append($"<b>BOOTPRINT {(sub == 0 ? "A (near the door)" : "B (by the pillar)")}</b>\nSlide the ruler until it reaches from heel to toe, then confirm.\n\n");
                sb.Append("<mspace=0.55em>Print  " + new string('#', Mathf.RoundToInt(PrintCm[sub])) + "\nRuler  <color=" + ScreenText.Gold + ">" + new string('=', Mathf.RoundToInt(ruler)) + "</color>|</mspace>\n");
                sb.Append($"Ruler reads <b>{ruler:0.0} cm</b>");
                footer = ScreenText.Key("A") + " / " + ScreenText.Key("D") + " Slide    " + ScreenText.Key("E") + " Confirm    " + ScreenText.Key("Esc") + " Close";
            }
            else if (Task == 0)
            {
                sb.Append($"<b>TWO SIZES</b>: {PrintCm[0]:0.0} cm and {PrintCm[1]:0.0} cm. One print is pressed far deeper into the mud.\nWho carried the heavy satchel?\n\n");
                sb.Append(ScreenText.Key("1") + " The smaller boot\n" + ScreenText.Key("2") + " The larger boot\n");
                footer = ScreenText.Key("1") + " / " + ScreenText.Key("2") + " Answer    " + ScreenText.Key("Esc") + " Close";
            }
            else if (Task == 1)
            {
                sb.Append("<b>BROKEN LAMP</b>\nHold the back of your hand near the wick and keep it steady.\n\n");
                int n = Mathf.RoundToInt(hold / 2f * 24f);
                sb.Append("<mspace=0.55em>[<color=" + ScreenText.Gold + ">" + new string('|', n) + "</color>" + new string('.', 24 - n) + "]</mspace>");
                footer = "Hold " + ScreenText.Key("E") + " for two seconds    " + ScreenText.Key("Esc") + " Close";
            }
            else
            {
                string[] arrow = { "UP", "RIGHT", "DOWN", "LEFT" };
                sb.Append("<b>TORN NOTE</b>\nTurn each piece until every torn edge points UP.\n\n");
                for (int i = 0; i < 4; i++)
                    sb.Append((i == piece ? "<color=" + ScreenText.Gold + ">> " : "  ") + ScreenText.Key((i + 1).ToString()) + $" Piece {i + 1}: {(rot[i] == 0 ? "<color=" + ScreenText.Green + ">UP</color>" : arrow[rot[i]])}" + (i == piece ? "</color>" : "") + "\n");
                footer = ScreenText.Key("1-4") + " Pick    " + ScreenText.Key("R") + " Turn    " + ScreenText.Key("E") + " Join    " + ScreenText.Key("Esc") + " Close";
            }
            if (feedback.Length > 0) sb.Append("\n\n").Append(feedback);
            ScreenText.Panel("forensics", $"FORENSIC KIT  {Mathf.Min(Task + 1, 3)}/3", sb.ToString(), PanelAnchor.Center, 1200f, footer, true, 60);
        }
    }

    /// <summary>Jungle path: follow the two men at a distance, hide (crouch, cover) when they look back.</summary>
    public sealed class JungleStealth : MonoBehaviour
    {
        public static JungleStealth Instance { get; private set; }
        public Vector3 Direction { get; private set; }
        public bool LookingBack { get; private set; }
        public float Progress => traveled / PathLength;
        public int Resets { get; private set; }

        private const float PathLength = 16f, WalkSpeed = 1.1f, SeeRange = 12f, CrouchSeeRange = 5.5f, LoseRange = 24f, ConeHalfAngle = 35f;
        private readonly List<Transform> robbers = new List<Transform>();
        private readonly List<Vector3> starts = new List<Vector3>();
        private Vector3 playerStart;
        private float loseRange = LoseRange;
        private float traveled, timer, message;
        private string messageText = "";
        private bool running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("JungleStealth");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<JungleStealth>();
            SceneManager.sceneLoaded += (s, m) => { if (Instance != null) Instance.running = false; };
        }
        private void Start() { StoryDialogueSystem.Instance?.RegisterMinigame("stealth"); }

        private static bool Active()
        {
            var s = StoryDialogueSystem.Instance;
            return s != null && s.Current != null && s.Current.complete.id == "stealth" && SceneManager.GetActiveScene().name == Constants.JunglePath;
        }

        private void Begin(PlayerController player)
        {
            robbers.Clear(); starts.Clear();
            foreach (var n in FindObjectsByType<NPCDialogueTrigger>())
                if (n.Id == "npc_" + Constants.Robber1 || n.Id == "npc_" + Constants.Robber2) { robbers.Add(n.transform); starts.Add(n.transform.position); }
            if (robbers.Count == 0) return;
            Vector3 d = robbers[0].position - player.transform.position; d.y = 0f;
            Direction = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
            playerStart = player.transform.position;
            traveled = 0f; timer = 0f; running = true;
            loseRange = Mathf.Max(LoseRange, d.magnitude + 10f); // never 'lost' at the starting distance
        }

        private void ResetSection(PlayerController player, string why)
        {
            Resets++;
            for (int i = 0; i < robbers.Count; i++) if (robbers[i] != null) robbers[i].position = starts[i];
            player.Teleport(playerStart, Quaternion.LookRotation(Direction));
            traveled = 0f; timer = 0f;
            messageText = why; message = 3f;
        }

        private static void Anim(Transform t, bool moving)
        {
            var a = t.GetComponentInChildren<Animator>();
            if (a == null) return;
            foreach (var p in a.parameters)
                if (p.name == "IsMoving" || p.name == "IsStealth") a.SetBool(p.name, moving);
        }

        private void Update()
        {
            if (!Active()) { running = false; return; }
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            if (!running) { Begin(player); if (!running) return; }

            timer += Time.deltaTime;
            float cycle = timer % 7f;
            LookingBack = cycle > 4.5f; // walk 4.5 s, look back 2.5 s
            bool moving = !LookingBack && traveled < PathLength;
            foreach (var r in robbers)
            {
                if (r == null) continue;
                Vector3 face = LookingBack ? (player.transform.position - r.position) : Direction;
                face.y = 0f;
                if (face.sqrMagnitude > 0.01f) r.rotation = Quaternion.Slerp(r.rotation, Quaternion.LookRotation(face), Time.deltaTime * 4f);
                if (moving && !Physics.Raycast(r.position + Vector3.up * 1.2f, Direction, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                    r.position += Direction * WalkSpeed * Time.deltaTime;
                Anim(r, moving);
            }
            if (moving) traveled += WalkSpeed * Time.deltaTime;

            // Detection
            Vector3 chest = player.transform.position + Vector3.up * (player.IsCrouching ? 0.8f : 1.4f);
            float nearest = float.MaxValue;
            bool seen = false;
            foreach (var r in robbers)
            {
                if (r == null) continue;
                Vector3 eye = r.position + Vector3.up * 1.6f;
                Vector3 to = chest - eye;
                nearest = Mathf.Min(nearest, to.magnitude);
                if (!LookingBack) continue;
                float range = player.IsCrouching ? CrouchSeeRange : SeeRange;
                if (to.magnitude > range) continue;
                if (Vector3.Angle(r.forward, new Vector3(to.x, 0f, to.z)) > ConeHalfAngle) continue;
                if (Physics.Raycast(eye, to.normalized, out RaycastHit hit, to.magnitude, ~0, QueryTriggerInteraction.Ignore) &&
                    !hit.collider.transform.IsChildOf(player.transform)) continue; // behind cover
                seen = true;
            }
            if (seen) ResetSection(player, "They spotted you! Stay farther back, crouch (C) and use the rocks for cover.");
            else if (nearest > loseRange) ResetSection(player, "You lost them. Stay closer.");

            if (traveled >= PathLength && !LookingBack)
            {
                foreach (var r in robbers) if (r != null) Anim(r, false);
                running = false;
                StoryDialogueSystem.Instance?.CompleteMinigame("stealth");
                return;
            }

            string status = LookingBack ? $"<color={ScreenText.Coral}>THEY ARE LOOKING BACK - hide!</color>" : $"<color={ScreenText.Green}>Moving. Follow quietly.</color>";
            message -= Time.deltaTime;
            string body = $"{status}\nDistance {(nearest < 999f ? nearest : 0f):0} m   Path {Progress * 100f:0}%\n{ScreenText.Key("C")} Crouch to hide";
            if (message > 0f) body += $"\n<color={ScreenText.Coral}>{messageText}</color>";
            ScreenText.Panel("stealth", null, body, PanelAnchor.TopRight, 640f, null, false, 4);
        }
    }
}
