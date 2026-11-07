using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Gameplay
{
    public class StoryDialogueSystem : MonoBehaviour
    {
        public static StoryDialogueSystem Instance { get; private set; }

        private OrbitCamera orbitCam;
        private Transform playerTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            FindSceneReferences();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindSceneReferences();
        }

        private void FindSceneReferences()
        {
            orbitCam = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        public bool HandleNPCInteraction(string characterId, string speakerName, Transform npcTransform)
        {
            string currentScene = SceneManager.GetActiveScene().name;

            // Trigger NPC talk animation
            if (npcTransform != null)
            {
                Animator anim = npcTransform.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.SetBool("IsTalking", true);
                }
            }

            switch (currentScene)
            {
                case Constants.Courtyard:
                    return TriggerCourtyardDialogue(characterId, npcTransform);

                case Constants.Sanctum:
                    return TriggerSanctumDialogue(characterId, npcTransform);

                case Constants.ShrineCrime:
                    return TriggerShrineCrimeDialogue(characterId, npcTransform);

                case Constants.PuzzleWall:
                    return TriggerPuzzleWallDialogue(characterId, npcTransform);

                case Constants.JunglePath:
                    return TriggerJunglePathDialogue(characterId, npcTransform);

                default:
                    return false;
            }
        }

        #region Scene 1: Courtyard
        private bool TriggerCourtyardDialogue(string characterId, Transform npcTransform)
        {
            FrameSpeaker(npcTransform);

            if (characterId == Constants.Priest || characterId == "priest")
            {
                string[] lines = new string[]
                {
                    "Pujari Ananda: Namaste. Three pieces from our storage hall are gone: a brass ceremonial vessel, a carved sandstone rosette, and a palm-leaf manuscript. Please find them before the festival.",
                    "Aryan: Stay calm and touch nothing. Every small detail matters. Did anyone see something?",
                    "Meera: Before sunrise I saw two men near the storage door. One wore a red scarf, the other a black hood."
                };

                string[] choices = new string[]
                {
                    "Ask Meera which way they ran",
                    "Ask the Pujari why the pieces matter"
                };

                EventBus.RaiseDialogueStart(characterId);
                EventBus.RaiseRequestDialogue("Pujari Ananda", lines, choices, (choiceIndex) =>
                {
                    if (choiceIndex == 0)
                    {
                        string[] followUp = new string[]
                        {
                            "Meera: They fled past the outer shrine gates heading southward toward the dense jungle path!"
                        };
                        EventBus.RaiseRequestDialogue("Meera", followUp, null, null, () => FinishDialogue(npcTransform, characterId));
                    }
                    else
                    {
                        string[] followUp = new string[]
                        {
                            "Pujari Ananda: They carry the community's living memory. Their value isn't wealth or gold—they are the sacred heritage of our people."
                        };
                        EventBus.RaiseRequestDialogue("Pujari Ananda", followUp, null, null, () => FinishDialogue(npcTransform, characterId));
                    }
                }, () => FinishDialogue(npcTransform, characterId));

                return true;
            }
            else if (characterId == Constants.Villager || characterId == "villager")
            {
                string[] lines = new string[]
                {
                    "Meera: Before sunrise I saw two men near the storage door. One wore a red scarf, the other a black hood.",
                    "Aryan: Did you see where they went?",
                    "Meera: They carried heavy burlap sacks through the outer shrine toward the dense jungle path!"
                };

                EventBus.RaiseDialogueStart(characterId);
                EventBus.RaiseRequestDialogue("Meera", lines, null, null, () => FinishDialogue(npcTransform, characterId));
                return true;
            }

            return false;
        }
        #endregion

        #region Scene 2: Sanctum
        private bool TriggerSanctumDialogue(string characterId, Transform npcTransform)
        {
            FrameSpeaker(npcTransform);

            string[] lines = new string[]
            {
                "Aryan: Three empty cloth-lined stands. Whoever came knew the hall.",
                "Pujari Ananda: Only temple staff know where the key is kept. The brass lock was shattered from the outside."
            };

            EventBus.RaiseDialogueStart(characterId);
            EventBus.RaiseRequestDialogue("Sanctum Vault Investigation", lines, null, null, () => FinishDialogue(npcTransform, characterId));
            return true;
        }
        #endregion

        #region Scene 3: Shrine Crime
        private bool TriggerShrineCrimeDialogue(string characterId, Transform npcTransform)
        {
            FrameSpeaker(npcTransform);

            string[] lines = new string[]
            {
                "Aryan: Muddy bootprints, two different sizes, and red silk caught on the door.",
                "Pujari Ananda: That cloth is not ours.",
                "Aryan: Two suspects breached the shrine—one in a red scarf, one in a black hood. Their trail leads into the jungle."
            };

            EventBus.RaiseDialogueStart(characterId);
            EventBus.RaiseRequestDialogue("Crime Scene Analysis", lines, null, null, () => FinishDialogue(npcTransform, characterId));
            return true;
        }
        #endregion

        #region Scene 4: Puzzle Wall
        private bool TriggerPuzzleWallDialogue(string characterId, Transform npcTransform)
        {
            FrameSpeaker(npcTransform);

            string[] lines = new string[]
            {
                "Pujari Ananda: My grandfather spoke of a wall that opens when four symbols face the rising sun.",
                "Ancient Scroll: Lotus, conch, wheel, kalash, then turn the centre wheel.",
                "Aryan: Align the four solar stone panels in order, then turn the centre wheel to unlock the passage."
            };

            EventBus.RaiseDialogueStart(characterId);
            EventBus.RaiseRequestDialogue("Surya Chakra Mystery", lines, null, null, () => FinishDialogue(npcTransform, characterId));
            return true;
        }

        public void TriggerPuzzleWallSolved()
        {
            string[] lines = new string[]
            {
                "Aryan: The passage is open. They went this way."
            };

            EventBus.RaiseRequestDialogue("Investigator Aryan", lines, null, null, null);
        }
        #endregion

        #region Scene 5: Jungle Path
        private bool TriggerJunglePathDialogue(string characterId, Transform npcTransform)
        {
            FrameSpeaker(npcTransform);

            string[] lines = new string[]
            {
                "Bhaskar (Robber 1): Keep moving. The investigator is close.",
                "Kishore (Robber 2): I told you the lamp would leave marks.",
                "Aryan: It's over. Return what belongs to the community.",
                "Bhaskar (Robber 1): Do you know what these fetch?",
                "Aryan: Their value isn't money. It's memory."
            };

            EventBus.RaiseDialogueStart(characterId);
            EventBus.RaiseRequestDialogue("Jungle Ruins Confrontation", lines, null, null, () =>
            {
                FinishDialogue(npcTransform, characterId);
                // Trigger peaceful recovery and Mission Completed ending
                if (GameLevelManager.Instance != null)
                {
                    GameLevelManager.Instance.CompleteLevel3();
                }
            });

            return true;
        }
        #endregion

        private void FrameSpeaker(Transform npcTransform)
        {
            if (orbitCam == null || npcTransform == null) return;

            Vector3 speakerHead = npcTransform.position + Vector3.up * 1.5f;
            Vector3 camPos = speakerHead + npcTransform.forward * 2.2f + Vector3.up * 0.2f;
            Quaternion camRot = Quaternion.LookRotation(speakerHead - camPos);
            orbitCam.SetCutsceneOverride(camPos, camRot);
        }

        private void FinishDialogue(Transform npcTransform, string characterId)
        {
            if (orbitCam != null)
            {
                orbitCam.ClearCutsceneOverride();
            }

            if (npcTransform != null)
            {
                Animator anim = npcTransform.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.SetBool("IsTalking", false);
                }
            }

            EventBus.RaiseDialogueEnd(characterId);
        }
    }
}

