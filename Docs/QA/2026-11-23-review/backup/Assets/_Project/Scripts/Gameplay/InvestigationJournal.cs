using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    public class InvestigationJournal : MonoBehaviour
    {
        public static InvestigationJournal Instance { get; private set; }

        [System.Serializable]
        public class ClueEntry
        {
            public string id;
            public string title;
            public string category;
            public string description;
            public string detectiveNote;
            public string discoveredTime;
        }

        [System.Serializable]
        public class ObjectiveEntry
        {
            public string id;
            public string title;
            public string description;
            public bool isCompleted;
        }

        [System.Serializable]
        public class SuspectEntry
        {
            public string id;
            public string name;
            public string role;
            public string description;
            public string status;
        }

        [System.Serializable]
        public class RelicEntry
        {
            public string id;
            public string name;
            public string description;
            public bool isRecovered;
        }

        [Header("Journal Audio")]
        [SerializeField] private AudioClip openJournalSound;
        [SerializeField] private AudioClip flipTabSound;

        private AudioSource audioSource;
        private bool isOpen = false;
        public bool IsOpen => isOpen;
        private int currentTab = 0; // 0: Objectives, 1: Clues, 2: Suspects, 3: Relics

        private readonly List<ClueEntry> discoveredClues = new List<ClueEntry>();
        private readonly List<ObjectiveEntry> objectives = new List<ObjectiveEntry>();
        private readonly List<SuspectEntry> suspects = new List<SuspectEntry>();
        private readonly List<RelicEntry> relics = new List<RelicEntry>();


        public int ClueCount => discoveredClues.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            InitializeDefaultData();
        }

        private void Start()
        {
            EventBus.OnClueFound += HandleClueFound;
            EventBus.OnPuzzleSolved += HandlePuzzleSolved;
            EventBus.OnIdolRecovered += HandleRelicRecovered;
        }

        private void OnDestroy()
        {
            EventBus.OnClueFound -= HandleClueFound;
            EventBus.OnPuzzleSolved -= HandlePuzzleSolved;
            EventBus.OnIdolRecovered -= HandleRelicRecovered;
        }

        private void InitializeDefaultData()
        {
            // Initial Objectives
            objectives.Clear();
            objectives.Add(new ObjectiveEntry
            {
                id = "obj_speak_priest",
                title = "Investigate the Temple Disturbance",
                description = "Speak with Head Priest Ananda at the Courtyard steps to gather initial testimony regarding the midnight breach.",
                isCompleted = false
            });
            objectives.Add(new ObjectiveEntry
            {
                id = "obj_inspect_sanctum",
                title = "Inspect the Sanctum Relic Vault",
                description = "Enter the temple sanctum and examine the broken locks, footprints, and breached cases for physical clues.",
                isCompleted = false
            });
            objectives.Add(new ObjectiveEntry
            {
                id = "obj_file_report",
                title = "File Incident Report with Priest Ananda",
                description = "Present your preliminary deductions to the priest to pinpoint the thieves' flight direction.",
                isCompleted = false
            });
            objectives.Add(new ObjectiveEntry
            {
                id = "obj_track_thieves",
                title = "Track the Escape Trail to Outer Shrine",
                description = "Follow the muddy tracks south toward the ancient outer shrine ruins.",
                isCompleted = false
            });
            objectives.Add(new ObjectiveEntry
            {
                id = "obj_solve_chakra",
                title = "Decipher the Surya Chakra Wall",
                description = "Align the three concentric stone wheels to unseal the jungle gateway.",
                isCompleted = false
            });
            objectives.Add(new ObjectiveEntry
            {
                id = "obj_confront_thieves",
                title = "Corner the Robbers & Recover Relics",
                description = "Confront Bhaskar and Kishore at the jungle ruins and recover all 3 sacred heritage treasures.",
                isCompleted = false
            });

            // Suspects & Witnesses
            suspects.Clear();
            suspects.Add(new SuspectEntry
            {
                id = "sus_priest",
                name = "Pujari Ananda",
                role = "Head Priest / Temple Custodian",
                description = "Eldest priest of the temple sanctuary. Deeply devout and heartbroken by the theft. Asks Aryan to find the three missing storage hall pieces before the festival.",
                status = "Witness / Ally"
            });
            suspects.Add(new SuspectEntry
            {
                id = "sus_villager",
                name = "Meera",
                role = "Village Girl in Red and Green Saree",
                description = "Saw two men near the storage door before sunrise: one wearing a red scarf, the other in a black hood fleeing toward the jungle.",
                status = "Eyewitness"
            });
            suspects.Add(new SuspectEntry
            {
                id = "sus_robber1",
                name = "Bhaskar (Robber 1)",
                role = "Lead Robber (Red Scarf, Dark Vest)",
                description = "Ruthless thief seeking to sell temple pieces. Fled south into the jungle path carrying the ceremonial vessel and rosette.",
                status = "Prime Suspect (Fled South)"
            });
            suspects.Add(new SuspectEntry
            {
                id = "sus_robber2",
                name = "Kishore (Robber 2)",
                role = "Accomplice (Black Hood, Cross-Strap)",
                description = "Nervous accomplice who dropped the oil lamp leaving marks at the crime scene. Fled with Bhaskar.",
                status = "Prime Suspect (Fled South)"
            });

            // Stolen Relics (Fictional & respectful)
            relics.Clear();
            relics.Add(new RelicEntry
            {
                id = "relic_pradip",
                name = "Sacred Pradip Brass Vessel",
                description = "An intricately engraved 16th-century ceremonial brass lamp used in the high Aarti rituals. Stolen from the inner sanctum pedestal.",
                isRecovered = false
            });
            relics.Add(new RelicEntry
            {
                id = "relic_rosette",
                name = "Carved Sandstone Rosette",
                description = "An ancient Surya rosette medallion carved from rare reddish sandstone, embodying celestial solar motifs.",
                isRecovered = false
            });
            relics.Add(new RelicEntry
            {
                id = "relic_manuscript",
                name = "Ancient Tala Patra Palm-Leaf Manuscript",
                description = "Palm-leaf scripture written in ancient Odia script documenting temple consecration hymns and architectural geometry.",
                isRecovered = false
            });
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if ((kb != null && kb.jKey.wasPressedThisFrame) || VirtualInput.Down("journal"))
            {
                ToggleJournal();
            }

            var gp = Gamepad.current;
            if (gp != null && gp.selectButton.wasPressedThisFrame)
            {
                ToggleJournal();
            }

            if (isOpen && kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) SwitchTab(0);
                if (kb.digit2Key.wasPressedThisFrame) SwitchTab(1);
                if (kb.digit3Key.wasPressedThisFrame) SwitchTab(2);
                if (kb.digit4Key.wasPressedThisFrame) SwitchTab(3);
                if (kb.escapeKey.wasPressedThisFrame) { EventBus.EscapeConsumedFrame = Time.frameCount; ToggleJournal(); }
            }
        }

        public void ToggleJournal()
        {
            isOpen = !isOpen;
            EventBus.RaiseOverlayMenuToggled(isOpen);
            if (openJournalSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(openJournalSound);
            }
        }

        public void SwitchTab(int tabIndex)
        {
            if (currentTab != tabIndex)
            {
                currentTab = tabIndex;
                if (flipTabSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(flipTabSound);
                }
            }
        }

        public void RegisterClue(string id, string title, string category, string desc, string note)
        {
            foreach (var existing in discoveredClues)
            {
                if (existing.id == id) return;
            }

            discoveredClues.Add(new ClueEntry
            {
                id = id,
                title = title,
                category = category,
                description = desc,
                detectiveNote = note,
                discoveredTime = DateTime.Now.ToString("HH:mm:ss")
            });

            // If we found sanctum clues, update objective
            if (id.Contains("lock") || id.Contains("footprint") || id.Contains("fabric"))
            {
                CompleteObjective("obj_inspect_sanctum");
            }

            Debug.Log($"[InvestigationJournal] Registered clue '{title}'. Total clues logged: {discoveredClues.Count}");
        }

        public void CompleteObjective(string objectiveId)
        {
            foreach (var obj in objectives)
            {
                if (obj.id == objectiveId && !obj.isCompleted)
                {
                    obj.isCompleted = true;
                    Debug.Log($"[InvestigationJournal] Completed Objective: {obj.title}");
                }
            }
        }

        public void RecoverRelic(string relicId)
        {
            foreach (var r in relics)
            {
                if (r.id == relicId || relicId.Contains(r.id))
                {
                    r.isRecovered = true;
                    Debug.Log($"[InvestigationJournal] RECOVERED SACRED RELIC: {r.name}");
                }
            }
        }

        private void HandleClueFound(string clueId)
        {
            // Automatic hook from EventBus
        }

        private void HandlePuzzleSolved(string puzzleId)
        {
            CompleteObjective("obj_solve_chakra");
        }

        private void HandleRelicRecovered(string relicId)
        {
            RecoverRelic(relicId);
        }

        public float CalculateProgressPercentage()
        {
            int totalItems = objectives.Count + 4 + relics.Count; // 6 objectives + 4 clues + 3 relics = 13 items
            int completedCount = 0;
            foreach (var o in objectives) if (o.isCompleted) completedCount++;
            completedCount += Mathf.Min(discoveredClues.Count, 4);
            foreach (var r in relics) if (r.isRecovered) completedCount++;

            return Mathf.Clamp01((float)completedCount / totalItems) * 100f;
        }

        private void LateUpdate()
        {
            if (!isOpen) return;
            string[] tabs = { "Goals", "Clues", "People", "Pieces" };
            var head = new System.Text.StringBuilder();
            for (int i = 0; i < tabs.Length; i++)
                head.Append(i == currentTab ? $"<color={ScreenText.Gold}><b><u>[{i + 1}] {tabs[i]}</u></b></color>   " : $"[{i + 1}] {tabs[i]}   ");
            head.Append("\n\n");
            var sb = new System.Text.StringBuilder(head.ToString());
            switch (currentTab)
            {
                case 0:
                    var story = StoryDialogueSystem.Instance;
                    var objs = StoryData.Objectives;
                    for (int i = 0; i < objs.Length; i++)
                    {
                        if (story != null && i > story.ObjectiveIndex) break;
                        bool done = story != null && i < story.ObjectiveIndex;
                        sb.Append(done ? $"<color={ScreenText.Green}>Done</color>  " : $"<color={ScreenText.Gold}>Now</color>  ")
                          .Append("<b>").Append(objs[i].title).Append("</b>\n");
                        if (!done) sb.Append(objs[i].instruction).Append('\n');
                    }
                    break;
                case 1:
                    if (discoveredClues.Count == 0) sb.Append("No clues yet. Look for glowing objects and press [F].\n");
                    foreach (var c in discoveredClues)
                        sb.Append($"<color={ScreenText.Cyan}><b>{c.title}</b></color>\n{c.description}\n<color={ScreenText.Gold}>Note:</color> {c.detectiveNote}\n\n");
                    break;
                case 2:
                    foreach (var p in suspects)
                        sb.Append($"<b>{p.name}</b>, {p.role}\n{p.description}\n\n");
                    break;
                case 3:
                    foreach (var r in relics)
                        sb.Append($"<b>{r.name}</b>  {(r.isRecovered ? $"<color={ScreenText.Green}>Recovered</color>" : $"<color={ScreenText.Coral}>Missing</color>")}\n{r.description}\n\n");
                    break;
            }
            ScreenText.Panel("journal", "CASE JOURNAL", sb.ToString(), PanelAnchor.Center, 1300f,
                ScreenText.Key("1-4") + " Tabs    " + ScreenText.Key("J") + " / " + ScreenText.Key("Esc") + " Close", true, 60);
        }

    }
}

