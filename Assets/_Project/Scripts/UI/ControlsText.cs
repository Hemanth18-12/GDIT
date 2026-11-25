using LegacyOfTemples.Core;

namespace LegacyOfTemples.UI
{
    /// <summary>The one list of keys, shown by the CONTROLS button in the main menu and in the pause screen.</summary>
    public static class ControlsText
    {
        private static string K(string k) => ScreenText.Key(k);
        private static string H(string t) => $"<color={ScreenText.Gold}><b>{t}</b></color>";

        public static string Body =>
            H("MOVE") + $"   {K("W A S D")} Move   {K("Mouse")} Look   {K("Shift")} Run   {K("Caps Lock")} Auto-run   {K("Space")} Jump   {K("C")} Crouch\n" +
            H("SOLVE") + $"   {K("E")} Grab, turn, push, place, talk   {K("H")} Guide and hints   {K("B")} Evidence board   {K("J")} Journal\n" +
            $"{K("M")} Map and fast travel ({K("1-8")})   {K("K")} Skip task   {K("T")} Return carried relic (TempleRing)   {K("Esc")} Pause\n" +
            H("FIGHT") + $"   {K("Left click")} Strike   {K("R")} Heavy   {K("Q")} Block (hold; tap to parry)   {K("V")} Dodge   {K("F")} Finish\n" +
            H("JEEP") + $"   {K("W S")} Drive   {K("A D")} Steer   {K("Space")} Brake   {K("E")} In / out   {K("L")} Lights   {K("G")} Horn\n" +
            H("GAMEPAD") + "   Left stick move, right stick look, A interact, X strike, Y heavy, LB block, B dodge, RB finish, Start pause";
    }
}
