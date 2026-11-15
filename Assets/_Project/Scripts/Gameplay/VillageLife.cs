using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.World;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Fills the village road with life: entrance arch, tea stall, shops with signboards, a well, a bullock cart,
    /// a charpoy, electric poles with sagging wires, hanging street lamps, villagers who walk beside the road
    /// (and wait for the jeep), people chatting at the stalls, one carrying a water pot, low village ambience,
    /// and the investigator's jeep parked at the village entrance.
    /// </summary>
    public sealed class VillageLife : MonoBehaviour
    {
        public static VillageLife Instance { get; private set; }
        public static int NpcCount { get; private set; }
        public readonly List<Transform> Npcs = new List<Transform>();
        public Transform TeaStall { get; private set; }
        public Vector3 TeaStallStand { get; private set; }
        private static MaterialPropertyBlock mpb;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot() { SceneManager.sceneLoaded += (s, m) => { if (s.name == Constants.Courtyard) new GameObject("VillageLife").AddComponent<VillageLife>(); }; if (SceneManager.GetActiveScene().name == Constants.Courtyard) new GameObject("VillageLife").AddComponent<VillageLife>(); }

        private float wait = 0f; private bool built;
        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (built) return;
            wait += Time.unscaledDeltaTime;
            if (GameObject.Find("Village") == null) { if (wait > 8f) enabled = false; return; }   // the village is built by LookTuner a moment after load
            built = true; Build();
        }

        private static GameObject Box(Transform p, string n, Vector3 world, Vector3 size, string mat, float tile, Color? tint = null, bool collider = true, float yaw = 0f)
        {
            var g = Village.Box(p, n, Vector3.zero, size, mat, tile, collider); g.transform.position = world; g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (tint.HasValue) { var r = g.GetComponent<Renderer>(); if (mpb == null) mpb = new MaterialPropertyBlock(); r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", tint.Value); r.SetPropertyBlock(mpb); }
            return g;
        }
        private static GameObject Cyl(Transform p, string n, Vector3 a, Vector3 b, float r, string mat, Color? tint = null)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = n; g.transform.SetParent(p, false); Destroy(g.GetComponent<Collider>());
            Vector3 d = b - a; g.transform.position = (a + b) * 0.5f; g.transform.up = d.normalized; g.transform.localScale = new Vector3(r * 2f, d.magnitude * 0.5f, r * 2f);
            var m = ArtUpgrade.Mat(mat); var rr = g.GetComponent<Renderer>(); if (m != null) rr.sharedMaterial = m;
            if (mpb == null) mpb = new MaterialPropertyBlock(); rr.GetPropertyBlock(mpb); mpb.SetVector("_BaseMap_ST", new Vector4(1f, Mathf.Max(1f, d.magnitude), 0f, 0f)); mpb.SetColor("_BaseColor", tint ?? Color.white); rr.SetPropertyBlock(mpb);
            return g;
        }
        private static void Sign(Transform p, Vector3 pos, float yaw, string text, Vector2 size, Color board)
        {
            Box(p, "Signboard", pos, new Vector3(size.x, size.y, 0.06f), "weathered_planks", 1.2f, board, false, yaw);
            for (int s = -1; s <= 1; s += 2)
            {
                var t = new GameObject("SignText").AddComponent<TMPro.TextMeshPro>(); t.transform.SetParent(p, false);
                t.transform.position = pos + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, s * 0.09f);   // well clear of the board, so the back text never shows through t.transform.rotation = Quaternion.Euler(0f, yaw + (s > 0 ? 0f : 180f), 0f);   // each face reads the right way round
                t.rectTransform.sizeDelta = size * 0.92f; t.alignment = TMPro.TextAlignmentOptions.Center; t.enableAutoSizing = true; t.fontSizeMin = 0.5f; t.fontSizeMax = 4f;
                t.color = new Color(1f, 0.95f, 0.85f); t.fontStyle = TMPro.FontStyles.Bold; t.text = text;
            }
        }

        private void Build()
        {
            var root = new GameObject("VillageLife_Props").transform;
            Vector3 c = Village.Centre; float top = Village.Top, z0 = Village.ZMax;
            Vector3 P(float x, float z) => new Vector3(c.x + Village.RoadOffset(z) + x, top, z);   // roadside positions follow the spline road's bend
            var rng = new System.Random(4242); float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            Village.Gateway(root, Village.RoadStart + Vector3.forward * -2f, 0f, 8.4f, "SURYAPUR VILLAGE", "");

            // ---- tea stall (west side of the road)
            Vector3 ts = P(-7.2f, z0 + 22f);
            var tea = new GameObject("TeaStall").transform; tea.SetParent(root, false); tea.position = ts; TeaStall = tea; TeaStallStand = ts + new Vector3(2.6f, 0f, 0f);
            Box(tea, "Counter", ts + new Vector3(0.6f, 0.5f, 0f), new Vector3(0.9f, 1f, 3.2f), "weathered_planks", 1.2f, new Color(0.85f, 0.6f, 0.4f));
            Box(tea, "Back", ts + new Vector3(-1.2f, 1.3f, 0f), new Vector3(0.15f, 2.6f, 3.4f), "medieval_wood", 1.4f);
            for (int i = -1; i <= 1; i += 2) Cyl(tea, "Post", ts + new Vector3(1.3f, 0f, i * 1.6f), ts + new Vector3(1.3f, 2.5f, i * 1.6f), 0.06f, "medieval_wood");
            var aw = Box(tea, "Awning", ts + new Vector3(0.6f, 2.55f, 0f), new Vector3(3.2f, 0.06f, 3.8f), "white_plaster_rough_01", 1f, new Color(0.95f, 0.45f, 0.12f), false); aw.transform.rotation = Quaternion.Euler(0f, 0f, -12f);
            ArtUpgrade.Place(tea, "brass_pot_01", ts + new Vector3(0.6f, 1.0f, -0.8f), 2.6f, 0f, false);
            ArtUpgrade.Place(tea, "brass_goblets", ts + new Vector3(0.6f, 1.0f, 0.4f), 1.6f, 40f, false);
            ArtUpgrade.Place(tea, "wooden_bowl_02", ts + new Vector3(0.6f, 1.0f, 1.1f), 3f, 0f, false);
            Sign(tea, ts + new Vector3(1.35f, 2.95f, 0f), 90f, "CHAI", new Vector2(2.2f, 0.6f), new Color(0.75f, 0.1f, 0.08f));
            Charpoy(tea, ts + new Vector3(3.0f, 0f, 2.8f), 15f);
            var tl = new GameObject("TeaStallLight").AddComponent<Light>(); tl.transform.SetParent(tea, false); tl.transform.position = ts + new Vector3(0.8f, 2.3f, 0f); tl.type = LightType.Point; tl.range = 7f; tl.intensity = 2.2f; tl.color = new Color(1f, 0.72f, 0.4f); tl.shadows = LightShadows.None;

            // ---- shops (east side)
            Shop(root, P(7.4f, z0 + 18f), "KIRANA STORE", new Color(0.12f, 0.35f, 0.7f), new Color(0.2f, 0.55f, 0.3f));
            Shop(root, P(7.4f, z0 + 33f), "FLOWERS & DIYAS", new Color(0.8f, 0.45f, 0.05f), new Color(0.85f, 0.2f, 0.45f));
            Shop(root, P(-7.4f, z0 + 40f), "TAILOR", new Color(0.45f, 0.2f, 0.55f), new Color(0.15f, 0.5f, 0.55f), true);

            // ---- well, cart, poles and wires, lamps
            Well(root, P(-6.8f, z0 + 54f));
            Cart(root, P(6.2f, z0 + 48f), 8f);
            Vector3 lastTop = Vector3.zero; bool have = false;
            for (int i = 0; i < 7; i++)
            {
                Vector3 b = P(5.2f, z0 + 6f + i * 11f);
                Box(root, "ElectricPole", b + Vector3.up * 3.6f, new Vector3(0.22f, 7.2f, 0.22f), "sandstone_blocks_08", 3f, new Color(0.75f, 0.74f, 0.72f));
                Box(root, "PoleCrossarm", b + Vector3.up * 6.9f, new Vector3(1.4f, 0.1f, 0.12f), "medieval_wood", 1f, null, false);
                Vector3 topP = b + Vector3.up * 6.95f;
                if (have) for (int w = -1; w <= 1; w += 2) Wire(root, lastTop + Vector3.right * w * 0.6f, topP + Vector3.right * w * 0.6f);
                lastTop = topP; have = true;
                if (i % 2 == 0)
                {   // street lamp hanging on an arm over the road
                    Cyl(root, "LampArm", b + Vector3.up * 5.6f, b + new Vector3(-1.6f, 5.8f, 0f), 0.04f, "medieval_wood", new Color(0.3f, 0.3f, 0.3f));
                    var lan = ArtUpgrade.Place(root, "wooden_lantern_01", b + new Vector3(-1.6f, 5.15f, 0f), 1.4f, 0f, false);
                    var l = new GameObject("StreetLamp").AddComponent<Light>(); l.transform.SetParent(root, false); l.transform.position = b + new Vector3(-1.6f, 5.0f, 0f);
                    l.type = LightType.Point; l.range = 11f; l.intensity = 2.6f; l.color = new Color(1f, 0.74f, 0.42f); l.shadows = LightShadows.None;
                }
            }
            // a few sacks of rice and baskets of marigolds by the shops
            for (int i = 0; i < 5; i++) ArtUpgrade.Place(root, i % 2 == 0 ? "ceramic_pot" : "wooden_bowl_02", P(R(5.6f, 6.4f), z0 + 15f + i * 1.1f), R(1.4f, 2.2f), R(0f, 360f));

            SpawnVillagers(root, c, top, z0, rng);
            Ambience();
            if (Jeep.Instance == null && PlayerPrefs.GetInt("Jeep_Parked_Courtyard", 1) == 1)
            {
                var j = Jeep.Spawn(Village.RoadStart + new Vector3(2.2f, 0.6f, -9f), Quaternion.LookRotation(Vector3.back));
                if (j != null) j.transform.SetParent(null);
            }
            Debug.Log($"[VillageLife] built: {NpcCount} villagers, jeep {(Jeep.Instance != null ? "parked" : "missing")}");
        }

        private static void Charpoy(Transform p, Vector3 at, float yaw)
        {
            var g = new GameObject("Charpoy").transform; g.SetParent(p, false); g.position = at; g.rotation = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < 4; i++) { var leg = Box(g, "Leg", Vector3.zero, new Vector3(0.08f, 0.45f, 0.08f), "medieval_wood", 1f); leg.transform.localPosition = new Vector3(i % 2 == 0 ? -0.45f : 0.45f, 0.22f, i < 2 ? -0.95f : 0.95f); }
            var frame = Box(g, "Frame", Vector3.zero, new Vector3(1.0f, 0.06f, 2.0f), "medieval_wood", 1f, null, false); frame.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            var web = Box(g, "RopeWeave", Vector3.zero, new Vector3(0.9f, 0.03f, 1.9f), "thatch_roof_angled", 0.6f, new Color(0.95f, 0.85f, 0.6f), false); web.transform.localPosition = new Vector3(0f, 0.48f, 0f);
        }

        private static void Shop(Transform p, Vector3 at, string name, Color awning, Color shutter, bool west = false)
        {
            float face = west ? 90f : -90f; var g = new GameObject("Shop_" + name).transform; g.SetParent(p, false); g.position = at; g.rotation = Quaternion.Euler(0f, face, 0f);
            Vector3 L(float x, float y, float z) => g.TransformPoint(new Vector3(x, y, z));
            Box(g, "ShopWalls", L(0f, 1.7f, -1.8f), new Vector3(4.6f, 3.4f, 3.2f), "white_plaster_rough_01", 2.4f, Color.Lerp(awning, Color.white, 0.55f), true, face);
            Box(g, "ShopFloor", L(0f, 0.2f, 0.2f), new Vector3(4.6f, 0.4f, 1.2f), "sandstone_blocks_08", 2f, null, true, face);
            Box(g, "Shutter", L(-1.5f, 1.6f, -0.18f), new Vector3(1.2f, 2.4f, 0.06f), "weathered_planks", 1f, shutter, false, face);
            Box(g, "Counter", L(0.5f, 0.85f, 0.4f), new Vector3(2.6f, 0.9f, 0.6f), "weathered_planks", 1.2f, new Color(0.8f, 0.6f, 0.4f), true, face);
            var aw = Box(g, "Awning", L(0f, 3.05f, 0.7f), new Vector3(4.8f, 0.05f, 1.9f), "white_plaster_rough_01", 1f, awning, false, face); aw.transform.rotation = Quaternion.Euler(14f, face, 0f) ;
            Sign(g, L(0f, 3.6f, -0.18f), face + 180f, name, new Vector2(4.0f, 0.65f), new Color(0.1f, 0.1f, 0.12f));
            ArtUpgrade.Place(g, "brass_vase_03", L(-0.2f, 1.3f, 0.4f), 2.6f, 0f, false); ArtUpgrade.Place(g, "ceramic_vase_01", L(1.1f, 1.3f, 0.4f), 1.6f, 0f, false);
            var l = new GameObject("ShopLight").AddComponent<Light>(); l.transform.SetParent(g, false); l.transform.position = L(0f, 2.6f, 0.5f); l.type = LightType.Point; l.range = 6f; l.intensity = 1.8f; l.color = new Color(1f, 0.8f, 0.55f); l.shadows = LightShadows.None;
        }

        private static void Well(Transform p, Vector3 at)
        {
            var g = new GameObject("Well").transform; g.SetParent(p, false); g.position = at;
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f; var s = Box(g, "WellStone", at + new Vector3(Mathf.Cos(a) * 0.95f, 0.45f, Mathf.Sin(a) * 0.95f), new Vector3(0.5f, 0.9f, 0.32f), "sandstone_blocks_08", 1f);
                s.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
            }
            Box(g, "WellWater", at + Vector3.up * 0.3f, new Vector3(1.5f, 0.02f, 1.5f), "white_plaster_rough_01", 1f, new Color(0.05f, 0.1f, 0.12f), false);
            for (int s = -1; s <= 1; s += 2) Cyl(g, "WellPost", at + new Vector3(s * 1.0f, 0.8f, 0f), at + new Vector3(s * 1.0f, 2.3f, 0f), 0.06f, "medieval_wood");
            Cyl(g, "WellBeam", at + new Vector3(-1.05f, 2.25f, 0f), at + new Vector3(1.05f, 2.25f, 0f), 0.05f, "medieval_wood");
            ArtUpgrade.Place(g, "brass_pot_02", at + new Vector3(0.2f, 1.25f, 0f), 2f, 0f, false);
        }

        private static void Cart(Transform p, Vector3 at, float yaw)
        {
            var g = new GameObject("BullockCart").transform; g.SetParent(p, false); g.position = at; g.rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 L(float x, float y, float z) => g.TransformPoint(new Vector3(x, y, z));
            Box(g, "CartBed", L(0f, 0.95f, 0f), new Vector3(1.5f, 0.12f, 2.4f), "weathered_planks", 1.2f, null, true, yaw);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(g, "CartSide", L(s * 0.72f, 1.2f, 0f), new Vector3(0.06f, 0.4f, 2.4f), "weathered_planks", 1.2f, null, false, yaw);
                var w = Cyl(g, "CartWheel", L(s * 0.9f, 0.7f, 0.2f) - g.right * s * 0.05f, L(s * 0.9f, 0.7f, 0.2f) + g.right * s * 0.05f, 0.7f, "medieval_wood", new Color(0.8f, 0.6f, 0.4f));
                Cyl(g, "CartShaft", L(s * 0.35f, 0.95f, 1.1f), L(s * 0.25f, 0.55f, 3.1f), 0.05f, "medieval_wood");
            }
            for (int i = 0; i < 3; i++) ArtUpgrade.Place(g, "ceramic_pot", L(-0.4f + i * 0.4f, 1.02f, -0.5f + i * 0.3f), 1.5f, i * 50f, false);
        }

        private static void Wire(Transform p, Vector3 a, Vector3 b)
        {
            const int n = 8; Vector3 prev = a;
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n; Vector3 q = Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.45f;
                Cyl(p, "Wire", prev, q, 0.012f, "medieval_wood", new Color(0.05f, 0.05f, 0.05f)); prev = q;
            }
        }

        // ------------------------------------------------------------------ villagers
        private static readonly Color[] Saree = {
            new Color(0.95f, 0.35f, 0.28f), // Terracotta crimson
            new Color(0.24f, 0.52f, 0.85f), // Indigo coastal blue
            new Color(0.96f, 0.72f, 0.18f), // Turmeric marigold
            new Color(0.25f, 0.68f, 0.45f), // Temple moss green
            new Color(0.85f, 0.36f, 0.60f), // Lotus pink
            new Color(0.92f, 0.48f, 0.16f), // Saffron dawn
            new Color(0.30f, 0.75f, 0.80f), // Peacock turquoise
            new Color(0.92f, 0.88f, 0.82f)  // Raw silk ivory
        };
        private static readonly Color[] DhotiKurta = {
            new Color(0.94f, 0.92f, 0.87f), // Khadi cotton white
            new Color(0.82f, 0.60f, 0.35f), // Earthen ochre
            new Color(0.92f, 0.52f, 0.18f), // Saffron kurta
            new Color(0.55f, 0.40f, 0.30f), // Clay brown
            new Color(0.32f, 0.48f, 0.62f), // Village blue
            new Color(0.68f, 0.32f, 0.26f)  // Brick red
        };
        private static readonly Color[] SkinTones = {
            new Color(0.92f, 0.80f, 0.70f), // Warm wheatish
            new Color(0.78f, 0.62f, 0.50f), // Caramel bronze
            new Color(0.65f, 0.48f, 0.36f), // Dusky earthen
            new Color(0.52f, 0.36f, 0.26f)  // Deep terracotta
        };

        private void SpawnVillagers(Transform root, Vector3 c, float top, float z0, System.Random rng)
        {
            var pfFemale = Resources.Load<GameObject>("NPC/Villager_NPC");
            var pfMale = Resources.Load<GameObject>("NPC/Villager_Male_NPC") ?? pfFemale;
            NpcCount = 0;
            if (pfFemale == null) { Debug.LogWarning("[VillageLife] Resources/NPC/Villager_NPC missing (run Part2Tools.BuildNpc)"); return; }
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            Vector3 P(float x, float z) => new Vector3(c.x + Village.RoadOffset(z) + x, top, z);   // roadside positions follow the spline road's bend
            int k = 0;
            Transform Spawn(Vector3 at, float yaw, bool isMale)
            {
                var pf = isMale ? pfMale : pfFemale;
                var g = Instantiate(pf, at, Quaternion.Euler(0f, yaw, 0f), root);
                g.name = (isMale ? "Villager_Male_" : "Villager_Female_") + k;
                g.transform.localScale = Vector3.one * R(isMale ? 0.98f : 0.93f, isMale ? 1.05f : 1.01f);
                Color clothCol = isMale ? DhotiKurta[k % DhotiKurta.Length] : Saree[k % Saree.Length];
                Color skinCol = SkinTones[k % SkinTones.Length];
                foreach (var r in g.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null) continue;
                        string mName = mats[i].name.ToLower();
                        if (mpb == null) mpb = new MaterialPropertyBlock();
                        r.GetPropertyBlock(mpb, i);
                        if (mName.Contains("outfit") || mName.Contains("material_01") || mName.Contains("body") || mName.Contains("cloth") || mName.Contains("kurta") || mName.Contains("dhoti"))
                            mpb.SetColor("_BaseColor", clothCol);
                        else if (mName.Contains("head") || mName.Contains("skin") || mName.Contains("face"))
                            mpb.SetColor("_BaseColor", skinCol);
                        r.SetPropertyBlock(mpb, i);
                    }
                }
                foreach (var col in g.GetComponentsInChildren<Collider>()) Destroy(col);   // villagers step aside; they must not stop the jeep like a wall
                Npcs.Add(g.transform); k++; NpcCount++; return g.transform;
            }
            // walkers along both edges of the road (mix of men and women)
            for (int i = 0; i < 5; i++)
            {
                float side = i % 2 == 0 ? -4.6f : 4.6f;
                bool male = i % 2 == 1;
                var w = Spawn(P(side, z0 + R(8f, 60f)), 0f, male).gameObject.AddComponent<RoadWalker>();
                w.a = P(side, z0 + 6f); w.b = P(side, z0 + 66f); w.speed = R(1.0f, 1.35f);
                if (i == 1) ArtUpgrade.Place(w.transform, "brass_pot_02", w.transform.position + Vector3.up * 1.72f, 1.9f, 0f, false);   // water pot carried on the head
            }
            // chatting at the tea stall and the shops (idle, facing each other)
            Vector3 ts = TeaStall != null ? TeaStall.position : P(-7f, z0 + 22f);
            Spawn(ts + new Vector3(2.0f, 0f, -1.2f), 70f, true);   // male villager at tea stall
            Spawn(ts + new Vector3(3.1f, 0f, -0.6f), 250f, false);  // female villager talking
            Spawn(P(5.4f, z0 + 18.5f), 90f, true);                  // male shopkeeper/patron
            Spawn(P(4.6f, z0 + 33.2f), 80f, false);                 // female shopper
            Spawn(P(-5.6f, z0 + 54f), 270f, false);                 // woman at the well
            Spawn(P(-4.2f, z0 + 40.5f), 270f, true);                // man at the tailor
            foreach (var n in Npcs) if (n.GetComponent<RoadWalker>() == null) n.gameObject.AddComponent<IdleLook>();
        }

        private static void Ambience()
        {
            foreach (var (clip, vol) in new[] { ("amb_village", 0.22f), ("amb_birds", 0.16f) })
            {
                var c = AudioManager.Instance != null ? AudioManager.Instance.GetClip(clip) : Resources.Load<AudioClip>("Audio/" + clip);
                if (c == null) continue;
                var s = new GameObject("Ambience_" + clip).AddComponent<AudioSource>(); s.clip = c; s.loop = true; s.volume = vol; s.spatialBlend = 0f; s.Play();
                AudioManager.Route(s, "Ambience");
            }
        }
    }

    /// <summary>Walks back and forth beside the road; waits while the jeep comes past.</summary>
    public sealed class RoadWalker : MonoBehaviour
    {
        public Vector3 a, b; public float speed = 1.2f; private bool toB = true; private float pause;
        private void Update()
        {
            if (pause > 0f) { pause -= Time.deltaTime; return; }
            var j = Jeep.Instance;
            if (j != null && j.Car != null && j.Car.CurrentSpeed > 1f)
            {
                Vector3 rel = j.transform.position - transform.position; rel.y = 0f;
                if (rel.magnitude < 7f && Vector3.Dot(j.Car.GetComponent<Rigidbody>().linearVelocity, -rel) > 0f) return;   // stand and let it pass
            }
            Vector3 goal = toB ? b : a; Vector3 d = goal - transform.position; d.y = 0f;
            if (d.magnitude < 0.4f) { toB = !toB; pause = Random.Range(1.5f, 4f); return; }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 4f);
            Vector3 np = transform.position + d.normalized * speed * Time.deltaTime;
            if (Physics.Raycast(np + Vector3.up * 1.5f, Vector3.down, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore) && h.collider.GetComponentInParent<Animator>() == null) np.y = h.point.y;
            transform.position = np;
        }
    }

    /// <summary>Standing villager: turns the whole body now and then to look at whoever is close (the player or the jeep).</summary>
    public sealed class IdleLook : MonoBehaviour
    {
        private Quaternion home; private float t;
        private void Start() { home = transform.rotation; t = Random.Range(0f, 4f); }
        private void Update()
        {
            t += Time.deltaTime; if (t < 0.5f) return; t = 0f;
            var pc = FindAnyObjectByType<PlayerController>(); Quaternion goal = home;
            if (pc != null) { Vector3 d = pc.transform.position - transform.position; d.y = 0f; if (d.magnitude < 5f) goal = Quaternion.Slerp(home, Quaternion.LookRotation(d), 0.6f); }
            StopAllCoroutines(); StartCoroutine(Turn(goal));
        }
        private System.Collections.IEnumerator Turn(Quaternion goal) { for (float k = 0; k < 1f; k += Time.deltaTime * 2f) { transform.rotation = Quaternion.Slerp(transform.rotation, goal, k); yield return null; } }
    }
}
