using LegacyOfTemples.Core;

namespace LegacyOfTemples.UI
{
    /// <summary>The one list of keys, shown by the CONTROLS button in the main menu and in the pause screen.</summary>
    public static class ControlsText
    {
        private static string K(string k) => ScreenText.Key(k);
        private static string H(string t) => $"<color={ScreenText.Gold}><b>{t}</b></color>";

        public static string Body =>
            H("MOVE") + "\n" +
            $"{K("W A S D")} Move      {K("Mouse")} Look      {K("Shift")} Run      {K("Space")} Jump      {K("C")} Crouch\n\n" +
            H("INVESTIGATE") + "\n" +
            $"{K("E")} Interact / talk      {K("F")} Inspect a clue      {K("G")} Flashlight      {K("K")} Forensic kit\n" +
            $"{K("J")} Journal      {K("M")} Map      {K("H")} Help and hints      {K("Esc")} Pause\n\n" +
            H("FIGHT") + "\n" +
            $"{K("Left click")} Attack      {K("R")} Heavy      {K("Right click")} Block      {K("V")} Dodge      {K("G")} Flash\n\n" +
            H("JEEP") + "\n" +
            $"{K("W S")} Drive / reverse      {K("A D")} Steer      {K("Space")} Brake      {K("E")} Get in / out      {K("L")} Lights\n\n" +
            H("GAMEPAD") + "\n" +
            "Left stick move, right stick look, A interact / jump, X attack, Y heavy, LB block, B dodge, Start pause";
    }
}
