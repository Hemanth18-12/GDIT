using UnityEngine;
using TMPro;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Original modular market architecture, dressed with the already attributed Poly Haven materials and props.</summary>
    public static class MarketArt
    {
        public static GameObject Box(Transform p, string n, Vector3 at, Vector3 size, string mat, bool solid = true) => Village.Box(p, n, at, size, mat, 2f, solid);
        public static void Sign(Transform p, Vector3 at, string words, float width = 3f, float yaw = 0f)
        {
            var b = Box(p, "Painted timber sign", at, new Vector3(width, 0.65f, 0.08f), "weathered_planks", false); b.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var text = new GameObject("Hand painted lettering").AddComponent<TextMeshPro>(); text.transform.SetParent(p, false);
            text.transform.localPosition = at + Quaternion.Euler(0f, yaw, 0f) * Vector3.back * 0.07f;
            text.transform.localRotation = Quaternion.Euler(0f, yaw, 0f); text.rectTransform.sizeDelta = new Vector2(width * 0.94f, 0.6f);
            text.fontSize = 3.5f; text.enableAutoSizing = true; text.fontSizeMin = 1f; text.fontSizeMax = 3.5f;
            text.alignment = TextAlignmentOptions.Center; text.color = new Color(1f, 0.92f, 0.68f); text.text = words;
        }
        public static void House(Transform p, Vector3 at, float yaw, Color tint, string name, int variant)
        {
            var root = new GameObject(name).transform; root.SetParent(p, false); root.localPosition = at; root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var plaster = ZoneKit.Tinted("yellow_plaster", tint, 0.2f);
            ZoneKit.Part(root, "Ochre walls", new Vector3(0f, 3.1f, 0f), new Vector3(7.8f, 6.2f, 8f), plaster);
            Box(root, "Stone footing", new Vector3(0f, 0.3f, -0.1f), new Vector3(8.1f, 0.6f, 8.2f), "sandstone_blocks_08");
            for (int side = -1; side <= 1; side += 2)
            {
                var roof = Box(root, "Terracotta pitched roof", new Vector3(side * 2.2f, 6.8f, 0f), new Vector3(5.1f, 0.2f, 9.4f), "clay_roof_tiles_02"); roof.transform.localRotation = Quaternion.Euler(0f, 0f, side * -20f);
                Box(root, "Upper window", new Vector3(side * 2.25f, 4.5f, -4.05f), new Vector3(1.2f, 1.6f, 0.12f), "medieval_wood", false);
                Box(root, "Veranda post", new Vector3(side * 3.5f, 1.6f, -6.1f), new Vector3(0.14f, 3.2f, 0.14f), "weathered_planks");
            }
            Box(root, "Shop shutter", new Vector3(0f, 1.5f, -4.08f), new Vector3(2.6f, 2.7f, 0.16f), "weathered_planks", false);
            Box(root, "Balcony rail", new Vector3(0f, 3.6f, -4.5f), new Vector3(7f, 0.12f, 0.12f), "medieval_wood", false);
            for (int i = -3; i <= 3; i++) Box(root, "Balustrade", new Vector3(i, 3.85f, -4.5f), new Vector3(0.08f, 0.6f, 0.08f), "medieval_wood", false);
            var cloth = ZoneKit.Tinted("clay_plaster", variant % 2 == 0 ? new Color(0.8f, 0.32f, 0.08f) : new Color(0.19f, 0.28f, 0.5f), 0.1f);
            var canopy = ZoneKit.Part(root, "Canvas canopy", new Vector3(0f, 3.1f, -5.2f), new Vector3(7.5f, 0.07f, 3f), cloth, false); canopy.transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            Sign(root, new Vector3(0f, 3.55f, -4.15f), name.ToUpperInvariant(), 4.8f);
            for (int i = 0; i < 3; i++)
                ArtUpgrade.Place(root, new[] { "ceramic_pot", "Barrel_01", "wooden_bowl_02" }[(i + variant) % 3], root.TransformPoint(new Vector3(-2.5f + i * 2.4f, 0f, -4.8f)), 1.1f, i * 53f);
            ArtUpgrade.Place(root, "WoodenTable_01", root.TransformPoint(new Vector3(0f, 0f, -5.5f)), 1.1f, yaw, true);
        }
        public static void Road(Transform p, Vector3[] points, float width, bool solid = true)
        {
            for (int i = 1; i < points.Length; i++)
            {
                Vector3 d = points[i] - points[i - 1]; var r = Box(p, "Gravel road", (points[i] + points[i - 1]) * 0.5f + Vector3.up * 0.01f, new Vector3(width, 0.06f, d.magnitude + 0.4f), "stony_dirt_path", solid);
                r.transform.localRotation = Quaternion.LookRotation(d);
                if (solid && width >= 6f) r.AddComponent<DriveableRoad>();
            }
        }
    }
}
