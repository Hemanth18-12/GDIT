using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Hand-held flashlight (G). The storage hall is dark: clues there need the light on.</summary>
    public sealed class Flashlight : MonoBehaviour
    {
        public static bool On { get; private set; }
        public static float NeedLightUntil;
        private Light spot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("Flashlight");
            DontDestroyOnLoad(go);
            go.AddComponent<Flashlight>();
        }

        public static void Set(bool on) { On = on; }

        private void Update()
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            if ((kb != null && kb.gKey.wasPressedThisFrame) || (gp != null && gp.dpad.up.wasPressedThisFrame)) On = !On;
            var cam = Camera.main;
            if (cam == null) return;
            if (spot == null || spot.transform.parent != cam.transform)
            {
                spot = new GameObject("FlashlightBeam").AddComponent<Light>();
                spot.transform.SetParent(cam.transform, false);
                spot.transform.localPosition = new Vector3(0.25f, -0.2f, 0.3f);
                spot.type = LightType.Spot; spot.spotAngle = 48f; spot.innerSpotAngle = 22f; spot.range = 18f;
                spot.intensity = 6f; spot.color = new Color(1f, 0.93f, 0.8f); spot.shadows = LightShadows.Soft;
            }
            spot.enabled = On;
            bool sanctum = SceneManager.GetActiveScene().name == Constants.Sanctum;
            if (sanctum && !On)
                ScreenText.Panel("flashlight_hint", null, $"It is too dark to search. Press {ScreenText.Key("G")} for your flashlight.", PanelAnchor.TopRight, 640f, null, false, 4);
            if (Time.unscaledTime < NeedLightUntil)
                ScreenText.Panel("flashlight_need", null, $"<color={ScreenText.Coral}>You can't see any detail in the dark.</color> Turn on the flashlight with {ScreenText.Key("G")}.", PanelAnchor.BottomCenter, 900f, null, false, 2);
        }
    }

    /// <summary>Outer shrine forensic kit (K): compare the two boot sizes and match the red silk, with feedback and retry.</summary>
    public sealed class ForensicKit : MonoBehaviour
    {
        public static ForensicKit Instance { get; private set; }
        public bool IsOpen { get; private set; }
        public int Step { get; private set; }
        private string feedback = "";

        private static readonly string[] Questions = {
            "BOOTPRINTS: The muddy prints near the door come in two sizes. What does that tell you?",
            "RED SILK: The torn red silk was caught on the doorframe. Whose clothing does it match?" };
        private static readonly string[][] Options = {
            new[] { "One person walked in and out twice", "Two different people, a smaller and a larger boot", "A temple worker's sandals" },
            new[] { "Kishore's black hood", "Bhaskar's red scarf", "The temple's own curtains" } };
        private static readonly int[] Correct = { 1, 1 };
        private static readonly string[] Hints = {
            "Look at the sizes: if there are two sizes, how many people?",
            "Meera saw one man in a red scarf. The Pujari said the cloth is not the temple's." };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("ForensicKit");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ForensicKit>();
        }

        private static bool Active()
        {
            var s = StoryDialogueSystem.Instance;
            return s != null && s.Current != null && s.Current.id == "obj_forensics" && SceneManager.GetActiveScene().name == Constants.ShrineCrime;
        }

        public void Answer(int i)
        {
            if (!IsOpen) return;
            if (i == Correct[Step])
            {
                Step++;
                feedback = $"<color={ScreenText.Green}>Correct.</color>";
                if (Step >= Questions.Length)
                {
                    IsOpen = false; Step = 0;
                    StoryDialogueSystem.Instance?.CompleteMinigame("forensics");
                }
            }
            else feedback = $"<color={ScreenText.Coral}>Not quite. Try again.</color> Hint: {Hints[Step]}";
        }

        private void Update()
        {
            if (!Active()) { IsOpen = false; return; }
            var kb = Keyboard.current;
            if (kb != null && kb.kKey.wasPressedThisFrame) { IsOpen = !IsOpen; feedback = ""; }
            if (!IsOpen)
            {
                ScreenText.Panel("forensic_hint", null, $"Press {ScreenText.Key("K")} to open the forensic kit and compare the evidence.", PanelAnchor.TopRight, 640f, null, false, 4);
                return;
            }
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Answer(0);
                else if (kb.digit2Key.wasPressedThisFrame) Answer(1);
                else if (kb.digit3Key.wasPressedThisFrame) Answer(2);
                if (kb.escapeKey.wasPressedThisFrame) { EventBus.EscapeConsumedFrame = Time.frameCount; IsOpen = false; }
            }
            if (!IsOpen) return;
            var sb = new System.Text.StringBuilder(Questions[Step]).Append("\n\n");
            for (int k = 0; k < 3; k++) sb.Append(ScreenText.Key((k + 1).ToString())).Append(' ').Append(Options[Step][k]).Append('\n');
            if (feedback.Length > 0) sb.Append('\n').Append(feedback);
            ScreenText.Panel("forensics", $"FORENSIC KIT  {Step + 1}/{Questions.Length}", sb.ToString(), PanelAnchor.Center, 1100f, ScreenText.Key("1-3") + " Answer    " + ScreenText.Key("Esc") + " Close", true, 60);
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

        private static bool Active()
        {
            var s = StoryDialogueSystem.Instance;
            return s != null && s.Current != null && s.Current.id == "obj_follow" && SceneManager.GetActiveScene().name == Constants.JunglePath;
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
