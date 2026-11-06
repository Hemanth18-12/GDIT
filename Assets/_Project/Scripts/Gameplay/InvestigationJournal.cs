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
        private int currentTab = 0; // 0: Objectives, 1: Clues, 2: Suspects, 3: Relics

        private readonly List<ClueEntry> discoveredClues = new List<ClueEntry>();
        private readonly List<ObjectiveEntry> objectives = new List<ObjectiveEntry>();
        private readonly List<SuspectEntry> suspects = new List<SuspectEntry>();
        private readonly List<RelicEntry> relics = new List<RelicEntry>();

        private Vector2 scrollPos;
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle tabActiveStyle;
        private GUIStyle tabInactiveStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle itemTitleStyle;
        private GUIStyle itemBodyStyle;
        private GUIStyle statusStyle;
        private GUIStyle footerStyle;

        public bool IsOpen => isOpen;
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
                title = "Corner the Smugglers & Recover Relics",
                description = "Confront Kala and Rudra at the jungle ruins and recover all 3 sacred heritage treasures.",
                isCompleted = false
            });

            // Suspects & Witnesses
            suspects.Clear();
            suspects.Add(new SuspectEntry
            {
                id = "sus_priest",
                name = "Priest Ananda",
                role = "Head Pujari / Temple Custodian",
                description = "Eldest priest of the Jagannath sanctuary. Deeply devout and heartbroken by the sacrilege. First to discover the broken storage doors at dawn.",
                status = "Witness / Ally"
            });
            suspects.Add(new SuspectEntry
            {
                id = "sus_villager",
                name = "Meera Devi",
                role = "Devoted Village Pilgrim",
                description = "Carries temple flowers every morning. Heard heavy hurried steps and saw two masked silhouettes fleeing south into the mist before sunrise.",
                status = "Eyewitness"
            });
            suspects.Add(new SuspectEntry
            {
                id = "sus_robber1",
                name = "Kala (The Corrupted King)",
                role = "Lead Smuggler / Antiquities Thief",
                description = "Ruthless black-market operative seeking ancient sacred artifacts for illegal offshore collectors. Known to carry heavy chisels.",
                status = "Prime Suspect (Fled South)"
            });
            suspects.Add(new SuspectEntry
            {
                id = "sus_robber2",
                name = "Rudra (The Winged Accomplice)",
                role = "Accomplice / Scout",
                description = "Agile thief who bypassed the high perimeter wall and unlatched the inner iron bolts from above.",
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
            if (kb != null && kb.jKey.wasPressedThisFrame)
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
                if (kb.escapeKey.wasPressedThisFrame) ToggleJournal();
            }
        }

        public void ToggleJournal()
        {
            isOpen = !isOpen;
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
                scrollPos = Vector2.zero;
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

        private void OnGUI()
        {
            if (!isOpen) return;

            InitStyles();

            float winWidth = Mathf.Min(820f, Screen.width - 40f);
            float winHeight = Mathf.Min(560f, Screen.height - 40f);
            float winX = (Screen.width - winWidth) * 0.5f;
            float winY = (Screen.height - winHeight) * 0.5f;

            GUI.Box(new Rect(winX, winY, winWidth, winHeight), GUIContent.none, windowStyle);

            // Title Header
            GUI.Label(new Rect(winX + 24f, winY + 16f, winWidth - 48f, 28f), "CASE DOSSIER: THE MISSING TEMPLE RELICS", headerStyle);
            float progress = CalculateProgressPercentage();
            GUI.Label(new Rect(winX + winWidth - 220f, winY + 20f, 196f, 20f), $"Investigation Progress: {progress:F0}%", statusStyle);

            // Tabs
            string[] tabs = { "[1] Objectives", "[2] Clues & Evidence", "[3] Suspects & Witnesses", "[4] Sacred Relics" };
            float tabWidth = (winWidth - 48f) / tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
            {
                Rect tabRect = new Rect(winX + 24f + i * tabWidth, winY + 52f, tabWidth - 4f, 32f);
                GUIStyle style = (i == currentTab) ? tabActiveStyle : tabInactiveStyle;
                if (GUI.Button(tabRect, tabs[i], style))
                {
                    SwitchTab(i);
                }
            }

            // Scrollable Content Area
            Rect viewRect = new Rect(winX + 24f, winY + 92f, winWidth - 48f, winHeight - 140f);
            GUILayout.BeginArea(viewRect);
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            switch (currentTab)
            {
                case 0:
                    DrawObjectivesTab();
                    break;
                case 1:
                    DrawCluesTab();
                    break;
                case 2:
                    DrawSuspectsTab();
                    break;
                case 3:
                    DrawRelicsTab();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Footer
            GUI.Label(new Rect(winX + 24f, winY + winHeight - 34f, winWidth - 48f, 22f), "Press [J] or [Esc] to Close Dossier  •  Keys [1]-[4] to switch tabs", footerStyle);
        }

        private void DrawObjectivesTab()
        {
            GUILayout.Label("CASE OBJECTIVES", sectionHeaderStyle);
            GUILayout.Space(8f);

            foreach (var obj in objectives)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                string mark = obj.isCompleted ? "<color=#4cd137>✓ COMPLETED</color>" : "<color=#fbc531>○ ACTIVE</color>";
                GUILayout.Label($"{mark}  <b>{obj.title}</b>", itemTitleStyle);
                GUILayout.Label(obj.description, itemBodyStyle);
                GUILayout.EndVertical();
                GUILayout.Space(6f);
            }
        }

        private void DrawCluesTab()
        {
            GUILayout.Label($"FORENSIC EVIDENCE ({discoveredClues.Count} Discovered)", sectionHeaderStyle);
            GUILayout.Space(8f);

            if (discoveredClues.Count == 0)
            {
                GUILayout.Label("No physical clues inspected yet. Search the Courtyard and Sanctum rooms carefully.", itemBodyStyle);
                return;
            }

            foreach (var clue in discoveredClues)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"<b>{clue.title}</b> <color=#e1b12c>[{clue.category}]</color>", itemTitleStyle);
                GUILayout.Label(clue.description, itemBodyStyle);
                GUILayout.Label($"<color=#f5cd79>Deduction:</color> <i>\"{clue.detectiveNote}\"</i>", itemBodyStyle);
                GUILayout.Label($"<color=#718093>Logged at: {clue.discoveredTime}</color>", footerStyle);
                GUILayout.EndVertical();
                GUILayout.Space(6f);
            }
        }

        private void DrawSuspectsTab()
        {
            GUILayout.Label("PERSONS OF INTEREST", sectionHeaderStyle);
            GUILayout.Space(8f);

            foreach (var s in suspects)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"<b>{s.name}</b> — <color=#00a8ff>{s.role}</color>  [<color=#e84118>{s.status}</color>]", itemTitleStyle);
                GUILayout.Label(s.description, itemBodyStyle);
                GUILayout.EndVertical();
                GUILayout.Space(6f);
            }
        }

        private void DrawRelicsTab()
        {
            GUILayout.Label("STOLEN HERITAGE ARTIFACTS", sectionHeaderStyle);
            GUILayout.Space(8f);

            foreach (var r in relics)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                string status = r.isRecovered ? "<color=#4cd137>★ RECOVERED SECURED</color>" : "<color=#e84118>⚠ STOLEN / IN POSSESSION OF SMUGGLERS</color>";
                GUILayout.Label($"<b>{r.name}</b>  {status}", itemTitleStyle);
                GUILayout.Label(r.description, itemBodyStyle);
                GUILayout.EndVertical();
                GUILayout.Space(6f);
            }
        }

        private void InitStyles()
        {
            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(GUI.skin.box);
            }

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.96f, 0.82f, 0.28f) }
                };
            }

            if (tabActiveStyle == null)
            {
                tabActiveStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.yellow }
                };
            }

            if (tabInactiveStyle == null)
            {
                tabInactiveStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    normal = { textColor = Color.white }
                };
            }

            if (sectionHeaderStyle == null)
            {
                sectionHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.85f, 0.7f, 0.3f) }
                };
            }

            if (itemTitleStyle == null)
            {
                itemTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    richText = true,
                    normal = { textColor = Color.white }
                };
            }

            if (itemBodyStyle == null)
            {
                itemBodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true,
                    richText = true,
                    normal = { textColor = new Color(0.88f, 0.88f, 0.88f) }
                };
            }

            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(0.4f, 0.9f, 0.4f) }
                };
            }

            if (footerStyle == null)
            {
                footerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
            }
        }
    }
}

