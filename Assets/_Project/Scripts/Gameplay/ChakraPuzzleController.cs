using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Sun wall puzzle (story beat 4): turn the four symbol panels to face the rising sun in the order
    /// lotus, conch, wheel, kalash, then turn the centre wheel. UI is drawn by LegacyOfTemples.UI.HeritageHUD
    /// (TextMeshPro) from the public state below.
    /// </summary>
    public class ChakraPuzzleController : MonoBehaviour, IInteractable
    {
        [Header("Puzzle Wheels (centre wheel visuals)")]
        [SerializeField] private Transform outerWheel;
        [SerializeField] private Transform middleWheel;
        [SerializeField] private Transform innerWheel;

        [Header("Gateway Door")]
        [SerializeField] private Transform stoneGatewayDoor;
        [SerializeField] private Vector3 doorOpenOffset = new Vector3(0f, 5.0f, 0f);

        [Header("Audio")]
        [SerializeField] private AudioClip stoneGrindSound;
        [SerializeField] private AudioClip alignmentChimeSound;
        [SerializeField] private AudioClip doorOpenSound;

        public static readonly string[] Symbols = { "Lotus", "Conch", "Wheel", "Kalash" };
        public static ChakraPuzzleController Active { get; private set; }

        private readonly Transform[] panels = new Transform[4];
        private readonly Quaternion[] panelStart = new Quaternion[4];
        private readonly bool[] turned = new bool[4];
        private AudioSource audioSource;
        private bool isInteracting;
        private bool isSolved;
        private int step; // how many symbols are correctly turned
        private float centreSpin;
        private Vector3 initialDoorPos, targetDoorPos;

        public string Id => "puzzle_chakra_wall";
        public string Prompt => isSolved ? "The sun wall is open" : "Use the sun wall";
        public Transform Anchor => transform;
        public float Radius => 3.5f;
        public bool IsSolved => isSolved;
        public bool IsInteracting => isInteracting;
        public int Step => step;
        public bool IsTurned(int i) => turned[i];
        public string Message { get; private set; } = "";
        /// <summary>Seconds left before the mechanism resets (starts with the first symbol).</summary>
        public float ResetLeft { get; private set; }
        public bool Cranking { get; private set; }
        public float Crank { get; private set; }
        public int Resets { get; private set; }
        private int lastCrankKey; private bool timerOn;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; }
            if (stoneGatewayDoor != null) { initialDoorPos = stoneGatewayDoor.position; targetDoorPos = initialDoorPos; }
            for (int i = 0; i < 4; i++)
            {
                var go = GameObject.Find("panel_" + Symbols[i].ToLowerInvariant());
                if (go != null) { panels[i] = go.transform; panelStart[i] = go.transform.localRotation; }
            }
        }

        private void Start()
        {
            if (PlayerPrefs.GetInt("ChakraPuzzle_Solved", 0) == 1) SolvePuzzle(true);
        }

        private void OnEnable() { Active = this; }
        private void OnDisable() { if (Active == this) Active = null; }

        public void Interact()
        {
            if (isSolved) return;
            isInteracting = !isInteracting;
            Message = isInteracting ? "Press the symbols in the scroll's order." : "";
        }

        private void Update()
        {
            for (int i = 0; i < 4; i++)
            {
                if (panels[i] == null) continue;
                Quaternion goal = turned[i] ? panelStart[i] * Quaternion.Euler(0f, 0f, 180f) : panelStart[i];
                panels[i].localRotation = Quaternion.Slerp(panels[i].localRotation, goal, Time.deltaTime * 4f);
            }
            if (isSolved && centreSpin < 360f)
            {
                float d = Time.deltaTime * 180f; centreSpin += d;
                if (outerWheel) outerWheel.Rotate(0f, 0f, d, Space.Self);
                if (middleWheel) middleWheel.Rotate(0f, 0f, -d, Space.Self);
                if (innerWheel) innerWheel.Rotate(0f, 0f, d * 2f, Space.Self);
            }
            if (stoneGatewayDoor != null)
                stoneGatewayDoor.position = Vector3.Lerp(stoneGatewayDoor.position, targetDoorPos, Time.deltaTime * 2f);

            if (timerOn && !isSolved && Time.timeScale > 0f)
            {
                ResetLeft -= Time.deltaTime;
                if (ResetLeft <= 0f) ResetWall("The mechanism resets. Begin again.", 2f, "the wall reset", "wall_reset");
            }
            if (Cranking && !isSolved) Crank = Mathf.Max(0f, Crank - Time.deltaTime * 0.12f);
            if (!isInteracting || isSolved) return;
            InputGate.EnterReservedUntil = Time.frameCount + 1;
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            if (kb == null && gp == null) return;
            int pressed = -1;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) pressed = 0;
                else if (kb.digit2Key.wasPressedThisFrame) pressed = 1;
                else if (kb.digit3Key.wasPressedThisFrame) pressed = 2;
                else if (kb.digit4Key.wasPressedThisFrame) pressed = 3;
                if (kb.escapeKey.wasPressedThisFrame) { EventBus.EscapeConsumedFrame = Time.frameCount; isInteracting = false; return; }
            }
            if (gp != null)
            {
                if (gp.dpad.up.wasPressedThisFrame) pressed = 0;
                else if (gp.dpad.right.wasPressedThisFrame) pressed = 1;
                else if (gp.dpad.down.wasPressedThisFrame) pressed = 2;
                else if (gp.dpad.left.wasPressedThisFrame) pressed = 3;
                if (gp.buttonEast.wasPressedThisFrame) { isInteracting = false; return; }
            }
            if (pressed >= 0 && !Cranking) PressSymbol(pressed);
            if (Cranking && kb != null)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) CrankPush(1);
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) CrankPush(2);
            }

            bool centre = (kb != null && (kb.digit5Key.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
            if (centre) TurnCentre();
        }

        public void PressSymbol(int i)
        {
            if (isSolved || turned[i]) return;
            if (i == step)
            {
                if (!timerOn) { timerOn = true; ResetLeft = GameOptions.WallResetSeconds; }
                turned[i] = true; step++;
                Message = Symbols[i] + " faces the sun." + (step == 4 ? " Now turn the centre wheel." : "");
                if (stoneGrindSound) audioSource.PlayOneShot(stoneGrindSound);
            }
            else
            {
                ResetWall("Stone dust falls and the panels slide back. Follow the scroll: Lotus, Conch, Wheel, Kalash.", 4f, "wrong symbol", "wall_wrong");
                Fx.Burst("Dust", transform.position + Vector3.up * 3f, 40, 2.5f, 0.5f, 2.2f, new Color(0.7f, 0.62f, 0.5f, 0.6f), 0.4f);
            }
        }

        public void TurnCentre()
        {
            if (isSolved) return;
            if (step == 4) { if (!Cranking) { Cranking = true; Crank = 0f; lastCrankKey = 0; Message = "The hand-wheel is free. Alternate A and D to crank it."; } }
            else Message = "The centre will not move. Turn all four symbols first.";
        }

        /// <summary>One push of the hand-wheel: the keys must alternate (1 = A, 2 = D).</summary>
        public void CrankPush(int key)
        {
            if (!Cranking || isSolved || key == lastCrankKey) return;
            lastCrankKey = key; Crank += 1f / 14f;
            if (stoneGrindSound) audioSource.PlayOneShot(stoneGrindSound, 0.4f);
            if (outerWheel) outerWheel.Rotate(0f, 0f, 12f, Space.Self);
            if (Crank >= 1f) SolvePuzzle(false);
        }

        private void ResetWall(string msg, float penalty, string why, string bark)
        {
            for (int k = 0; k < 4; k++) turned[k] = false;
            step = 0; Cranking = false; Crank = 0f; timerOn = false; ResetLeft = 0f; Resets++;
            Message = msg;
            DawnClock.Instance?.Penalty(penalty, why);
            if (Resets == 1 || bark == "wall_wrong") StoryDialogueSystem.Instance?.Say(bark);
        }

        public void SolvePuzzle() => SolvePuzzle(false);

        private void SolvePuzzle(bool silent)
        {
            isSolved = true; isInteracting = false; timerOn = false; Cranking = false;
            for (int k = 0; k < 4; k++) turned[k] = true;
            step = 4;
            if (silent) centreSpin = 360f;
            PlayerPrefs.SetInt("ChakraPuzzle_Solved", 1);
            PlayerPrefs.SetInt("LevelUnlocked_3", 1);
            PlayerPrefs.Save();
            if (stoneGatewayDoor != null) targetDoorPos = initialDoorPos + doorOpenOffset;
            if (silent) return;
            if (alignmentChimeSound) audioSource.PlayOneShot(alignmentChimeSound);
            if (doorOpenSound) audioSource.PlayOneShot(doorOpenSound);
            Message = "";
            EventBus.RaisePuzzleSolved("chakra_wall");
        }
    }
}
