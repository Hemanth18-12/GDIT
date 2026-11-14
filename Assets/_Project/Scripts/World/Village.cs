using UnityEngine;

namespace LegacyOfTemples.World
{
    /// <summary>
    /// Builds a village around the temple courtyard at runtime: plastered houses with thatch or clay-tile roofs,
    /// verandas, lit windows, a dirt lane, lanterns and household props (Poly Haven CC0 textures and models).
    /// Everything stands outside the compound wall, so it never blocks the story route.
    /// </summary>
    public static class Village
    {
        public static int Houses { get; private set; }
        private static MaterialPropertyBlock mpb;

        public static void Build(Transform root)
        {
            Houses = 0;
            var floor = GameObject.Find("Plaza_WetStoneFloor");
            if (floor == null || GameObject.Find("Village") != null) return;
            var fr = floor.GetComponent<Renderer>(); if (fr == null) return;
            Bounds b = fr.bounds; float top = b.max.y;
            float xOut = b.extents.x + 7f, zMax = b.max.z + 7f, zMin = Mathf.Min(b.min.z, b.center.z - 40f) - 7f;
            var v = new GameObject("Village").transform; v.SetParent(root, false);
            Vector3 c = new Vector3(b.center.x, top, b.center.z);

            // ground and the lane that circles the compound
            Box(v, "Village_Ground", new Vector3(c.x, top - 0.16f, c.z), new Vector3(280f, 0.2f, 280f), "red_laterite_soil_stones", 5f, false);
            float laneX = xOut - 2.5f, zc = (zMax + zMin) * 0.5f, zLen = zMax - zMin + 6f;
            Box(v, "Lane_E", new Vector3(c.x + laneX, top - 0.1f, zc), new Vector3(5f, 0.2f, zLen), "stony_dirt_path", 4f, false);
            Box(v, "Lane_W", new Vector3(c.x - laneX, top - 0.1f, zc), new Vector3(5f, 0.2f, zLen), "stony_dirt_path", 4f, false);
            Box(v, "Lane_S", new Vector3(c.x, top - 0.1f, zMax - 2.5f), new Vector3(laneX * 2f, 0.2f, 5f), "stony_dirt_path", 4f, false);
            Box(v, "Lane_N", new Vector3(c.x, top - 0.1f, zMin + 2.5f), new Vector3(laneX * 2f, 0.2f, 5f), "stony_dirt_path", 4f, false);

            var rng = new System.Random(1712);
            float R(float a, float d) => a + (float)rng.NextDouble() * (d - a);
            int lights = 0;
            for (int row = 0; row < 2; row++)
            {
                float off = 5f + row * 12f;
                for (float z = zMin - 6f; z <= zMax + 6f; z += R(10.5f, 13f))
                {
                    House(v, new Vector3(c.x + xOut + off + R(-1f, 1f), top, z + R(-1.2f, 1.2f)), -90f + R(-8f, 8f), rng, row == 0 && lights++ % 3 == 0);
                    House(v, new Vector3(c.x - xOut - off + R(-1f, 1f), top, z + R(-1.2f, 1.2f)), 90f + R(-8f, 8f), rng, row == 0 && lights++ % 3 == 0);
                }
                for (float x = -xOut + 6f; x <= xOut - 6f; x += R(10.5f, 13f))
                {
                    House(v, new Vector3(c.x + x + R(-1.2f, 1.2f), top, zMax + off + R(-1f, 1f)), 180f + R(-8f, 8f), rng, row == 0 && lights++ % 3 == 0);
                    House(v, new Vector3(c.x + x + R(-1.2f, 1.2f), top, zMin - off + R(-1f, 1f)), R(-8f, 8f), rng, row == 0 && lights++ % 3 == 0);
                }
            }
        }

