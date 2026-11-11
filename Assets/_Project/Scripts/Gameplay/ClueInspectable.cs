using System;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Gameplay
{
    [SelectionBase]
    public class ClueInspectable : MonoBehaviour, IInteractable
    {
        [Header("Clue Identity")]
        [SerializeField] private string clueId = "clue_default";
        [SerializeField] private string clueTitle = "Mysterious Evidence";
        [SerializeField] private string clueCategory = "Sanctum Breach";
        [SerializeField] private float interactionRadius = 2.4f;

        [Header("Description & Deductions")]
        [TextArea(3, 6)]
        [SerializeField] private string clueDescription = "Evidence examined by Detective Aryan.";
        [TextArea(2, 4)]
        [SerializeField] private string detectiveNote = "This indicates the intruders were prepared and had insider knowledge.";

        [Header("3D Inspection Model")]
        [SerializeField] private Transform inspectTransform;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private bool allow3DRotation = true;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip inspectSound;

        private bool isInspecting = false;
        private bool isExamined = false;
        private OrbitCamera mainOrbitCam;
        private AudioSource audioSource;

        public string Id => clueId;
        public string Prompt => isExamined ? $"Examine {clueTitle} again" : $"Inspect {clueTitle}";
        public Transform Anchor => inspectTransform != null ? inspectTransform : transform;
        public float Radius => interactionRadius;
        public bool IsInspecting => isInspecting;
        public bool IsExamined => isExamined;
        public string ClueTitle => clueTitle;
        public string ClueCategory => clueCategory;
        public string ClueDescription => clueDescription;
        public string DetectiveNote => detectiveNote;

        public void Setup(string id, string title, string category, string desc, string note, float radius = 2.4f)
        {
            clueId = id;
            clueTitle = title;
            clueCategory = category;
            clueDescription = desc;
            detectiveNote = note;
            interactionRadius = radius;
        }

        private void Awake()
        {
            if (inspectTransform == null) inspectTransform = transform;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.5f;
            }
        }

        private void Start()
        {
            var st = StoryData.Clue(clueId); // story text overrides the scene's old labels
            if (st != null) { clueTitle = st.title; clueDescription = st.text; detectiveNote = st.note; }
            if (Camera.main != null)
            {
                mainOrbitCam = Camera.main.GetComponent<OrbitCamera>();
            }
        }

        private void Update()
        {
            if (isInspecting)
            {
                // Handle 3D rotation with mouse or keyboard [A]/[D] / Left / Right
                if (allow3DRotation && inspectTransform != null)
                {
                    float rotInput = 0f;
                    var kb = Keyboard.current;
                    if (kb != null)
                    {
                        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) rotInput += 1f;
                        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) rotInput -= 1f;
                    }
                    var mouse = Mouse.current;
                    if (mouse != null && mouse.leftButton.isPressed)
                    {
                        rotInput -= mouse.delta.x.ReadValue() * 0.4f;
                    }

                    if (Mathf.Abs(rotInput) > 0.01f)
                    {
                        inspectTransform.Rotate(Vector3.up, rotInput * rotationSpeed * Time.deltaTime, Space.World);
                    }
                }

                // Close inspection on Escape, E, or Space
                bool closePressed = false;
                var kbCurrent = Keyboard.current;
                if (kbCurrent != null && (kbCurrent.escapeKey.wasPressedThisFrame || kbCurrent.eKey.wasPressedThisFrame || kbCurrent.spaceKey.wasPressedThisFrame))
                {
                    closePressed = true;
                }
                var gp = Gamepad.current;
                if (gp != null && (gp.buttonEast.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame))
                {
                    closePressed = true;
                }

                if (closePressed)
                {
                    EventBus.EscapeConsumedFrame = Time.frameCount;
                    CloseInspection();
                }
            }
        }

        public void Interact()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == Constants.Sanctum && !Flashlight.On)
            {
                Flashlight.NeedLightUntil = Time.unscaledTime + 2.5f;
                return;
            }
            EventBus.RaisePlayerInteract(this);
            if (!isInspecting)
            {
                OpenInspection();
            }
            else
            {
                CloseInspection();
            }
        }

        public void OpenInspection()
        {
            isInspecting = true;
            isExamined = true;

            if (mainOrbitCam == null && Camera.main != null)
            {
                mainOrbitCam = Camera.main.GetComponent<OrbitCamera>();
            }
            if (mainOrbitCam != null)
            {
                mainOrbitCam.SetInspectionMode(Anchor);
            }

            PlayInspectionFeedback();
            EventBus.RaiseClueFound(clueId);

            if (InvestigationJournal.Instance != null)
            {
                InvestigationJournal.Instance.RegisterClue(clueId, clueTitle, clueCategory, clueDescription, detectiveNote);
            }

            Debug.Log($"[ClueInspectable] Opened clue inspection for '{clueTitle}' ({clueId}).");
        }

        public void CloseInspection()
        {
            if (isInspecting)
            {
                isInspecting = false;
                if (mainOrbitCam != null)
                {
                    mainOrbitCam.ClearInspectionMode();
                }
                Debug.Log($"[ClueInspectable] Closed clue inspection for '{clueTitle}'.");
            }
        }

        private void PlayInspectionFeedback()
        {
            if (inspectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(inspectSound);
            }
        }

        private void LateUpdate()
        {
            if (!isInspecting) return;
            string hint = allow3DRotation
                ? ScreenText.Key("Mouse") + " / " + ScreenText.Key("A") + ScreenText.Key("D") + " Rotate    " + ScreenText.Key("E") + " / " + ScreenText.Key("Esc") + " Close"
                : ScreenText.Key("E") + " / " + ScreenText.Key("Esc") + " Close    Saved to Journal " + ScreenText.Key("J");
            ScreenText.Panel("clue", clueTitle,
                $"<color={ScreenText.Cyan}>EVIDENCE: {clueCategory.ToUpperInvariant()}</color>\n{clueDescription}\n\n<color={ScreenText.Gold}>Aryan's note:</color> {detectiveNote}",
                PanelAnchor.BottomCenter, 1100f, hint, false, 30);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}

