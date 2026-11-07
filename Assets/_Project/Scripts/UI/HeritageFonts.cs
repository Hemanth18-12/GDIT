using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Game fonts (SIL Open Font License, see ATTRIBUTION.md): Cinzel for titles, Noto Sans for body text.
    /// TextMeshPro SDF font assets are generated at runtime from Resources/Fonts/*.ttf (dynamic atlas).
    /// </summary>
    public static class HeritageFonts
    {
        private static TMP_FontAsset body, title;

        public static TMP_FontAsset Body => body != null ? body : (body = Make("Fonts/NotoSans") ?? TMP_Settings.defaultFontAsset);
        public static TMP_FontAsset Title => title != null ? title : (title = Make("Fonts/Cinzel") ?? Body);

        private static TMP_FontAsset Make(string path)
        {
            var f = Resources.Load<Font>(path);
            if (f == null) { Debug.LogWarning("[HeritageFonts] Missing " + path); return null; }
            var fa = TMP_FontAsset.CreateFontAsset(f, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fa == null) return null;
            fa.name = f.name + " SDF (runtime)";
            if (TMP_Settings.defaultFontAsset != null && fa != TMP_Settings.defaultFontAsset)
            {
                if (fa.fallbackFontAssetTable == null) fa.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                fa.fallbackFontAssetTable.Add(TMP_Settings.defaultFontAsset);
            }
            return fa;
        }

        /// <summary>Applies the font and a thin dark outline (about 2 px at 1080p) for readability over 3D scenes.</summary>
        public static void Apply(TMP_Text t, bool isTitle)
        {
            if (t == null) return;
            var fa = isTitle ? Title : Body;
            if (fa != null && t.font != fa) t.font = fa;
            t.outlineWidth = 0.12f;
            t.outlineColor = new Color32(0, 0, 0, 255);
        }
    }
}
