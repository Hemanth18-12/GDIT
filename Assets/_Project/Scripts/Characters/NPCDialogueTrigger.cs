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
            EventBus.RaiseDialogueStart(characterId);
            EventBus.RaiseRequestDialogue(speakerName, dialogueLines, null, null, () =>
            {
                isDialogueActive = false;
                EventBus.RaiseDialogueEnd(characterId);
            });
            Debug.Log($"[NPCDialogueTrigger] Dialogue started with {speakerName}.");
        }

        public void AdvanceDialogue()
        {
            // Handled inside UIManager
        }

        public void EndDialogue()
        {
            if (isDialogueActive)
            {
                isDialogueActive = false;
                EventBus.RaiseDialogueEnd(characterId);
                Debug.Log($"[NPCDialogueTrigger] Dialogue ended with {speakerName}.");
            }
        }

        private void OnGUI()
        {
            // Suppressed in favor of high-resolution TextMeshPro UIManager
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

