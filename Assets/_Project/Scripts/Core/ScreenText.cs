using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Core
{
    public enum PanelAnchor { TopLeft, TopCenter, TopRight, Center, BottomCenter, BottomLeft, BottomRight }

    public sealed class PanelSpec
    {
        public string id;
        public string title;
        public string body;     // TextMeshPro rich text allowed (<b>, <color>, <size>)
        public string footer;   // key hints, e.g. "[J] Close"
        public PanelAnchor anchor = PanelAnchor.Center;
        public float width = 1100f; // reference pixels at 1920x1080
        public float maxHeight = 860f;
        public bool dimBackground;
        public int order;
        public int lastFrame;
    }

    public sealed class WorldLabelSpec
    {
        public string id;
        public Vector3 worldPos;
        public string text;
        public Color color = Color.white;
        public int lastFrame;
    }

    /// <summary>
    /// Engine-agnostic text request API. Gameplay scripts call Panel()/WorldLabel() every frame they want
    /// something shown (from Update/LateUpdate). LegacyOfTemples.UI.ScreenTextRenderer draws all requests
    /// with TextMeshPro, one shared font set, palette, minimum sizes and auto-layout (no overlaps).
    /// </summary>
    public static class ScreenText
    {
        public static readonly Dictionary<string, PanelSpec> Panels = new Dictionary<string, PanelSpec>();
        public static readonly Dictionary<string, WorldLabelSpec> Labels = new Dictionary<string, WorldLabelSpec>();

        public static PanelSpec Panel(string id, string title, string body, PanelAnchor anchor = PanelAnchor.Center,
            float width = 1100f, string footer = null, bool dim = false, int order = 0)
        {
            if (!Panels.TryGetValue(id, out var p)) { p = new PanelSpec { id = id }; Panels[id] = p; }
            p.title = title; p.body = body; p.anchor = anchor; p.width = width; p.footer = footer;
            p.dimBackground = dim; p.order = order; p.lastFrame = Time.frameCount;
            return p;
        }

        public static void WorldLabel(string id, Vector3 worldPos, string text, Color color)
        {
            if (!Labels.TryGetValue(id, out var l)) { l = new WorldLabelSpec { id = id }; Labels[id] = l; }
            l.worldPos = worldPos; l.text = text; l.color = color; l.lastFrame = Time.frameCount;
        }

        public static bool IsLive(int lastFrame) => lastFrame >= Time.frameCount - 1;

        /// <summary>Screen zones (1920x1080 reference units, origin bottom-left) that world labels must avoid.</summary>
        public static readonly List<Rect> Reserved = new List<Rect>();
        /// <summary>True while a big panel (tutorial, dialogue, journal, map, guide) is open.</summary>
        public static bool HideLabels;

        /// <summary>Palette (contrast >= 7:1 against the 80% black panel, measured by AuditBot).</summary>
        public const string Ivory = "#FFF4D6", Gold = "#FFC857", Cyan = "#5CE1E6", Coral = "#FFB4A8", Green = "#8FE3A8", Grey = "#D8D2C2";
        public static string Key(string k) => $"<color={Gold}><b>[{k}]</b></color>";
    }
}
