using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// GTA / Free Fire style compact square corner Minimap HUD in the bottom-left corner.
    /// Displays player position & heading arrow, road network, nearby zone landmarks, Jeep blip,
    /// and a directional objective beacon + distance readout.
    /// </summary>
    public sealed class MinimapHud : MonoBehaviour
    {
        public static MinimapHud Instance { get; private set; }
        public static bool Visible { get; private set; }

        private const float MapSize = 196f;
        private const float WorldHalfSpan = 72f; // metres shown from centre to edge
        private const float Scale = (MapSize * 0.5f) / WorldHalfSpan;

        private Canvas canvas;
        private RectTransform rootRect;
        private RectTransform worldLayer;
        private RectTransform playerArrow;
        private RectTransform jeepBlip;
        private RectTransform objectiveBlip;
        private Image objectiveImg;
        private TextMeshProUGUI headerText;
        private TextMeshProUGUI footerText;
        private readonly List<(RectTransform rt, Vector2 center, float yaw)> roads = new List<(RectTransform, Vector2, float)>();
        private readonly List<(RectTransform rt, int zoneIdx)> zoneBlips = new List<(RectTransform, int)>();
        private Texture2D arrowTex, diamondTex;
        private Sprite arrowSprite, diamondSprite;
        private bool builtTempleRing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("MinimapHud");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<MinimapHud>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSprites();
            BuildUi();
        }

        private void BuildSprites()
        {
            // Sharp directional chevron triangle pointing UP (+Y) for player & clamped arrow
            arrowTex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float nx = (x - 15.5f) / 15.5f;
                    float ny = (y - 15.5f) / 15.5f;
                    float halfW = (0.85f - ny) * 0.55f;
                    bool inside = ny <= 0.85f && ny >= -0.75f && Mathf.Abs(nx) <= halfW && !(ny < -0.25f && Mathf.Abs(nx) < (-0.25f - ny) * 0.85f);
                    arrowTex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            arrowTex.Apply();

            // Diamond blip texture
            diamondTex = new Texture2D(24, 24, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    float d = Mathf.Abs(x - 11.5f) + Mathf.Abs(y - 11.5f);
                    diamondTex.SetPixel(x, y, d <= 9.5f ? Color.white : Color.clear);
                }
            }
            diamondTex.Apply();
            arrowSprite = Sprite.Create(arrowTex, new Rect(0, 0, arrowTex.width, arrowTex.height), new Vector2(0.5f, 0.5f));
            diamondSprite = Sprite.Create(diamondTex, new Rect(0, 0, diamondTex.width, diamondTex.height), new Vector2(0.5f, 0.5f));
        }

        private void BuildUi()
        {
            var go = new GameObject("MinimapCanvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 18;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Root container bottom-left (28, 28)
            var rootObj = new GameObject("MinimapRoot", typeof(RectTransform));
            rootObj.transform.SetParent(go.transform, false);
            rootRect = rootObj.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.zero;
            rootRect.pivot = Vector2.zero;
            rootRect.anchoredPosition = new Vector2(28f, 28f);
            rootRect.sizeDelta = new Vector2(MapSize, MapSize + 48f);

            // Header bar above radar square
            var hdrBg = MakeBox(rootRect, "Header", new Vector2(0f, MapSize + 24f), new Vector2(MapSize, 24f), new Color(0.05f, 0.06f, 0.08f, 0.90f));
            AddOutline(hdrBg.gameObject, new Color(1f, 0.784f, 0.341f, 0.45f), 1.5f);
            headerText = MakeLabel(hdrBg, "HeaderLabel", 14f, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.784f, 0.341f, 1f));
            headerText.rectTransform.offsetMin = new Vector2(8f, 0f);
            headerText.rectTransform.offsetMax = new Vector2(-8f, 0f);

            // Map square panel in the middle (y = 24 .. 220)
            var mapPanel = MakeBox(rootRect, "RadarBox", new Vector2(0f, 24f), new Vector2(MapSize, MapSize), new Color(0.06f, 0.07f, 0.10f, 0.88f));
            AddOutline(mapPanel.gameObject, new Color(1f, 0.784f, 0.341f, 0.82f), 2f);

            // Clipped viewport inside border
            var clip = new GameObject("RadarMask", typeof(RectTransform));
            clip.transform.SetParent(mapPanel, false);
            var clipRt = clip.GetComponent<RectTransform>();
            clipRt.anchorMin = Vector2.zero; clipRt.anchorMax = Vector2.one;
            clipRt.offsetMin = new Vector2(2f, 2f); clipRt.offsetMax = new Vector2(-2f, -2f);
            clip.AddComponent<RectMask2D>();

            // Subtle radar crosshairs
            MakeBox(clipRt, "CrossH", new Vector2(0f, MapSize * 0.5f - 1f), new Vector2(MapSize, 1f), new Color(1f, 0.85f, 0.55f, 0.12f));
            MakeBox(clipRt, "CrossV", new Vector2(MapSize * 0.5f - 1f, 0f), new Vector2(1f, MapSize), new Color(1f, 0.85f, 0.55f, 0.12f));

            // Dynamic world layer (centered at map center)
            var wl = new GameObject("WorldLayer", typeof(RectTransform));
            wl.transform.SetParent(clipRt, false);
            worldLayer = wl.GetComponent<RectTransform>();
            worldLayer.anchorMin = new Vector2(0.5f, 0.5f);
            worldLayer.anchorMax = new Vector2(0.5f, 0.5f);
            worldLayer.pivot = new Vector2(0.5f, 0.5f);
            worldLayer.anchoredPosition = Vector2.zero;
            worldLayer.sizeDelta = Vector2.zero;

            // North compass pill at top-center inside radar
            var nPill = MakeBox(mapPanel, "NorthBadge", new Vector2(MapSize * 0.5f - 10f, MapSize - 19f), new Vector2(20f, 16f), new Color(0.10f, 0.08f, 0.05f, 0.92f));
            var nTxt = MakeLabel(nPill, "N", 12f, TextAlignmentOptions.Center, new Color(1f, 0.784f, 0.341f, 1f));
            nTxt.text = "N";

            // Jeep blip
            jeepBlip = MakeIcon(mapPanel, "JeepBlip", 11f, new Color(0.32f, 0.90f, 0.52f, 1f), diamondTex);
            jeepBlip.anchorMin = jeepBlip.anchorMax = new Vector2(0.5f, 0.5f);

            // Clamped objective marker/arrow
            objectiveBlip = MakeIcon(mapPanel, "ObjectiveBlip", 15f, new Color(0.36f, 0.88f, 0.90f, 1f), diamondTex);
            objectiveBlip.anchorMin = objectiveBlip.anchorMax = new Vector2(0.5f, 0.5f);
            objectiveImg = objectiveBlip.GetComponent<Image>();

            // Player heading arrow centered
            playerArrow = MakeIcon(mapPanel, "PlayerArrow", 17f, new Color(1f, 0.82f, 0.36f, 1f), arrowTex);
            playerArrow.anchorMin = playerArrow.anchorMax = new Vector2(0.5f, 0.5f);
            playerArrow.anchoredPosition = Vector2.zero;

            // Footer bar below radar showing destination + distance
            var ftrBg = MakeBox(rootRect, "Footer", Vector2.zero, new Vector2(MapSize, 24f), new Color(0.05f, 0.06f, 0.08f, 0.90f));
            AddOutline(ftrBg.gameObject, new Color(1f, 0.784f, 0.341f, 0.45f), 1.5f);
            footerText = MakeLabel(ftrBg, "FooterLabel", 14f, TextAlignmentOptions.MidlineLeft, new Color(0.36f, 0.88f, 0.90f, 1f));
            footerText.rectTransform.offsetMin = new Vector2(8f, 0f);
            footerText.rectTransform.offsetMax = new Vector2(-8f, 0f);

            rootRect.gameObject.SetActive(false);
        }

        private void EnsureMapLayout(bool isTempleRing)
        {
            if (builtTempleRing == isTempleRing && roads.Count > 0) return;
            for (int i = worldLayer.childCount - 1; i >= 0; i--) Destroy(worldLayer.GetChild(i).gameObject);
            roads.Clear(); zoneBlips.Clear();
            builtTempleRing = isTempleRing;

            if (isTempleRing)
            {
                // Landmark pads (matching TempleRing layout)
                AddWorldRect("Pad_Bazaar", new Vector2(0f, 4f), new Vector2(46f, 92f), new Color(0.22f, 0.17f, 0.12f, 0.48f), 0f);
                AddWorldRect("Pad_Courtyard", new Vector2(0f, 108f), new Vector2(56f, 46f), new Color(0.36f, 0.28f, 0.19f, 0.58f), 0f);
                AddWorldRect("Pad_Temple", new Vector2(0f, 156f), new Vector2(28f, 28f), new Color(0.75f, 0.56f, 0.25f, 0.65f), 0f);
                AddWorldRect("Pad_Stepwell", new Vector2(-55f, 130f), new Vector2(36f, 42f), new Color(0.14f, 0.38f, 0.52f, 0.62f), 0f);
                AddWorldRect("Pad_SunWall", new Vector2(60f, 130f), new Vector2(40f, 42f), new Color(0.56f, 0.44f, 0.22f, 0.58f), 0f);
                AddWorldRect("Pad_Jungle", new Vector2(0f, 440f), new Vector2(46f, 46f), new Color(0.16f, 0.34f, 0.18f, 0.58f), 0f);

                // Paved road network strips
                AddRoadSeg(new Vector2(0f, -85f), new Vector2(0f, 84f), 9f, new Color(0.58f, 0.49f, 0.36f, 0.78f));
                AddRoadSeg(new Vector2(0f, -8f), new Vector2(-52f, -8f), 5f, new Color(0.55f, 0.46f, 0.34f, 0.72f));
                AddRoadSeg(new Vector2(-52f, -8f), new Vector2(-52f, -90f), 5f, new Color(0.55f, 0.46f, 0.34f, 0.72f));
                AddRoadSeg(new Vector2(0f, 22f), new Vector2(30f, 36f), 5f, new Color(0.55f, 0.46f, 0.34f, 0.72f));
                AddRoadSeg(new Vector2(0f, 82f), new Vector2(-43f, 110f), 5f, new Color(0.55f, 0.46f, 0.34f, 0.72f));
                AddRoadSeg(new Vector2(0f, 82f), new Vector2(45f, 125f), 5f, new Color(0.55f, 0.46f, 0.34f, 0.72f));
                AddRoadSeg(new Vector2(26f, 140f), new Vector2(26f, 178f), 6f, new Color(0.55f, 0.46f, 0.34f, 0.72f));
                AddRoadSeg(new Vector2(26f, 178f), new Vector2(0f, 180f), 6f, new Color(0.55f, 0.46f, 0.34f, 0.72f));
                AddRoadSeg(new Vector2(0f, 180f), new Vector2(0f, 440f), 7f, new Color(0.52f, 0.44f, 0.32f, 0.74f));

                for (int i = 0; i < V3Places.Count; i++)
                {
                    var rt = MakeIcon(worldLayer, "Zone_" + i, 9f, new Color(1f, 0.85f, 0.42f, 0.92f), diamondTex);
                    zoneBlips.Add((rt, i));
                }
            }
            else
            {
                // Classic scenes: subtle central shrine ring
                AddWorldRect("ArenaCenter", new Vector2(0f, 0f), new Vector2(24f, 24f), new Color(0.55f, 0.45f, 0.28f, 0.25f), 0f);
            }
        }

        private void AddWorldRect(string name, Vector2 center, Vector2 sizeMetres, Color color, float angleDeg)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(worldLayer, false);
            var rt = g.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeMetres * Scale;
            g.AddComponent<Image>().color = color;
            roads.Add((rt, center, angleDeg));
        }

        private void AddRoadSeg(Vector2 a, Vector2 b, float widthMetres, Color color)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.1f) return;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            AddWorldRect("Road", (a + b) * 0.5f, new Vector2(len + widthMetres * 0.4f, widthMetres), color, angle);
        }

        private void LateUpdate()
        {
            string scene = SceneManager.GetActiveScene().name;
            bool menuOrIntro = scene == "MainMenu" || scene == "Boot" || string.IsNullOrEmpty(scene)
                || IntroCinematic.Playing
                || (TempleMapOverlay.Instance != null && TempleMapOverlay.Instance.IsOpen)
                || InputGate.GameplayBlocked
                || Time.timeScale <= 0f;

            var pc = FindAnyObjectByType<PlayerController>();
            var jeep = Jeep.Instance;
            Transform focusTf = (jeep != null && jeep.Occupied) ? jeep.transform : (pc != null ? pc.transform : null);

            Visible = !menuOrIntro && focusTf != null;
            if (rootRect != null && rootRect.gameObject.activeSelf != Visible) rootRect.gameObject.SetActive(Visible);
            if (!Visible) return;

            bool isTempleRing = Constants.IsNewWorld(scene);
            EnsureMapLayout(isTempleRing);

            Vector3 p = focusTf.position;
            Vector2 p2 = new Vector2(p.x, p.z);

            // Rotate player arrow by world yaw (-yaw in UI Z-rotation for North-Up minimap)
            float yaw = focusTf.eulerAngles.y;
            playerArrow.localRotation = Quaternion.Euler(0f, 0f, -yaw);

            // Update header: current area + compass heading
            string compass = CompassLabel(yaw);
            string area = ResolveAreaLabel(scene, p, isTempleRing);
            headerText.text = $"{area}   <color=#FFF4D6>{compass}</color>";

            // Position world road/landmark shapes
            foreach (var r in roads)
            {
                Vector2 delta = (r.center - p2) * Scale;
                r.rt.anchoredPosition = delta;
                r.rt.localRotation = Quaternion.Euler(0f, 0f, r.yaw);
            }

            // Position zone blips in TempleRing
            for (int i = 0; i < zoneBlips.Count; i++)
            {
                var (rt, idx) = zoneBlips[i];
                var pl = V3Places.Get(idx);
                rt.gameObject.SetActive(pl.ok);
                if (pl.ok)
                {
                    Vector2 d = (new Vector2(pl.at.x, pl.at.z) - p2) * Scale;
                    rt.anchoredPosition = d;
                }
            }

            // Position Jeep blip when outside
            if (jeep != null && !jeep.Occupied)
            {
                Vector2 jd = (new Vector2(jeep.transform.position.x, jeep.transform.position.z) - p2) * Scale;
                bool inRange = Mathf.Abs(jd.x) <= MapSize * 0.46f && Mathf.Abs(jd.y) <= MapSize * 0.46f;
                jeepBlip.gameObject.SetActive(inRange);
                if (inRange) jeepBlip.anchoredPosition = jd;
            }
            else if (jeepBlip != null)
            {
                jeepBlip.gameObject.SetActive(false);
            }

            // Objective beacon / edge arrow
            if (TryResolveObjective(scene, p, out Vector3 targetPos, out string targetLabel))
            {
                objectiveBlip.gameObject.SetActive(true);
                Vector2 flatDelta = new Vector2(targetPos.x - p.x, targetPos.z - p.z);
                float dist = flatDelta.magnitude;
                Vector2 px = flatDelta * Scale;
                float clampHalf = MapSize * 0.44f;
                bool clamped = Mathf.Abs(px.x) > clampHalf || Mathf.Abs(px.y) > clampHalf;
                if (clamped)
                {
                    float m = Mathf.Max(Mathf.Abs(px.x), Mathf.Abs(px.y));
                    px = px * (clampHalf / m);
                    objectiveImg.sprite = arrowSprite;
                    float ang = Mathf.Atan2(flatDelta.x, flatDelta.y) * Mathf.Rad2Deg;
                    objectiveBlip.localRotation = Quaternion.Euler(0f, 0f, -ang);
                    objectiveBlip.sizeDelta = new Vector2(15f, 15f);
                }
                else
                {
                    float pulse = 13f + Mathf.Sin(Time.unscaledTime * 5f) * 2.5f;
                    objectiveImg.sprite = diamondSprite;
                    objectiveBlip.localRotation = Quaternion.identity;
                    objectiveBlip.sizeDelta = new Vector2(pulse, pulse);
                }
                objectiveBlip.anchoredPosition = px;
                footerText.text = dist < 4f
                    ? $"● {targetLabel}"
                    : $"▲ {targetLabel} · {dist:0}m";
            }
            else
            {
                objectiveBlip.gameObject.SetActive(false);
                footerText.text = isTempleRing ? "[M] Map   [R] Jeep Reset" : "[M] Map";
            }
        }

        private static string CompassLabel(float yawDeg)
        {
            float y = Mathf.Repeat(yawDeg, 360f);
            string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            return dirs[Mathf.RoundToInt(y / 45f) % 8];
        }

        private static string ResolveAreaLabel(string scene, Vector3 p, bool isTempleRing)
        {
            if (!isTempleRing) return scene.ToUpperInvariant();
            if (p.z > 220f) return "JUNGLE TRAIL";
            if (p.x < -30f && p.z > 70f) return "STEPWELL";
            if (p.x > 30f && p.z > 80f) return "SUN WALL";
            if (p.y < -2.2f && p.z > 75f && p.z < 155f) return "VAULTS";
            if (p.z > 65f && p.z <= 185f) return "COURTYARD";
            return "BAZAAR";
        }

        private static bool TryResolveObjective(string scene, Vector3 playerPos, out Vector3 targetPos, out string label)
        {
            if (PlayerController.ObjectiveFocus != null)
            {
                targetPos = PlayerController.ObjectiveFocus.position;
                label = "Goal";
                return true;
            }
            if (Constants.IsNewWorld(scene))
            {
                var nwd = NewWorldDirector.Instance;
                if (nwd != null)
                {
                    if (nwd.Bazaar != null && nwd.Bazaar.Now != NewBazaar.Stage.Complete)
                    {
                        targetPos = V3Places.Get(0).ok ? V3Places.Get(0).at : new Vector3(0f, 0f, -10f);
                        label = "Zone 1: Bazaar";
                        return true;
                    }
                    if (nwd.Courtyard != null && nwd.Courtyard.Now != NewCourtyard.Stage.Complete)
                    {
                        targetPos = V3Places.Get(1).ok ? V3Places.Get(1).at : nwd.Courtyard.ArenaCenter;
                        label = "Zone 2: Courtyard";
                        return true;
                    }
                    if (nwd.Vaults != null && nwd.Vaults.Now != NewVaults.Stage.Complete)
                    {
                        targetPos = V3Places.Get(2).ok ? V3Places.Get(2).at : nwd.Vaults.ArenaCenter;
                        label = "Zone 3: Vaults";
                        return true;
                    }
                    if (nwd.Stepwell != null && nwd.Stepwell.Now != NewStepwell.Stage.Complete)
                    {
                        targetPos = V3Places.Get(3).ok ? V3Places.Get(3).at : nwd.Stepwell.ArenaCenter;
                        label = "Zone 4: Stepwell";
                        return true;
                    }
                    if (nwd.SunWall != null && nwd.SunWall.Now != NewSunWall.Stage.Complete)
                    {
                        targetPos = V3Places.Get(4).ok ? V3Places.Get(4).at : nwd.SunWall.ArenaCenter;
                        label = "Zone 5: Sun Wall";
                        return true;
                    }
                    if (nwd.Jungle != null && nwd.Jungle.Now != NewJungleChase.Stage.Complete)
                    {
                        targetPos = nwd.Jungle.ArenaCenter;
                        label = "Zone 6: Jungle";
                        return true;
                    }
                }
            }
            targetPos = Vector3.zero;
            label = "";
            return false;
        }

        private static RectTransform MakeBox(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var rt = g.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = g.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        private static RectTransform MakeIcon(Transform parent, string name, float size, Color color, Texture2D tex)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var rt = g.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            var img = g.AddComponent<Image>();
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        private static TextMeshProUGUI MakeLabel(Transform parent, string name, float size, TextAlignmentOptions align, Color color)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var rt = g.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = g.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private static void AddOutline(GameObject go, Color c, float thickness)
        {
            var ol = go.AddComponent<Outline>();
            ol.effectColor = c;
            ol.effectDistance = new Vector2(thickness, thickness);
        }
    }
}
