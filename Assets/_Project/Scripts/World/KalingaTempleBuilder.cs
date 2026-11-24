using UnityEngine;

namespace LegacyOfTemples.World
{
    /// <summary>
    /// Clean modular Kalinga-style sandstone temple (Rekha Deula + Jagamohana pavilion), grounded strictly at baseY
    /// with solid colliders and zero floating parts or broken raw blend meshes. Uses only already-attributed Poly Haven materials.
    /// </summary>
    public static class KalingaTempleBuilder
    {
        public struct BuildResult
        {
            public Transform root;
            public Vector3 towerTop;
            public Bounds footprint;
        }

        public static BuildResult Build(Transform parent, string objectName, Vector3 groundCenter, float scale = 1f)
        {
            var go = new GameObject(objectName);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = groundCenter;
            go.transform.localScale = Vector3.one * scale;
            Transform root = go.transform;

            Material brass = Tinted("clay_plaster", new Color(0.84f, 0.65f, 0.24f), 0.7f, 0.85f);
            Material redCloth = Tinted("clay_plaster", new Color(0.75f, 0.14f, 0.10f), 0.15f, 0f);
            Material wood = ArtUpgrade.Mat("medieval_wood") ?? Tinted("weathered_planks", new Color(0.32f, 0.2f, 0.12f), 0.25f, 0f);

            // 1. Broad stepped Jagati foundation plinth (grounded cleanly from y = -0.4 to +1.8)
            Box(root, "Jagati_Base_Lower", new Vector3(0f, 0.25f, 0f), new Vector3(24f, 1.3f, 24f), "red_sandstone_wall", true);
            Box(root, "Jagati_Base_Upper", new Vector3(0f, 1.35f, 0f), new Vector3(20f, 0.9f, 20f), "sandstone_blocks_08", true);

            // South access stairway ascending Jagati (facing -Z)
            for (int i = 0; i < 6; i++)
            {
                float stepY = 0.15f + i * 0.28f;
                float stepZ = -12.8f + i * 0.65f;
                Box(root, "TempleStep_" + (i + 1), new Vector3(0f, stepY, stepZ), new Vector3(6.2f, 0.3f, 0.7f), "red_sandstone_pavement", true);
            }

            // 2. Jagamohana (Front Pillared Porch Hall at z = -5.5, y = 1.8..8.8)
            Box(root, "Jagamohana_Body", new Vector3(0f, 4.4f, -4.5f), new Vector3(13f, 5.2f, 8.5f), "sandstone_blocks_08", true);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                Box(root, "Jagamohana_FrontPillar", new Vector3(sx * 3.4f, 4.3f, -8.8f), new Vector3(1.1f, 5.0f, 1.1f), "red_sandstone_wall", true);
                Box(root, "Porch_CornerPillar", new Vector3(sx * 6.2f, 4.3f, -8.6f), new Vector3(1.1f, 5.0f, 1.1f), "red_sandstone_wall", true);
                // Ornate pedestal oil lamps grounded on plinth at porch flank
                Box(root, "PorchLampPedestal", new Vector3(sx * 5.2f, 2.2f, -9.4f), new Vector3(0.9f, 0.8f, 0.9f), "sandstone_blocks_08", true);
                var lamp = ArtUpgrade.Place(root, "brass_diya_lantern", root.TransformPoint(new Vector3(sx * 5.2f, 2.6f, -9.4f)), 1.5f * scale, 0f, false);
                var l = new GameObject("PorchLight").AddComponent<Light>();
                l.transform.SetParent(root, false);
                l.transform.localPosition = new Vector3(sx * 5.2f, 3.5f, -9.4f);
                l.type = LightType.Point; l.range = 10f * scale; l.intensity = 2.4f; l.color = new Color(1f, 0.7f, 0.34f); l.shadows = LightShadows.None;
            }

            // Carved closed Sanctum Brass-Bound Doors on the south porch face (solid wall behind, zero black holes)
            Box(root, "Sanctum_RecessBack", new Vector3(0f, 4.0f, -8.4f), new Vector3(4.2f, 4.2f, 0.6f), "red_sandstone_wall", true);
            var doorL = Primitive(root, PrimitiveType.Cube, "Sanctum_Door_Left", new Vector3(-0.92f, 3.85f, -8.65f), new Vector3(1.75f, 3.8f, 0.16f), wood, true);
            var doorR = Primitive(root, PrimitiveType.Cube, "Sanctum_Door_Right", new Vector3(0.92f, 3.85f, -8.65f), new Vector3(1.75f, 3.8f, 0.16f), wood, true);
            Primitive(doorL.transform, PrimitiveType.Sphere, "BrassRing_L", new Vector3(0.28f, 0f, -0.6f), new Vector3(0.18f, 0.18f, 0.3f), brass, false);
            Primitive(doorR.transform, PrimitiveType.Sphere, "BrassRing_R", new Vector3(-0.28f, 0f, -0.6f), new Vector3(0.18f, 0.18f, 0.3f), brass, false);

            // Jagamohana stepped pyramidal Pidha roof tiers
            for (int tier = 0; tier < 5; tier++)
            {
                float w = 14.4f - tier * 2.0f;
                float d = 10.0f - tier * 1.3f;
                Box(root, "Pidha_Tier_" + tier, new Vector3(0f, 7.25f + tier * 0.72f, -4.5f), new Vector3(w, 0.68f, d), tier % 2 == 0 ? "red_sandstone_wall" : "sandstone_blocks_08", false);
            }
            Primitive(root, PrimitiveType.Cylinder, "Pidha_Kalash", new Vector3(0f, 11.1f, -4.5f), new Vector3(0.9f, 0.55f, 0.9f), brass, false);

