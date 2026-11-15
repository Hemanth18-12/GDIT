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
        /// <summary>Layout, for the village life, the jeep and the temple: compound centre (on the plaza), outer lane edges, main road.</summary>
        public static Vector3 Centre; public static float Top, XOut, ZMin, ZMax;
        public static Vector3 RoadStart, GatePos, NorthRoadEnd;
        /// <summary>Indian lime-wash colours for the house walls, and darker dado bands.</summary>
        private static readonly Color[] Wash = { new Color(0.62f, 0.8f, 0.95f), new Color(0.98f, 0.86f, 0.5f), new Color(0.96f, 0.72f, 0.74f), new Color(0.66f, 0.9f, 0.78f), new Color(1f, 0.97f, 0.9f), new Color(0.98f, 0.66f, 0.42f), new Color(0.72f, 0.7f, 0.95f), new Color(0.55f, 0.85f, 0.85f) };
        private static readonly Color[] Dado = { new Color(0.35f, 0.15f, 0.12f), new Color(0.15f, 0.28f, 0.5f), new Color(0.55f, 0.22f, 0.12f), new Color(0.2f, 0.35f, 0.25f) };
        private static readonly Color[] DoorPaint = { new Color(0.15f, 0.42f, 0.62f), new Color(0.2f, 0.5f, 0.32f), new Color(0.62f, 0.2f, 0.15f), new Color(0.75f, 0.55f, 0.2f) };
        private static Color? nextTint;
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
            Centre = c; Top = top; XOut = xOut; ZMin = zMin; ZMax = zMax;

            // ground and the lane that circles the compound
            Box(v, "Village_Ground", new Vector3(c.x, top - 0.16f, c.z), new Vector3(280f, 0.2f, 280f), "red_laterite_soil_stones", 5f);
            float laneX = xOut - 2.5f, zc = (zMax + zMin) * 0.5f, zLen = zMax - zMin + 6f;
            Box(v, "Lane_E", new Vector3(c.x + laneX, top - 0.1f, zc), new Vector3(5f, 0.2f, zLen), "stony_dirt_path", 4f);
            Box(v, "Lane_W", new Vector3(c.x - laneX, top - 0.1f, zc), new Vector3(5f, 0.2f, zLen), "stony_dirt_path", 4f);
            Box(v, "Lane_S", new Vector3(c.x, top - 0.1f, zMax - 2.5f), new Vector3(laneX * 2f, 0.2f, 5f), "stony_dirt_path", 4f);
            Box(v, "Lane_N", new Vector3(c.x, top - 0.1f, zMin + 2.5f), new Vector3(laneX * 2f, 0.2f, 5f), "stony_dirt_path", 4f);

            SplineRoad(v, zMax - 3f, zMax + 128f);   // the approach road: a Unity Spline with a bend, meshed with kerbs
            Box(v, "MainRoad_North", new Vector3(c.x, top - 0.08f, zMin - 30f), new Vector3(8f, 0.2f, 66f), "stony_dirt_path", 4f);
            RoadStart = RoadPoint(zMax + 118f); NorthRoadEnd = new Vector3(c.x, top, zMin - 62f);
            Hub(v, c, top, xOut, zMin, zMax, laneX);

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
                    if (Mathf.Abs(x) < 9f) continue;   // the main road runs through here
                    House(v, new Vector3(c.x + x + R(-1.2f, 1.2f), top, zMax + off + R(-1f, 1f)), 180f + R(-8f, 8f), rng, row == 0 && lights++ % 3 == 0);
                    House(v, new Vector3(c.x + x + R(-1.2f, 1.2f), top, zMin - off + R(-1f, 1f)), R(-8f, 8f), rng, row == 0 && lights++ % 3 == 0);
                }
            }
        }

        /// <summary>
        /// Makes the village walkable: a gate in the compound's south wall, an outer boundary, trees,
        /// and a stone gateway with a name board for each of the five places in the story.
        /// </summary>
        private static void Hub(Transform v, Vector3 c, float top, float xOut, float zMin, float zMax, float laneX)
        {
            // gate through the south wall
            var south = GameObject.Find("Wall_South");
            if (south != null)
            {
                Vector3 wp = south.transform.position, ws = south.transform.lossyScale; float half = ws.x * 0.5f, gap = 3.2f;
                south.SetActive(false);
                Box(v, "Wall_South_L", new Vector3(wp.x - (half + gap) * 0.5f, wp.y, wp.z), new Vector3(half - gap, ws.y, ws.z), "sandstone_blocks_08", 2.6f);
                Box(v, "Wall_South_R", new Vector3(wp.x + (half + gap) * 0.5f, wp.y, wp.z), new Vector3(half - gap, ws.y, ws.z), "sandstone_blocks_08", 2.6f);
                Gateway(v, new Vector3(wp.x, top, wp.z), 0f, gap * 2f, "TEMPLE COURTYARD", "1"); GatePos = new Vector3(wp.x, top, wp.z);
                Box(v, "GateApron", new Vector3(wp.x, top - 0.1f, wp.z + 2.5f), new Vector3(gap * 2f, 0.2f, 6f), "red_sandstone_pavement", 3.2f);
            }
            float zs = zMax - 2.5f, zn = zMin + 2.5f, zc = (zMax + zMin) * 0.5f;
            Gateway(v, new Vector3(c.x - laneX * 0.55f, top, zs), 90f, 5.6f, "STORAGE HALL", "2");
            Gateway(v, new Vector3(c.x - laneX, top, zc + 6f), 0f, 5.6f, "OUTER SHRINE", "3");
            Gateway(v, new Vector3(c.x + laneX * 0.2f, top, zn), 90f, 5.6f, "SUN WALL", "4");
            Gateway(v, new Vector3(c.x + laneX, top, zc + 6f), 0f, 5.6f, "JUNGLE RUINS", "5");

            // outer boundary (invisible) so nobody walks off the edge of the world
            float bx = xOut + 30f, bzMax = zMax + 128f, bzMin = zMin - 30f; float bzc = (bzMax + bzMin) * 0.5f;
            Fence(v, new Vector3(c.x + bx, top + 3f, bzc), new Vector3(1f, 8f, bzMax - bzMin + 2f)); Fence(v, new Vector3(c.x - bx, top + 3f, bzc), new Vector3(1f, 8f, bzMax - bzMin + 2f));
            Fence(v, new Vector3(c.x, top + 3f, bzMax), new Vector3(bx * 2f + 2f, 8f, 1f));
            var north = new GameObject("VillageBoundary_North"); north.transform.SetParent(v, false); north.transform.position = new Vector3(c.x, top + 3f, bzMin); north.AddComponent<BoxCollider>().size = new Vector3(bx * 2f + 2f, 8f, 1f);
            Box(v, "Village_Ground_South", new Vector3(c.x, top - 0.16f, zMax + 84f), new Vector3(bx * 2f + 20f, 0.2f, 108f), "red_laterite_soil_stones", 5f);

            // trees behind and between the houses
            var rng = new System.Random(99); float R(float a, float d) => a + (float)rng.NextDouble() * (d - a);
            for (int i = 0; i < 26; i++)
            {
                bool side = i % 2 == 0; float sgn = i % 4 < 2 ? 1f : -1f;
                Vector3 p = side ? new Vector3(c.x + sgn * (xOut + R(10.5f, 12.5f)), top - 0.06f, R(zMin - 4f, zMax + 4f)) : new Vector3(c.x + R(-xOut, xOut), top - 0.06f, sgn > 0 ? zMax + R(10.5f, 12.5f) : zMin - R(10.5f, 12.5f));
                Tree(v, p, R(4.2f, 6.2f), 500 + i, rng);
            }
            for (int i = 0; i < 20; i++)   // shade trees along the main road, the way village roads are lined
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float tz = zMax + 12f + (i / 2) * 10f;
                Tree(v, new Vector3(RoadPoint(tz).x + side * R(7.5f, 9.5f), top - 0.06f, tz + R(-1.5f, 1.5f)), R(4.5f, 6f), 700 + i, rng);
            }
        }

        /// <summary>Hides loose scenery (stalls, shrubs without colliders) standing in the gateway so the way out is clear.</summary>
        public static void ClearGate()
        {
            var v = GameObject.Find("Village"); var gate = GameObject.Find("Gateway_TEMPLE_COURTYARD"); if (v == null || gate == null) return;
            Vector3 g = gate.transform.position;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                Vector3 p = r.bounds.center;
                if (Mathf.Abs(p.x - g.x) > 4.2f || p.z > g.z - 0.9f || p.z < g.z - 7f || r.bounds.size.y > 6f) continue;
                if (r.transform.IsChildOf(v.transform) || r.GetComponent<Collider>() != null || r.GetComponentInParent<Animator>() != null || r.bounds.min.y > g.y + 4f) continue;
                if (r.bounds.size.x > 12f || r.bounds.size.z > 12f) continue; // floors and large slabs stay
                r.enabled = false;
            }
        }

        /// <summary>The CC0 oak from the Medieval Village Kit when it is imported, otherwise the built tree.</summary>
        private static void Tree(Transform v, Vector3 p, float h, int seed, System.Random rng)
        {
            var oak = ArtUpgrade.Place(v, "oak_tree", p, h / 5.2f * (0.85f + 0.3f * (float)rng.NextDouble()), (float)rng.NextDouble() * 360f, false);
            if (oak == null) ArtUpgrade.Tree(v, p, h, seed);
        }

        private static void Fence(Transform v, Vector3 pos, Vector3 size)
        {
            var g = new GameObject("VillageBoundary"); g.transform.SetParent(v, false); g.transform.position = pos;
            g.AddComponent<BoxCollider>().size = size;
        }

        /// <summary>A sandstone gateway across a lane with a painted name board on both faces.</summary>
        public static void Gateway(Transform v, Vector3 pos, float yaw, float span, string title, string number)
        {
            var g = new GameObject("Gateway_" + title.Replace(' ', '_')).transform; g.SetParent(v, false); g.position = pos; g.rotation = Quaternion.Euler(0f, yaw, 0f);
            float half = span * 0.5f + 0.45f, h = 4.2f;
            for (int s = -1; s <= 1; s += 2)
            {
                Box(g, "Pillar", new Vector3(s * half, h * 0.5f, 0f), new Vector3(0.9f, h, 1.3f), "red_sandstone_wall", 2f);
                Box(g, "PillarBase", new Vector3(s * half, 0.3f, 0f), new Vector3(1.3f, 0.6f, 1.7f), "sandstone_blocks_08", 2f);
                Box(g, "PillarCap", new Vector3(s * half, h + 0.9f, 0f), new Vector3(1.2f, 0.3f, 1.6f), "sandstone_blocks_08", 2f, false);
                ArtUpgrade.Place(g, "brass_diya_lantern", g.TransformPoint(new Vector3(s * half, h + 1.05f, 0f)), 2.6f, 0f, false);
                ArtUpgrade.Place(g, "lion_head", g.TransformPoint(new Vector3(s * half, 2.6f, 0.68f)), 1.3f, yaw, false);
            }
            Box(g, "Lintel", new Vector3(0f, h + 0.35f, 0f), new Vector3(half * 2f + 1.6f, 0.7f, 1.4f), "sandstone_blocks_08", 2.4f, false);
            Box(g, "Board", new Vector3(0f, h - 0.45f, 0f), new Vector3(span, 0.9f, 0.16f), "medieval_wood", 1.6f, false);
            for (int s = -1; s <= 1; s += 2)
            {
                var t = new GameObject("BoardText").AddComponent<TMPro.TextMeshPro>(); t.transform.SetParent(g, false);
                t.transform.localPosition = new Vector3(0f, h - 0.45f, s * 0.1f); t.transform.localRotation = Quaternion.Euler(0f, s > 0 ? 180f : 0f, 0f);
                t.rectTransform.sizeDelta = new Vector2(span - 0.3f, 0.8f); t.alignment = TMPro.TextAlignmentOptions.Center; t.enableAutoSizing = true; t.fontSizeMin = 1f; t.fontSizeMax = 5f;
                t.color = new Color(1f, 0.82f, 0.42f); t.fontStyle = TMPro.FontStyles.Bold; t.text = string.IsNullOrEmpty(number) ? title : number + "  ·  " + title;
            }
            var l = new GameObject("GatewayLight").AddComponent<Light>(); l.transform.SetParent(g, false); l.transform.localPosition = new Vector3(0f, h - 1.2f, 0f);
            l.type = LightType.Point; l.range = 8f; l.intensity = 2.6f; l.color = new Color(1f, 0.7f, 0.36f); l.shadows = LightShadows.None;
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
            nextTint = Wash[rng.Next(Wash.Length)];
            Box(h, "Walls", new Vector3(0f, baseY + ht * 0.5f, 0f), new Vector3(w, ht, d), "white_plaster_rough_01", 2.6f);
            nextTint = Dado[rng.Next(Dado.Length)];
            Box(h, "Band", new Vector3(0f, baseY + 0.45f, 0f), new Vector3(w + 0.06f, 0.9f, d + 0.06f), "white_plaster_rough_01", 2f);
            Roof(h, new Vector3(0f, baseY + ht, 0f), w + 1.1f, d + 1.1f, R(1.5f, 2.2f), thatch ? "thatch_roof_angled" : "clay_roof_tiles_02", thatch ? 2.4f : 1.8f);

            float fz = d * 0.5f;
            nextTint = DoorPaint[rng.Next(DoorPaint.Length)];
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

        /// <summary>Sideways shift of the approach road: straight through the village, then a smooth 6 m bend out in the fields.</summary>
        public static float RoadOffset(float z) => 6f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ZMax + 55f, ZMax + 95f, z));
        /// <summary>Centre of the approach road at this z (on the ground).</summary>
        public static Vector3 RoadPoint(float z) => new Vector3(Centre.x + RoadOffset(z), Top, z);

        /// <summary>Builds the approach road from a Unity Spline: knots every 8 m along the bend, then a flat 8 m road
        /// mesh (UVs in metres, with a collider) and a low kerb on each side, both sampled from the spline.</summary>
        private static void SplineRoad(Transform parent, float z0, float z1)
        {
            var go = new GameObject("MainRoad_South_Spline"); go.transform.SetParent(parent, false);
            var container = go.AddComponent<UnityEngine.Splines.SplineContainer>(); var spline = container.Spline; spline.Clear();
            for (float z = z0; z <= z1 + 0.01f; z += 8f) { Vector3 p = RoadPoint(z); spline.Add(new UnityEngine.Splines.BezierKnot(new Unity.Mathematics.float3(p.x, p.y + 0.02f, p.z)), UnityEngine.Splines.TangentMode.AutoSmooth); }
            float len = UnityEngine.Splines.SplineUtility.CalculateLength(spline, Unity.Mathematics.float4x4.identity);
            int n = Mathf.CeilToInt(len); var verts = new System.Collections.Generic.List<Vector3>(); var uvs = new System.Collections.Generic.List<Vector2>(); var tris = new System.Collections.Generic.List<int>();
            var kerb = new System.Collections.Generic.List<Vector3>(); var kuv = new System.Collections.Generic.List<Vector2>(); var ktri = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n; Vector3 p = (Vector3)UnityEngine.Splines.SplineUtility.EvaluatePosition(spline, t);
                Vector3 tan = ((Vector3)UnityEngine.Splines.SplineUtility.EvaluateTangent(spline, t)); tan.y = 0f; tan.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, tan); float d = t * len;
                verts.Add(p - side * 4f); verts.Add(p + side * 4f); uvs.Add(new Vector2(0f, d / 4f)); uvs.Add(new Vector2(2f, d / 4f));
                foreach (float sg in new[] { -1f, 1f })
                {   // kerb: a 0.3 m wide, 0.1 m high strip on each edge
                    Vector3 a = p + side * sg * 4f, b = p + side * sg * 4.3f;
                    kerb.Add(a); kerb.Add(a + Vector3.up * 0.1f); kerb.Add(b + Vector3.up * 0.1f); kerb.Add(b);
                    kuv.Add(new Vector2(0f, d)); kuv.Add(new Vector2(0.1f, d)); kuv.Add(new Vector2(0.4f, d)); kuv.Add(new Vector2(0.5f, d));
                }
                if (i > 0)
                {
                    int b0 = (i - 1) * 2; tris.AddRange(new[] { b0, b0 + 2, b0 + 1, b0 + 1, b0 + 2, b0 + 3 });
                    for (int k = 0; k < 2; k++)
                    {
                        int q0 = (i - 1) * 8 + k * 4, q1 = i * 8 + k * 4;
                        for (int e = 0; e < 3; e++) { int a0 = q0 + e, a1 = q0 + e + 1, c0 = q1 + e, c1 = q1 + e + 1; if (k == 0) ktri.AddRange(new[] { a0, a1, c0, a1, c1, c0 }); else ktri.AddRange(new[] { a0, c0, a1, a1, c0, c1 }); }
                    }
                }
            }
            var mesh = new Mesh { name = "MainRoad_South" }; mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var road = new GameObject("MainRoad_South"); road.transform.SetParent(go.transform, false);
            road.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = road.AddComponent<MeshRenderer>(); mr.sharedMaterial = ArtUpgrade.Mat("stony_dirt_path");
            if (mpb == null) mpb = new MaterialPropertyBlock();
            mr.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", ArtUpgrade.Tint("stony_dirt_path")); mr.SetPropertyBlock(mpb);
            road.AddComponent<MeshCollider>().sharedMesh = mesh;
            var km = new Mesh { name = "MainRoad_South_Kerbs" }; km.SetVertices(kerb); km.SetUVs(0, kuv); km.SetTriangles(ktri, 0); km.RecalculateNormals(); km.RecalculateBounds();
            var kg = new GameObject("MainRoad_South_Kerbs"); kg.transform.SetParent(go.transform, false);
            kg.AddComponent<MeshFilter>().sharedMesh = km; var kr = kg.AddComponent<MeshRenderer>(); kr.sharedMaterial = ArtUpgrade.Mat("sandstone_blocks_08") ?? ArtUpgrade.Mat("stony_dirt_path");
        }

        public static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, string mat, float tile, bool keepCollider = true)
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
            r.GetPropertyBlock(mpb); mpb.SetVector("_BaseMap_ST", new Vector4(Mathf.Max(0.25f, st.x), Mathf.Max(0.25f, st.y), 0f, 0f));
            if (nextTint.HasValue) { mpb.SetColor("_BaseColor", nextTint.Value); nextTint = null; } else mpb.SetColor("_BaseColor", ArtUpgrade.Tint(mat));
            r.SetPropertyBlock(mpb);
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
