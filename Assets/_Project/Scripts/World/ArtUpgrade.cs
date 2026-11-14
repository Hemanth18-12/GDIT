using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.World
{
    /// <summary>
    /// Replaces the flat materials on the levels' box / cylinder geometry with real PBR surfaces (Poly Haven, CC0)
    /// tiled in world units, and scatters real prop models (brass lamps and pots, vases, rocks, ferns, barrels).
    /// The shrine figure and all custom meshes are left untouched.
    /// </summary>
    public static class ArtUpgrade
    {
        private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        private static readonly Dictionary<string, GameObject> props = new Dictionary<string, GameObject>();
        public static int Retextured { get; private set; }
        public static int PropsPlaced { get; private set; }

        public static Material Mat(string n) { if (!mats.TryGetValue(n, out var m) || m == null) mats[n] = m = Resources.Load<Material>("Mats/" + n); return m; }
        public static GameObject Prop(string n) { if (!props.TryGetValue(n, out var p) || p == null) props[n] = p = Resources.Load<GameObject>("Props/" + n); return p; }

        /// <summary>(new material, metres per texture repeat) for an old flat material name; null = leave it.</summary>
        private static (string mat, float tile) Map(string old, string scene, string mesh)
        {
            switch (old)
            {
                case "mat_StoneTemple": case "mat_WarmTempleStone": return ("sandstone_blocks_08", 2.6f);
                case "mat_LightStone": case "mat_CarvedPillar": case "mat_LimestoneRelief": return mesh == "Sphere" ? (null, 0f) : ("red_sandstone_wall", 2.2f);
                case "mat_StoneFloorWet": return mesh == "Sphere" ? (null, 0f) : ("red_sandstone_pavement", 3.2f);
                case "mat_MudPath": return ("forest_leaves_02", 4f);
                case "mat_JungleMoss": return mesh == "Sphere" ? (null, 0f) : ("mossy_sandstone", 3f);
                case "mat_JungleBark": return ("bark_brown_02", 1.6f);
                case "mat_TempleWood": return ("weathered_planks", 1.4f);
                case "mat_DarkWood": return ("medieval_wood", 1.4f);
                default: return (null, 0f);
            }
        }

        public static void Apply(string scene)
        {
            Retextured = 0;
            var mpb = new MaterialPropertyBlock();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                var mf = r.GetComponent<MeshFilter>(); var old = r.sharedMaterial;
                if (mf == null || mf.sharedMesh == null || old == null) continue;
                string mesh = mf.sharedMesh.name;
                bool cube = mesh == "Cube", cyl = mesh == "Cylinder", sph = mesh == "Sphere";
                var map = Map(old.name.Replace(" (Instance)", ""), scene, mesh);
                if (map.mat == null) continue;
                bool custom = !cube && !cyl && !sph;
                var m = Mat(map.mat); if (m == null) continue;
                r.sharedMaterial = m;
                Vector3 s = r.transform.lossyScale; s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                Vector2 st;
                if (custom) { var b = r.bounds.size; st = new Vector2(Mathf.Max(b.x, b.z), Mathf.Max(b.y, 1f)) / map.tile; }
                else if (cyl) st = new Vector2(Mathf.PI * Mathf.Max(s.x, s.z), s.y * 2f) / map.tile;
                else if (s.y <= s.x && s.y <= s.z) st = new Vector2(s.x, s.z) / map.tile;          // floor or slab
                else if (s.z <= s.x) st = new Vector2(s.x, s.y) / map.tile;                        // wall along X
                else st = new Vector2(s.z, s.y) / map.tile;                                        // wall along Z
                st = new Vector2(Mathf.Max(0.25f, st.x), Mathf.Max(0.25f, st.y));
                r.GetPropertyBlock(mpb); mpb.SetVector("_BaseMap_ST", new Vector4(st.x, st.y, 0f, 0f)); r.SetPropertyBlock(mpb);
                Retextured++;
            }
        }

        public static Vector3 Ground(Vector3 p) => Physics.Raycast(p + Vector3.up * 8f, Vector3.down, out RaycastHit h, 30f, ~0, QueryTriggerInteraction.Ignore) ? h.point : p;

        /// <summary>Place a prop model on the ground. Returns null if the prefab is missing.</summary>
        public static GameObject Place(Transform root, string prop, Vector3 pos, float scale = 1f, float yaw = 0f, bool snap = true)
        {
            var pf = Prop(prop); if (pf == null) return null;
            var g = Object.Instantiate(pf, snap ? Ground(pos) : pos, Quaternion.Euler(0f, yaw, 0f), root);
            g.name = prop; g.transform.localScale = Vector3.one * scale; PropsPlaced++;
            return g;
        }

        public static void Scatter(Transform root, string scene, Vector3 p, Vector3 f)
        {
            PropsPlaced = 0;
            Vector3 r = Vector3.Cross(Vector3.up, f); var rng = new System.Random(scene.GetHashCode());
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            switch (scene)
            {
                case "Courtyard":
                    for (int i = 0; i < 6; i++)
                    {   // offerings and brassware along both sides of the plaza
                        float side = i % 2 == 0 ? -1f : 1f; Vector3 c = p + r * side * R(5f, 7.5f) - f * R(-6f, 8f);
                        Place(root, "brass_pot_01", c, 2.2f, R(0, 360)); Place(root, "brass_vase_03", c + r * 0.6f, 3.5f, R(0, 360));
                        Place(root, "ceramic_vase_01", c - r * 0.7f + f * 0.4f, 1.8f, R(0, 360)); Place(root, "wooden_bowl_02", c + f * 0.8f, 3f, R(0, 360));
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        float side = i % 2 == 0 ? -1f : 1f; Vector3 c = p + r * side * 10.5f - f * (4f - i * 3.5f);
                        Place(root, "planter_box_01", c, 1.6f, 90f); Place(root, "calathea_orbifolia_01", c + Vector3.up * 0.55f, 0.6f, R(0, 360), false);
                        Place(root, "ceramic_pot", c + f * 1.6f, 1.1f, R(0, 360)); Place(root, "ceramic_pot", c - f * 1.5f, 1.4f, R(0, 360));
                    }
                    Place(root, "WoodenTable_01", p - r * 8f + f * 2f, 1.1f, 80f); Place(root, "brass_goblets", Ground(p - r * 8f + f * 2f) + Vector3.up * 0.62f, 1.4f, 20f, false);
                    Place(root, "folding_wooden_stool", p - r * 7f + f * 3.2f, 1f, 40f); Place(root, "wooden_lantern_01", p + r * 8f + f * 3f, 1.6f, 0f);
                    break;
                case "Sanctum":
                    for (int i = 0; i < 6; i++)
                    {
                        float side = i % 2 == 0 ? -1f : 1f; Vector3 c = p + f * (2f + (i / 2) * 5f) + r * side * 6.2f;
                        Place(root, "brass_pot_01", c, 2.4f, R(0, 360)); Place(root, "antique_ceramic_vase_01", c + f * 0.9f, 2f, R(0, 360));
                        Place(root, "brass_diya_lantern", c - f * 0.8f, 2.2f, R(0, 360)); Place(root, "carved_wooden_plate", c + r * -side * 0.7f, 2f, R(0, 360));
                    }
                    Place(root, "ceramic_pot", p + r * 7f - f * 1f, 1.1f); Place(root, "ceramic_pot", p + r * 7.2f - f * 2.1f, 1.1f, 40f);
                    break;
                case "ShrineCrime":
                    for (int i = 0; i < 5; i++)
                    {
                        float side = i % 2 == 0 ? -1f : 1f; Vector3 c = p + f * (1f + i * 3f) + r * side * 5.6f;
                        Place(root, "vintage_oil_lamp", c, 1.5f, R(0, 360)); Place(root, "brass_pot_02", c + f * 0.8f, 2.2f, R(0, 360)); Place(root, "ceramic_vase_01", c - f * 0.7f, 1.8f, R(0, 360));
                    }
                    Place(root, "dry_branches_medium_01", p + f * 3f - r * 4f, 1f, 30f);
                    break;
                case "PuzzleWall":
                    for (int i = 0; i < 4; i++)
                    {
                        float side = i % 2 == 0 ? -1f : 1f; Vector3 c = p + f * (1.5f + (i / 2) * 5f) + r * side * 6f;
                        Place(root, "antique_ceramic_vase_01", c, 2.4f, R(0, 360)); Place(root, "brass_pot_01", c + f * 1f, 2.2f); Place(root, "vintage_oil_lamp", c - f * 1f, 1.4f);
                    }
                    Place(root, "rock_07", p + r * 5f + f * 8f, 4f, 60f); Place(root, "rock_09", p - r * 5.5f + f * 7f, 9f, 10f);
                    break;
                case "JunglePath":
                    for (int i = 0; i < 6; i++)
                    {   // mossy rock outcrops along both sides
                        float side = i % 2 == 0 ? -1f : 1f; Vector3 c = p + f * (-4f + i * 9f) + r * side * R(11f, 15f);
                        Place(root, i % 2 == 0 ? "rock_moss_set_01" : "rock_moss_set_02", c, R(0.9f, 1.3f), R(0, 360));
                    }
                    for (int i = 0; i < 60; i++)
                    {
                        float side = rng.NextDouble() < 0.5 ? -1f : 1f; Vector3 c = p + f * R(-8f, 50f) + r * side * R(3.5f, 16f);
                        string pr = i % 5 == 0 ? "nettle_plant" : i % 5 == 1 ? "periwinkle_plant" : i % 7 == 0 ? "dry_branches_medium_01" : "fern_02";
                        var g = Place(root, pr, c, R(0.8f, 1.5f), R(0, 360)); if (g != null && i % 3 == 0) g.AddComponent<WindSway>().amount = 0.6f;
                    }
                    for (int i = 0; i < 5; i++) Place(root, "tree_stump_01", p + f * R(0f, 45f) + r * (i % 2 == 0 ? -1f : 1f) * R(5f, 12f), R(0.8f, 1.2f), R(0, 360));
                    for (int i = 0; i < 8; i++) Place(root, "rock_07", p + f * R(-5f, 50f) + r * (i % 2 == 0 ? -1f : 1f) * R(2.6f, 9f), R(2f, 5f), R(0, 360));
                    break;
            }
        }
    }
}
