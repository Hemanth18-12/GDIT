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

        /// <summary>Warm colour for each surface so stone reads as red / ochre sandstone, not pale cream.</summary>
        public static Color Tint(string mat) =>
            mat == "sandstone_blocks_08" ? new Color(1f, 0.74f, 0.55f) : mat == "red_sandstone_wall" ? new Color(1f, 0.78f, 0.62f) :
            mat == "red_sandstone_pavement" ? new Color(0.92f, 0.78f, 0.7f) : Color.white;

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
                r.GetPropertyBlock(mpb); mpb.SetVector("_BaseMap_ST", new Vector4(st.x, st.y, 0f, 0f)); mpb.SetColor("_BaseColor", Tint(map.mat)); r.SetPropertyBlock(mpb);
                Retextured++;
            }
            Lamps(); Guardians();
        }

        private static void Hide(Transform t) { var r = t.GetComponent<Renderer>(); if (r != null) r.enabled = false; }

        /// <summary>Brass-cylinder lamp stands become a sandstone pedestal carrying a brass diya lantern model.</summary>
        public static void Lamps()
        {
            var mpb = new MaterialPropertyBlock();
            foreach (var t in Object.FindObjectsByType<Transform>())
            {
                if (t.name != "OilBowl" || t.parent == null || t.parent.Find("Lantern") != null) continue;
                var root = t.parent; var stone = Mat("red_sandstone_wall");
                foreach (Transform c in root)
                {
                    if (c.name == "OilBowl" || c.name == "FlameMesh") Hide(c);
                    else if ((c.name == "Pillar" || c.name == "BasePedestal") && stone != null)
                    {
                        var r = c.GetComponent<Renderer>(); if (r == null) continue;
                        if (c.name == "Pillar") c.localScale = new Vector3(0.34f, c.localScale.y, 0.34f);
                        r.sharedMaterial = stone; r.GetPropertyBlock(mpb); mpb.SetVector("_BaseMap_ST", new Vector4(1.5f, c.name == "Pillar" ? 1.5f : 0.4f, 0f, 0f)); r.SetPropertyBlock(mpb);
                    }
                }
                var cap = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(cap.GetComponent<Collider>()); cap.name = "PedestalCap"; cap.transform.SetParent(root, false);
                cap.transform.localPosition = new Vector3(0f, 1.52f, 0f); cap.transform.localScale = new Vector3(0.62f, 0.1f, 0.62f); if (stone != null) cap.GetComponent<Renderer>().sharedMaterial = stone;
                var g = Place(root, "brass_diya_lantern", root.TransformPoint(new Vector3(0f, 1.57f, 0f)), 2.4f, 0f, false); if (g != null) g.name = "Lantern";
            }
        }

        /// <summary>The box-built gate guardians become carved elephant statues on their pedestals.</summary>
        public static void Guardians()
        {
            foreach (var t in Object.FindObjectsByType<Transform>())
            {
                if (t.name != "Lion_Torso" || t.parent == null || t.parent.Find("Guardian") != null) continue;
                var root = t.parent;
                foreach (Transform c in root) if (c.name.StartsWith("Lion_") || c.name.StartsWith("Paw_")) { Hide(c); var col = c.GetComponent<Collider>(); if (col != null) col.enabled = false; }
                var g = Place(root, "carved_wooden_elephant", root.TransformPoint(new Vector3(0f, 0.9f, 0f)), 15f, 0f, false); if (g == null) continue;
                g.name = "Guardian"; g.transform.localRotation = Quaternion.Euler(0f, GuardianYaw, 0f);
                var box = g.AddComponent<BoxCollider>(); box.center = new Vector3(0f, 0.05f, 0f); box.size = new Vector3(0.1f, 0.1f, 0.06f);
            }
        }
        public static float GuardianYaw = -90f;

        // ---- foliage: lumpy leaf masses instead of smooth green balls
        private static Mesh[] blobs;
        private static Mesh Blob(int k)
        {
            if (blobs == null)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere); var src = tmp.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(tmp);
                blobs = new Mesh[4];
                for (int i = 0; i < blobs.Length; i++)
                {
                    var v = src.vertices; float s = 3.1f + i * 7.7f;
                    for (int j = 0; j < v.Length; j++)
                    {
                        Vector3 q = v[j] * 2f;
                        float n = Mathf.PerlinNoise(q.x * 2.6f + s, q.y * 2.6f + s) + Mathf.PerlinNoise(q.z * 2.6f + s * 2f, q.x * 2.6f - s) + 0.5f * Mathf.PerlinNoise(q.y * 6f + s, q.z * 6f + s) - 1.25f;
                        v[j] *= 1f + 0.42f * n;
                    }
                    var m = new Mesh { name = "FoliageBlob" + i, vertices = v, uv = src.uv, triangles = src.triangles };
                    m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds(); blobs[i] = m;
                }
            }
            return blobs[Mathf.Abs(k) % blobs.Length];
        }

        private static void Leafy(GameObject g, int seed, MaterialPropertyBlock mpb)
        {
            var mat = Mat("foliage"); if (mat == null) return;
            var rng = new System.Random(seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            void One(GameObject o)
            {
                o.GetComponent<MeshFilter>().sharedMesh = Blob(rng.Next(4));
                var r = o.GetComponent<MeshRenderer>(); r.sharedMaterial = mat;
                float t = R(0.75f, 1.15f); r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", new Color(0.55f * t, 0.8f * t, 0.42f * t)); mpb.SetVector("_BaseMap_ST", new Vector4(2.5f, 2.5f, R(0, 1), R(0, 1))); r.SetPropertyBlock(mpb);
            }
            One(g);
            int extra = Mathf.Max(g.transform.lossyScale.x, g.transform.lossyScale.z) > 2f ? 5 : 2;
            for (int i = 0; i < extra; i++)
            {
                var c = new GameObject("Leaves"); c.transform.SetParent(g.transform, false);
                Vector3 d = new Vector3(R(-1f, 1f), R(-0.35f, 0.7f), R(-1f, 1f)).normalized * R(0.3f, 0.48f);
                c.transform.localPosition = d; c.transform.localRotation = Quaternion.Euler(R(0, 360), R(0, 360), R(0, 360)); c.transform.localScale = Vector3.one * R(0.5f, 0.8f);
                c.AddComponent<MeshFilter>(); c.AddComponent<MeshRenderer>(); One(c);
            }
        }

        /// <summary>Every green ball (tree crown, bush, fern) in the scene becomes a cluster of leaf masses.</summary>
        public static int Foliage()
        {
            int n = 0; var mpb = new MaterialPropertyBlock();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                var mf = r.GetComponent<MeshFilter>(); var m = r.sharedMaterial;
                if (mf == null || mf.sharedMesh == null || mf.sharedMesh.name != "Sphere" || m == null || !m.HasProperty("_BaseColor") || !r.enabled) continue;
                if (r.name.StartsWith("Lion") || r.name.Contains("Flame") || r.name.Contains("Temple")) continue;
                Color c = m.GetColor("_BaseColor"); Vector3 s = r.transform.lossyScale;
                if (!(c.g > c.r * 1.2f && c.g > c.b * 1.1f) || Mathf.Max(s.x, s.z) < 0.6f) continue;
                Leafy(r.gameObject, r.transform.position.GetHashCode(), mpb); n++;
            }
            return n;
        }

        /// <summary>A tree: bark trunk, two boughs and a leaf crown.</summary>
        public static GameObject Tree(Transform root, Vector3 pos, float height, int seed)
        {
            var rng = new System.Random(seed); float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var t = new GameObject("Tree"); t.transform.SetParent(root, false); t.transform.position = pos; t.transform.rotation = Quaternion.Euler(0f, R(0, 360), 0f);
            var mpb = new MaterialPropertyBlock(); var bark = Mat("bark_brown_02");
            GameObject Limb(Vector3 lp, Vector3 scale, Vector3 euler)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder); c.name = "Limb"; c.transform.SetParent(t.transform, false);
                c.transform.localPosition = lp; c.transform.localScale = scale; c.transform.localRotation = Quaternion.Euler(euler);
                var r = c.GetComponent<Renderer>(); if (bark != null) r.sharedMaterial = bark;
                r.GetPropertyBlock(mpb); mpb.SetVector("_BaseMap_ST", new Vector4(2f, scale.y * 1.2f, 0f, 0f)); r.SetPropertyBlock(mpb);
                return c;
            }
            Limb(new Vector3(0f, height * 0.5f, 0f), new Vector3(0.42f, height * 0.5f, 0.42f), new Vector3(R(-3, 3), 0f, R(-3, 3)));
            Object.Destroy(Limb(new Vector3(0.55f, height * 0.82f, 0f), new Vector3(0.18f, 0.9f, 0.18f), new Vector3(0f, 0f, -48f)).GetComponent<Collider>());
            Object.Destroy(Limb(new Vector3(-0.5f, height * 0.72f, 0.2f), new Vector3(0.16f, 0.8f, 0.16f), new Vector3(15f, 0f, 52f)).GetComponent<Collider>());
            for (int i = 0; i < 3; i++)
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(crown.GetComponent<Collider>()); crown.name = "Crown"; crown.transform.SetParent(t.transform, false);
                crown.transform.localPosition = i == 0 ? new Vector3(0f, height + 0.9f, 0f) : new Vector3(i == 1 ? 1.5f : -1.4f, height + R(-0.2f, 0.5f), i == 1 ? 0.2f : 0.5f);
                float w = i == 0 ? R(4.2f, 5.2f) : R(2.6f, 3.4f); crown.transform.localScale = new Vector3(w, w * 0.68f, w);
                Leafy(crown, seed * 7 + i, mpb);
                if (i == 0) crown.AddComponent<WindSway>().amount = 0.35f;
            }
            return t;
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
