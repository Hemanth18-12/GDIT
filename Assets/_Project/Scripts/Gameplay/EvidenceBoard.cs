using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay.Puzzles;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// The journal's centre page (B): only what the player has actually found is listed. With three of the four
    /// pieces of evidence the player may name who opened the storeroom from the inside. Nobody tells them the answer;
    /// naming the right person before the final zone changes the last fight (see Docs/STORY_V3.md).
    /// </summary>
    public sealed class EvidenceBoard : MonoBehaviour
    {
        public static readonly (string key, string zone, string text, string meaning)[] Pieces =
        {
            ("evidence.list", "Bazaar", "The courier's list: five items, neat handwriting, ticks of two short strokes.", "Someone who writes lists wrote it."),
            ("evidence.inside_lock", "Courtyard", "The storeroom lock is scratched on the inside. Lamp oil on the key hook.", "Opened with the real key, by someone who handles lamps."),
            ("evidence.ledger", "Vaults", "The vault duty ledger: the same handwriting, the same ticks, signed \"M.\"", "The list writer keeps the temple ledgers."),
            ("evidence.key_ring", "Stepwell", "A temple key ring and an oil-stained cloth in the hidden chamber.", "The key-keeper was there tonight."),
        };
        public static readonly string[] Suspects = { "Pujari Ananda", "Meera", "Madhav, the key-keeper", "A stranger from outside" };
        public const int InsideMan = 2, NeededToName = 3;
        const string NamedKey = "evidence.named";

        public static int Found { get { int n = 0; foreach (var p in Pieces) if (PuzzleSave.Get(p.key) == 1) n++; return n; } }
        public static bool CanName => Found >= NeededToName && Named < 0;
        /// <summary>Index of the suspect the player named, or -1.</summary>
        public static int Named => PuzzleSave.Get(NamedKey, 0) - 1;
        public static bool NamedCorrectly => Named == InsideMan;
        public static bool Name(int suspect)
        {
            if (!CanName || suspect < 0 || suspect >= Suspects.Length) return false;
            PuzzleSave.Set(NamedKey, suspect + 1); Debug.Log($"[Evidence] the player named {Suspects[suspect]} (correct={suspect == InsideMan}) with {Found} pieces");
            return true;
        }

        public static EvidenceBoard Instance { get; private set; }
        public bool IsOpen { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (Instance == null) { var g = new GameObject("EvidenceBoard"); DontDestroyOnLoad(g); Instance = g.AddComponent<EvidenceBoard>(); } }

        public void Toggle() { IsOpen = !IsOpen; }
        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.bKey.wasPressedThisFrame && !InputGate.GameplayBlocked) Toggle();
            if (!IsOpen) return;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { IsOpen = false; return; }
                if (CanName) { if (kb.digit1Key.wasPressedThisFrame) Name(0); else if (kb.digit2Key.wasPressedThisFrame) Name(1); else if (kb.digit3Key.wasPressedThisFrame) Name(2); else if (kb.digit4Key.wasPressedThisFrame) Name(3); }
            }
            ScreenText.Panel("v3_evidence", "WHAT I KNOW", Body(), PanelAnchor.Center, 1150f, $"{ScreenText.Key("B")} Close", true, 60);
        }

        /// <summary>The page text: found evidence only, the recovered treasures and the score so far.</summary>
        public static string Body()
        {
            var sb = new StringBuilder();
            sb.Append($"<color={ScreenText.Gold}><b>WHO OPENED THE DOOR?</b></color>\n");
            int found = 0;
            foreach (var p in Pieces) if (PuzzleSave.Get(p.key) == 1) { found++; sb.Append($"<color={ScreenText.Cyan}>{p.zone}</color>  {p.text}\n<color={ScreenText.Grey}><i>{p.meaning}</i></color>\n"); }
            if (found == 0) sb.Append("<i>Nothing yet. The answer is in what the robbers left behind.</i>\n");
            for (int i = found; i < Pieces.Length; i++) sb.Append($"<color={ScreenText.Grey}>• ...</color>\n");
            sb.Append('\n');
            if (Named >= 0) sb.Append($"<color={ScreenText.Gold}><b>I HAVE NAMED:</b></color> {Suspects[Named]}\n");
            else if (CanName) { sb.Append($"<color={ScreenText.Gold}><b>NAME THE INSIDE MAN</b></color> (one choice, it cannot be changed)\n"); for (int i = 0; i < Suspects.Length; i++) sb.Append($"{ScreenText.Key((i + 1).ToString())} {Suspects[i]}\n"); }
            else sb.Append($"<i>{NeededToName - found} more piece(s) of evidence before I can name anyone.</i>\n");
            sb.Append($"\n<color={ScreenText.Gold}><b>RECOVERED</b></color>  ");
            int items = 0; foreach (var z in ScoreSystem.Zones) if (z.zone != SunWallPuzzle.ZoneId) items += z.items;
            sb.Append($"{items} of 5     <color={ScreenText.Gold}><b>SCORE</b></color>  {ScoreSystem.Total}\n");
            foreach (var z in ScoreSystem.Zones) if (z.puzzleSolved || z.items > 0) sb.Append($"{z.zone}: {ScoreSystem.Points(z)} points, grade {ScoreSystem.Grade(z)}{(z.hintsUsed > 0 ? $", {z.hintsUsed} hint(s)" : "")}\n");
            return sb.ToString();
        }
    }
}