            // 3. Main Rekha Deula (Shikhara Spire Tower at z = +3.8)
            Vector3 towerBase = new Vector3(0f, 1.8f, 3.8f);
            Box(root, "Bada_SanctumCore", towerBase + Vector3.up * 3.8f, new Vector3(12.2f, 7.6f, 12.2f), "red_sandstone_wall", true);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                Box(root, "Bada_Buttress_Side", towerBase + new Vector3(sx * 6.3f, 3.6f, 0f), new Vector3(0.8f, 7.2f, 5.2f), "sandstone_blocks_08", true);
                Box(root, "Bada_Buttress_Z", towerBase + new Vector3(0f, 3.6f, sx * 6.3f), new Vector3(5.2f, 7.2f, 0.8f), "sandstone_blocks_08", true);
            }

            // Curved corbelled Gandi spire tiers
            int spireSteps = 11;
            float spireY = 9.4f;
            for (int i = 0; i < spireSteps; i++)
            {
                float t = i / (float)(spireSteps - 1);
                float ease = t * t * 0.55f + t * 0.45f;
                float size = Mathf.Lerp(11.6f, 3.8f, ease);
                float h = 1.35f;
                Box(root, "Gandi_Tier_" + i, new Vector3(0f, spireY + i * h + h * 0.5f, 3.8f), new Vector3(size, h * 0.96f, size), i % 2 == 0 ? "sandstone_blocks_08" : "red_sandstone_wall", false);
                if (size > 5f)
                {
                    // Vertical ribbed ratha pilasters anchored flush to spire walls
                    Box(root, "Ratha_X_" + i, new Vector3(0f, spireY + i * h + h * 0.5f, 3.8f), new Vector3(size + 0.55f, h * 0.9f, size * 0.44f), "red_sandstone_wall", false);
                    Box(root, "Ratha_Z_" + i, new Vector3(0f, spireY + i * h + h * 0.5f, 3.8f), new Vector3(size * 0.44f, h * 0.9f, size + 0.55f), "red_sandstone_wall", false);
                }
            }

            // Beki neck, Amalaka stone disc crown, and Kalash finial
            float crownY = spireY + spireSteps * 1.35f;
            Primitive(root, PrimitiveType.Cylinder, "Beki_Neck", new Vector3(0f, crownY + 0.4f, 3.8f), new Vector3(2.8f, 0.55f, 2.8f), MaterialOf("sandstone_blocks_08"), false);
            Primitive(root, PrimitiveType.Cylinder, "Amalaka_Crown", new Vector3(0f, crownY + 1.25f, 3.8f), new Vector3(4.6f, 0.65f, 4.6f), MaterialOf("red_sandstone_wall"), false);
            Primitive(root, PrimitiveType.Cylinder, "Khapuri_Cap", new Vector3(0f, crownY + 2.0f, 3.8f), new Vector3(2.5f, 0.4f, 2.5f), brass, false);
            Primitive(root, PrimitiveType.Sphere, "Kalash_Finial", new Vector3(0f, crownY + 2.8f, 3.8f), new Vector3(1.1f, 1.3f, 1.1f), brass, false);
            Primitive(root, PrimitiveType.Cube, "Chakra_Wheel", new Vector3(0f, crownY + 3.9f, 3.8f), new Vector3(1.3f, 1.3f, 0.18f), brass, false);

            // Red silk banner trim along porch lintel
            Box(root, "Temple_Toran_Trim", new Vector3(0f, 6.45f, -8.85f), new Vector3(8.2f, 0.35f, 0.22f), "clay_plaster", false).GetComponent<Renderer>().sharedMaterial = redCloth;

            Vector3 topWorld = root.TransformPoint(new Vector3(0f, crownY + 4.4f, 3.8f));
            var rends = root.GetComponentsInChildren<Renderer>();
            Bounds b = rends.Length > 0 ? rends[0].bounds : new Bounds(groundCenter, Vector3.one * 24f);
            foreach (var r in rends) b.Encapsulate(r.bounds);

            return new BuildResult { root = root, towerTop = topWorld, footprint = b };
        }

        private static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, string matName, bool solid)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = size;
            if (!solid) { var c = g.GetComponent<Collider>(); if (c != null) Object.Destroy(c); }
            var mat = ArtUpgrade.Mat(matName);
            if (mat != null)
            {
                var r = g.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetVector("_BaseMap_ST", new Vector4(Mathf.Max(1f, size.x / 2.5f), Mathf.Max(1f, Mathf.Max(size.y, size.z) / 2.5f), 0f, 0f));
                r.SetPropertyBlock(mpb);
            }
            return g;
        }

        private static GameObject Primitive(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale, Material mat, bool solid)
        {
            var g = GameObject.CreatePrimitive(type);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            if (!solid) { var c = g.GetComponent<Collider>(); if (c != null) Object.Destroy(c); }
            if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
            return g;
        }

        private static Material MaterialOf(string n) => ArtUpgrade.Mat(n) ?? new Material(Shader.Find("Universal Render Pipeline/Lit"));

        private static Material Tinted(string source, Color c, float smooth, float metal)
        {
            var src = ArtUpgrade.Mat(source);
            var m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            return m;
        }
    }
}