        /// <summary>One house. Its front (door, veranda) faces local +Z.</summary>
        private static void House(Transform parent, Vector3 pos, float yaw, System.Random rng, bool lantern)
        {
            float R(float a, float d) => a + (float)rng.NextDouble() * (d - a);
            var h = new GameObject("House_" + Houses).transform; h.SetParent(parent, false);
            h.position = pos; h.rotation = Quaternion.Euler(0f, yaw, 0f); Houses++;
            float w = R(5.2f, 7.4f), d = R(5f, 7f), ht = R(2.9f, 3.6f), baseY = 0.4f;
            int style = rng.Next(3);
            string plaster = style == 0 ? "white_plaster_rough_01" : style == 1 ? "yellow_plaster" : "clay_plaster";
            bool thatch = rng.Next(5) < 2;

            Box(h, "Plinth", new Vector3(0f, baseY * 0.5f, 0.5f), new Vector3(w + 0.7f, baseY, d + 1.8f), "sandstone_blocks_08", 2.2f);
            Box(h, "Walls", new Vector3(0f, baseY + ht * 0.5f, 0f), new Vector3(w, ht, d), plaster, 2.6f);
            Box(h, "Band", new Vector3(0f, baseY + 0.25f, 0f), new Vector3(w + 0.06f, 0.5f, d + 0.06f), "red_sandstone_wall", 2f);
            Roof(h, new Vector3(0f, baseY + ht, 0f), w + 1.1f, d + 1.1f, R(1.5f, 2.2f), thatch ? "thatch_roof_angled" : "clay_roof_tiles_02", thatch ? 2.4f : 1.8f);

            float fz = d * 0.5f;
            Box(h, "Door", new Vector3(0f, baseY + 1f, fz + 0.03f), new Vector3(1.1f, 2f, 0.12f), "medieval_wood", 1.2f);
            Box(h, "DoorFrame", new Vector3(0f, baseY + 1.05f, fz + 0.01f), new Vector3(1.4f, 2.2f, 0.1f), "weathered_planks", 1.2f);
            for (int s = -1; s <= 1; s += 2)
            {
                float wx = s * (w * 0.5f - 1.2f);
                Box(h, "WindowFrame", new Vector3(wx, baseY + 1.75f, fz + 0.01f), new Vector3(1.15f, 1.15f, 0.1f), "medieval_wood", 1.2f);
                bool lit = rng.Next(10) < 6;
                Box(h, lit ? "WindowLit" : "Shutter", new Vector3(wx, baseY + 1.75f, fz + 0.04f), new Vector3(0.85f, 0.85f, 0.1f), lit ? "window_glow" : "weathered_planks", 0.9f);
                // side windows
                Box(h, "SideFrame", new Vector3(s * (w * 0.5f + 0.01f), baseY + 1.75f, 0f), new Vector3(0.1f, 1.1f, 1.1f), "medieval_wood", 1.2f);
                Box(h, rng.Next(10) < 5 ? "WindowLit" : "Shutter", new Vector3(s * (w * 0.5f + 0.04f), baseY + 1.75f, 0f), new Vector3(0.1f, 0.8f, 0.8f), rng.Next(10) < 5 ? "window_glow" : "weathered_planks", 0.9f);
            }

            if (rng.Next(10) < 6)
            {   // veranda: lean-to roof on wooden posts
                var slab = Box(h, "VerandaRoof", new Vector3(0f, baseY + 2.55f, fz + 1.05f), new Vector3(w + 0.6f, 0.12f, 2.4f), thatch ? "thatch_roof_angled" : "clay_roof_tiles_02", 1.8f);
                slab.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
                for (int s = -1; s <= 1; s++)
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder); post.name = "Post"; post.transform.SetParent(h, false);
                    post.transform.localPosition = new Vector3(s * (w * 0.5f - 0.1f), baseY + 1.13f, fz + 1.9f); post.transform.localScale = new Vector3(0.16f, 1.13f, 0.16f);
                    Paint(post, "medieval_wood", new Vector2(1f, 2f));
                }
            }

