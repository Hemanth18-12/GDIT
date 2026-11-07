using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    public class HelpHintSystem : MonoBehaviour
    {
        public static HelpHintSystem Instance { get; private set; }

        private bool isOpen = false;
        private int currentHintTier = 1; // 1 to 3

        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle hintHeaderStyle;
        private GUIStyle hintBodyStyle;
        private GUIStyle buttonStyle;
        private GUIStyle footerStyle;

        public bool IsOpen => isOpen;

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

        private void Update()
        {
            if (enabled) return; // H / Tab now open the GuideSystem panel
            var kb = Keyboard.current;
            if (kb != null && kb.hKey.wasPressedThisFrame)
            {
                ToggleHints();
            }

            var gp = Gamepad.current;
            if (gp != null && gp.dpad.up.wasPressedThisFrame)
            {
                ToggleHints();
            }

            if (isOpen && kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) ToggleHints();
                if (kb.spaceKey.wasPressedThisFrame && currentHintTier < 3)
                {
                    currentHintTier++;
                }
            }
        }

        public void ToggleHints()
        {
            isOpen = !isOpen;
            EventBus.RaiseOverlayMenuToggled(isOpen);
            if (isOpen)
            {
                // Reset to tier 1 when opening in a new scene
            }
        }

        private void LegacyOnGUI()
        {
            if (enabled) return; // replaced by TextMeshPro GuideSystem / ScreenTextRenderer
            if (!isOpen) return;

            InitStyles();

            float scale = Mathf.Max(1.0f, Screen.height / 1080f);
            float width = Mathf.Min(980f * scale, Screen.width - 40f);
            float height = Mathf.Min(560f * scale, Screen.height - 40f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, boxStyle);

            string scene = SceneManager.GetActiveScene().name;
            GUI.Label(new Rect(x + 30f, y + 20f, width - 60f, 44f), "DETECTIVE'S FIELD NOTEBOOK: PROGRESSIVE HINTS", titleStyle);
            GUI.Label(new Rect(x + 30f, y + 68f, width - 60f, 32f), $"Current Investigation Zone: <color=#f4c430>{scene}</color>  (Tier {currentHintTier} of 3)", hintHeaderStyle);

            string[] hints = GetHintsForScene(scene);

            float curY = y + 110f;
            for (int i = 0; i < currentHintTier; i++)
            {
                string tierTitle = i == 0 ? "Tier 1 — General Observation:" : (i == 1 ? "Tier 2 — Specific Location:" : "Tier 3 — Direct Deduction:");
                GUI.Label(new Rect(x + 30f, curY, width - 60f, 30f), $"<color=#f5cd79><b>{tierTitle}</b></color>", hintHeaderStyle);
                curY += 32f;
                GUI.Label(new Rect(x + 30f, curY, width - 60f, 65f), hints[i], hintBodyStyle);
                curY += 70f;
            }

            // Button to reveal next hint tier
            if (currentHintTier < 3)
            {
                if (GUI.Button(new Rect(x + width * 0.5f - 160f, y + height - 85f, 320f, 44f), $"Reveal Next Hint ({currentHintTier + 1}/3)", buttonStyle))
                {
                    currentHintTier++;
                }
            }
            else
            {
                GUI.Label(new Rect(x + 30f, y + height - 82f, width - 60f, 32f), "<color=#4cd137>All field guidance for this sector revealed.</color>", footerStyle);
            }

            GUI.Label(new Rect(x + 30f, y + height - 38f, width - 60f, 28f), "Press [H] or [Esc] to Close  •  [J] Investigation Dossier", footerStyle);
        }

        private string[] GetHintsForScene(string scene)
        {
            switch (scene)
            {
                case "Courtyard":
                    return new string[]
                    {
                        "Speak with Head Priest Ananda on the temple plinth steps and witness Meera Devi near the eastern colonnade to gather preliminary statements.",
                        "Follow the stone steps directly up into the Jagannath Vimana entrance to access the inner Sanctum Sanctorum.",
                        "Walk through the central temple entrance portal. The red carpet corridor leads straight to the sanctum storage vault where the breach occurred."
                    };

                case "Sanctum":
                    return new string[]
                    {
                        "The intruders left physical marks when forcing the ceremonial storage chambers open. Search the perimeter floor and altar cases.",
                        "Inspect the Broken Brass Sanctum Lock on the eastern plinth, and examine the muddy reddish clay footprints trailing toward the south exit.",
                        "After logging all 3 physical clues in your Dossier [J], speak with Priest Ananda inside the sanctum to file the official incident report and deduce the flight trail to the Outer Shrine."
                    };

                case "ShrineCrime":
                    return new string[]
                    {
                        "The thieves dropped hasty items while fleeing through the ruined outer courtyard. Search around the fallen pillar bases.",
                        "Find the torn dark cloak wool fragment caught on the carved plinth, and retrieve the Smuggler's Egress Route sketch from the old stone urn.",
                        "Follow the glowing red lantern trail west toward the Ancient Surya Chakra Wall to pursue Bhaskar and Kishore into the jungle."
                    };

                case "PuzzleWall":
                    return new string[]
                    {
                        "The ancient stone gateway is sealed by three concentric Surya Chakra solar discs that must be aligned in harmony.",
                        "Use keys [1], [2], [3] to select the Outer, Middle, or Inner stone wheels, then use [A]/[D] or [Left]/[Right] to rotate them until the sacred solar notches lock.",
                        "Align the outer 12 solar spokes with the middle 8 lunar arcs and the inner lotus medallion. When all three chime in alignment, the ancient portal opens to the Jungle Path."
                    };

                case "JunglePath":
                    return new string[]
                    {
                        "Follow the torchlit jungle path past the banyan roots to the ruined forest pavilion.",
                        "Bhaskar and Kishore are cornered ahead with the stolen relics. Approach and confront them directly.",
                        "Present the broken lock evidence and stolen cloth in dialogue to secure their non-violent surrender, recover all 3 sacred relics, and solve the Heritage Mystery!"
                    };

                default:
                    return new string[]
                    {
                        "Explore the area and look for interactable objects highlighted with the [E] prompt.",
                        "Check your Investigation Dossier [J] to review active objectives.",
                        "Speak with nearby witnesses or follow obvious trails of footprints."
                    };
            }
        }

        private void InitStyles()
        {
            float scale = Mathf.Max(1.0f, Screen.height / 1080f);

            if (boxStyle == null) boxStyle = new GUIStyle(GUI.skin.box);

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(38 * scale),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1.0f, 0.784f, 0.341f) } // #FFC857
                };
            }

            if (hintHeaderStyle == null)
            {
                hintHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(34 * scale),
                    fontStyle = FontStyle.Bold,
                    richText = true,
                    normal = { textColor = new Color(1.0f, 0.784f, 0.341f) } // #FFC857
                };
            }

            if (hintBodyStyle == null)
            {
                hintBodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(30 * scale),
                    wordWrap = true,
                    normal = { textColor = new Color(1.0f, 0.957f, 0.839f) } // #FFF4D6
                };
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = Mathf.RoundToInt(30 * scale),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1.0f, 0.784f, 0.341f) }
                };
            }

            if (footerStyle == null)
            {
                footerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(26 * scale),
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    normal = { textColor = new Color(1.0f, 0.784f, 0.341f) }
                };
            }
        }
    }
}

