using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Characters
{
    [SelectionBase]
    public class NPCDialogueTrigger : MonoBehaviour, IInteractable
    {
        [Header("Character Identity")]
        [SerializeField] private string characterId = Constants.Priest;
        [SerializeField] private string speakerName = "Priest Madhav";
        [SerializeField] private float interactionRadius = 2.8f;

        [Header("Dialogue Content")]
        [TextArea(2, 4)]
        [SerializeField] private string[] dialogueLines;

        private int currentLineIndex = -1;
        private bool isDialogueActive = false;
        private Transform playerTransform;
        private GUIStyle boxStyle;
        private GUIStyle nameStyle;
        private GUIStyle textStyle;
        private GUIStyle hintStyle;

        public string Id => $"npc_{characterId}";
        public string Prompt => $"Talk to {speakerName}";
        public Transform Anchor => transform;
        public float Radius => interactionRadius;
        public bool IsDialogueActive => isDialogueActive;

        public void Setup(string id, string name, string[] lines, float radius = 2.8f)
        {
            characterId = id;
            speakerName = name;
            dialogueLines = lines;
            interactionRadius = radius;
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        private void Update()
        {
            if (isDialogueActive)
            {
                // Auto close if player walks away
                if (playerTransform == null)
                {
                    GameObject p = GameObject.FindWithTag("Player");
                    if (p != null) playerTransform = p.transform;
                }

                if (playerTransform != null)
                {
                    float dist = Vector3.Distance(transform.position, playerTransform.position);
                    if (dist > interactionRadius * 1.6f)
                    {
                        EndDialogue();
                    }
                }

                // Advance dialogue on space, enter, or E key
                bool advancePressed = false;
                var kb = Keyboard.current;
                if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                {
                    advancePressed = true;
                }
                var gp = Gamepad.current;
                if (gp != null && gp.buttonSouth.wasPressedThisFrame)
                {
                    advancePressed = true;
                }

                if (advancePressed)
                {
                    AdvanceDialogue();
                }
            }
        }

        public void Interact()
        {
            EventBus.RaisePlayerInteract(this);

            if (!isDialogueActive)
            {
                StartDialogue();
            }
            else
            {
                AdvanceDialogue();
            }
        }

        public void StartDialogue()
        {
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                dialogueLines = new string[] { $"Blessings, traveler. The sacred deities watch over our temple." };
            }

            isDialogueActive = true;
            currentLineIndex = 0;
            EventBus.RaiseDialogueStart(characterId);
            Debug.Log($"[NPCDialogueTrigger] Dialogue started with {speakerName}: '{dialogueLines[currentLineIndex]}'");
        }

        public void AdvanceDialogue()
        {
            currentLineIndex++;
            if (currentLineIndex >= dialogueLines.Length)
            {
                EndDialogue();
            }
            else
            {
                Debug.Log($"[NPCDialogueTrigger] {speakerName}: '{dialogueLines[currentLineIndex]}'");
            }
        }

        public void EndDialogue()
        {
            if (isDialogueActive)
            {
                isDialogueActive = false;
                currentLineIndex = -1;
                EventBus.RaiseDialogueEnd(characterId);
                Debug.Log($"[NPCDialogueTrigger] Dialogue ended with {speakerName}.");
            }
        }

        private void OnGUI()
        {
            if (isDialogueActive && currentLineIndex >= 0 && currentLineIndex < dialogueLines.Length)
            {
                InitStyles();

                float width = Mathf.Min(680f, Screen.width - 40f);
                float height = 110f;
                float x = (Screen.width - width) * 0.5f;
                float y = Screen.height - 140f;

                GUI.Box(new Rect(x, y, width, height), GUIContent.none, boxStyle);
                GUI.Label(new Rect(x + 20f, y + 10f, width - 40f, 22f), speakerName.ToUpperInvariant(), nameStyle);
                GUI.Label(new Rect(x + 20f, y + 36f, width - 40f, 44f), $"\"{dialogueLines[currentLineIndex]}\"", textStyle);
                GUI.Label(new Rect(x + 20f, y + 84f, width - 40f, 18f), "Press [E] or [Space] to continue...", hintStyle);
            }
        }

        private void InitStyles()
        {
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
            }

            if (nameStyle == null)
            {
                nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.96f, 0.78f, 0.22f) }
                };
            }

            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
            }

            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.7f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}