            // household things by the door
            Vector3 front = h.TransformPoint(new Vector3(0f, baseY, fz + 0.9f)), side = h.right;
            ArtUpgrade.Place(h, rng.Next(2) == 0 ? "ceramic_pot" : "brass_pot_01", front + side * 1.5f, rng.Next(2) == 0 ? 1.5f : 2.2f, R(0f, 360f), false);
            if (rng.Next(2) == 0) ArtUpgrade.Place(h, "ceramic_pot", front - side * (w * 0.5f - 0.5f), 1.1f, R(0f, 360f), false);
            if (rng.Next(3) == 0) { ArtUpgrade.Place(h, "planter_box_01", front - side * 1.6f, 1.3f, yaw, false); ArtUpgrade.Place(h, "periwinkle_plant", front - side * 1.6f + Vector3.up * 0.4f, 1f, R(0f, 360f), false); }
            if (lantern)
            {
                var lg = ArtUpgrade.Place(h, "wooden_lantern_01", front + side * 0.95f, 1.5f, R(0f, 360f), false);
                var l = new GameObject("VillageLantern_Light").AddComponent<Light>(); l.transform.SetParent(h, false);
                l.transform.position = front + side * 0.95f + Vector3.up * 0.7f + h.forward * 0.4f;
                l.type = LightType.Point; l.range = 9f; l.intensity = 3f; l.color = new Color(1f, 0.68f, 0.34f); l.shadows = LightShadows.None;
                if (lg == null) l.intensity = 2.4f;
            }
        }

        private static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, string mat, float tile, bool keepCollider = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos; g.transform.localScale = size;
            if (!keepCollider || size.x < 0.3f || size.z < 0.3f) { var col = g.GetComponent<Collider>(); if (col != null) Object.Destroy(col); }
            Vector2 st = size.y <= size.x && size.y <= size.z ? new Vector2(size.x, size.z) : new Vector2(Mathf.Max(size.x, size.z), size.y);
            Paint(g, mat, st / tile);
            return g;
        }

        private static void Paint(GameObject g, string mat, Vector2 st)
        {
            var r = g.GetComponent<Renderer>(); var m = ArtUpgrade.Mat(mat);
            if (m != null) r.sharedMaterial = m;
            if (mat == "window_glow") { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return; }
            if (mpb == null) mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb); mpb.SetVector("_BaseMap_ST", new Vector4(Mathf.Max(0.25f, st.x), Mathf.Max(0.25f, st.y), 0f, 0f)); r.SetPropertyBlock(mpb);
        }

        /// <summary>A gabled roof (ridge along local Z) with UVs in metres so thatch and tiles keep their real size.</summary>
        private static void Roof(Transform parent, Vector3 localPos, float w, float d, float rise, string mat, float tile)
        {
            float hw = w * 0.5f, hd = d * 0.5f, slope = Mathf.Sqrt(hw * hw + rise * rise), drop = 0.25f;
            float eave = -drop, u = d / tile, vv = slope / tile;
            var verts = new[]
            {
                // left slope
                new Vector3(-hw, eave, -hd), new Vector3(-hw, eave, hd), new Vector3(0f, rise, hd), new Vector3(0f, rise, -hd),
                // right slope
                new Vector3(hw, eave, hd), new Vector3(hw, eave, -hd), new Vector3(0f, rise, -hd), new Vector3(0f, rise, hd),
                // front gable, back gable
                new Vector3(-hw, eave, hd), new Vector3(hw, eave, hd), new Vector3(0f, rise, hd),
                new Vector3(hw, eave, -hd), new Vector3(-hw, eave, -hd), new Vector3(0f, rise, -hd),
                // underside
                new Vector3(-hw, eave, -hd), new Vector3(hw, eave, -hd), new Vector3(hw, eave, hd), new Vector3(-hw, eave, hd),
            };
            var uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(u, 0f), new Vector2(u, vv), new Vector2(0f, vv),
                new Vector2(0f, 0f), new Vector2(u, 0f), new Vector2(u, vv), new Vector2(0f, vv),
                new Vector2(0f, 0f), new Vector2(w / tile, 0f), new Vector2(hw / tile, rise / tile),
                new Vector2(0f, 0f), new Vector2(w / tile, 0f), new Vector2(hw / tile, rise / tile),
                new Vector2(0f, 0f), new Vector2(w / tile, 0f), new Vector2(w / tile, u), new Vector2(0f, u),
            };
            var tris = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 14, 16, 17 };
            var mesh = new Mesh { name = "VillageRoof", vertices = verts, uv = uv, triangles = tris };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            var g = new GameObject("Roof"); g.transform.SetParent(parent, false); g.transform.localPosition = localPos;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); var m = ArtUpgrade.Mat(mat); if (m != null) r.sharedMaterial = m;
        }
    }
}
