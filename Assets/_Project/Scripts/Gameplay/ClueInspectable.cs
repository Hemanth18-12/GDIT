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

        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle categoryStyle;
        private GUIStyle descStyle;
        private GUIStyle noteStyle;
        private GUIStyle hintStyle;

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
                    CloseInspection();
                }
            }
        }

        public void Interact()
        {
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

        private void OnGUI()
        {
            if (isInspecting)
            {
                InitStyles();

                float width = Mathf.Min(640f, Screen.width - 40f);
                float height = 260f;
                float x = (Screen.width - width) * 0.5f;
                float y = Screen.height - height - 40f;

                GUI.Box(new Rect(x, y, width, height), GUIContent.none, panelStyle);

                // Header
                GUI.Label(new Rect(x + 24f, y + 16f, width - 48f, 20f), $"EVIDENCE RECORD: {clueCategory.ToUpperInvariant()}", categoryStyle);
                GUI.Label(new Rect(x + 24f, y + 38f, width - 48f, 28f), clueTitle, titleStyle);

                // Description
                GUI.Label(new Rect(x + 24f, y + 74f, width - 48f, 75f), clueDescription, descStyle);

                // Forensic note
                GUI.Label(new Rect(x + 24f, y + 155f, width - 48f, 50f), $"<color=#f4c430>Detective's Deduction:</color> \"{detectiveNote}\"", noteStyle);

                // Controls hint
                string hint = allow3DRotation
                    ? "Drag [Mouse] or [A]/[D] to rotate 3D object  •  Press [E] or [Esc] to close inspection"
                    : "Press [E] or [Esc] to close inspection  •  Logged to Investigation Journal [J]";
                GUI.Label(new Rect(x + 24f, y + height - 26f, width - 48f, 20f), hint, hintStyle);
            }
        }

        private void InitStyles()
        {
            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box);
            }

            if (categoryStyle == null)
            {
                categoryStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.85f, 0.65f, 0.2f) }
                };
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            if (descStyle == null)
            {
                descStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    wordWrap = true,
                    normal = { textColor = new Color(0.88f, 0.88f, 0.88f) }
                };
            }

            if (noteStyle == null)
            {
                noteStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true,
                    fontStyle = FontStyle.Italic,
                    richText = true,
                    normal = { textColor = new Color(0.95f, 0.95f, 0.75f) }
                };
            }

            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.65f, 0.65f, 0.65f) }
                };
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}

