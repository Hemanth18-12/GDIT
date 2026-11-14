using LegacyOfTemples.Core;

namespace LegacyOfTemples.UI
{
    /// <summary>The one list of keys, shown by the CONTROLS button in the main menu and in the pause screen.</summary>
    public static class ControlsText
    {
        private static string K(string k) => ScreenText.Key(k);
        private static string H(string t) => $"<color={ScreenText.Gold}><b>{t}</b></color>";

        public static string Body =>
            H("MOVE") + $"   {K("W A S D")} Move   {K("Mouse")} Look   {K("Shift")} Run   {K("Space")} Jump   {K("C")} Crouch\n" +
            H("INVESTIGATE") + $"   {K("E")} Interact   {K("F")} Inspect   {K("G")} Flashlight   {K("K")} Forensic kit\n" +
            $"{K("J")} Journal   {K("M")} Map   {K("H")} Help and hints   {K("Esc")} Pause\n" +
            H("FIGHT") + $"   {K("Left click")} Attack   {K("R")} Heavy   {K("Right click")} Block   {K("V")} Dodge\n" +
            H("JEEP") + $"   {K("W S")} Drive   {K("A D")} Steer   {K("Space")} Brake   {K("E")} In / out   {K("L")} Lights\n" +
            H("GAMEPAD") + "   Left stick move, right stick look, A interact, X attack, Y heavy, LB block, B dodge, Start pause";
    }
}
