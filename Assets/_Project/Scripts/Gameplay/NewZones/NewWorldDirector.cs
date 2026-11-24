using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.World;
using LegacyOfTemples.Gameplay.Puzzles;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Master coordinator for the New World in TempleRing.unity.
    /// Ensures all 6 zones (Bazaar, Courtyard, Vaults, Stepwell, Sun Wall, Jungle) are present,
    /// anchors a clean grounded Kalinga sandstone temple at the Courtyard center, and coordinates checkpoints.
    /// </summary>
    public sealed class NewWorldDirector : MonoBehaviour
    {
        public static NewWorldDirector Instance { get; private set; }

        public NewBazaar Bazaar { get; private set; }
        public NewCourtyard Courtyard { get; private set; }
        public NewVaults Vaults { get; private set; }
        public NewStepwell Stepwell { get; private set; }
        public NewSunWall SunWall { get; private set; }
        public NewJungleChase Jungle { get; private set; }

        public bool AllZonesReady => Bazaar != null && Courtyard != null && Vaults != null && Stepwell != null && SunWall != null && Jungle != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoBoot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == Constants.TempleRing)
            {
                if (Instance == null)
                {
                    var g = new GameObject("NewWorld_Director");
                    g.AddComponent<NewWorldDirector>();
                }
                else
                {
                    Instance.EnsureZones();
                }
            }
        }

        void Awake()
        {
            Instance = this;
            EnsureZones();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnsureZones()
        {
            // Destroy any lingering unoptimized Temple_Game mesh from earlier scene saves
            var legacyRaw = GameObject.Find("Temple_Game");
            if (legacyRaw != null) Destroy(legacyRaw);

            Bazaar = FindAnyObjectByType<NewBazaar>() ?? new GameObject("Zone1_Bazaar").AddComponent<NewBazaar>();
            Courtyard = FindAnyObjectByType<NewCourtyard>() ?? new GameObject("Zone2_Courtyard").AddComponent<NewCourtyard>();
            Vaults = FindAnyObjectByType<NewVaults>() ?? new GameObject("Zone3_Vaults").AddComponent<NewVaults>();
            Stepwell = FindAnyObjectByType<NewStepwell>() ?? new GameObject("Zone4_Stepwell").AddComponent<NewStepwell>();
            SunWall = FindAnyObjectByType<NewSunWall>() ?? new GameObject("Zone5_SunWall").AddComponent<NewSunWall>();
            Jungle = FindAnyObjectByType<NewJungleChase>() ?? new GameObject("Zone6_Jungle").AddComponent<NewJungleChase>();

            // Build clean grounded modular Kalinga temple on the Courtyard's north raised platform (z = 156, y = 0.7f)
            if (GameObject.Find("Temple_KalingaCentral") == null)
            {
                var res = KalingaTempleBuilder.Build(transform, "Temple_KalingaCentral", new Vector3(0f, 0.7f, 156f), 0.92f);
                var flag = new GameObject("TempleCentralFlag").AddComponent<WavingFlag>();
                flag.transform.SetParent(res.root, false);
                flag.transform.position = res.towerTop;
                // Connecting pathways linking Courtyard east/west flanks tightly to Stepwell, Sun Wall, and North Jungle Road
                MarketArt.Road(transform, new[] { new Vector3(0f, 0.02f, 82f), new Vector3(-43f, 0.02f, 110f) }, 5f);
                MarketArt.Road(transform, new[] { new Vector3(0f, 0.02f, 82f), new Vector3(45f, 0.02f, 125f) }, 5f);
                MarketArt.Road(transform, new[] { new Vector3(26f, 0.02f, 140f), new Vector3(26f, 0.02f, 178f), new Vector3(0f, 0.02f, 180f) }, 6f);
            }

            Debug.Log("[NewWorldDirector] All 6 zones and grounded Kalinga central temple linked cleanly across TempleRing.unity!");
        }

        private int lastSavedZoneIdx = -1;

        IEnumerator Start()
        {
            yield return null;
            PlayerPrefs.SetString("LastPlayedScene", Constants.TempleRing);
            PlayerPrefs.Save();
            int startZone = PlayerPrefs.GetInt("SelectedStartZone", -1);
            if (startZone >= 0)
            {
                PlayerPrefs.DeleteKey("SelectedStartZone");
                PlayerPrefs.Save();
                var place = V3Places.Get(startZone);
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null && place.ok) pc.Teleport(place.at, Quaternion.LookRotation(place.look - place.at));
            }
        }

        void Update()
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc == null || Time.timeScale <= 0f || InputGate.GameplayBlocked) return;
            for (int i = 0; i < V3Places.Count && i < 6; i++)
            {
                if (i == lastSavedZoneIdx) continue;
                string zoneKey = $"zone{i + 1}";
                if (Checkpoints.Has && Checkpoints.Current.zone == zoneKey) { lastSavedZoneIdx = i; continue; }
                var pl = V3Places.Get(i);
                if (!pl.ok) continue;
                Vector3 a = new Vector3(pc.transform.position.x, 0f, pc.transform.position.z);
                Vector3 b = new Vector3(pl.at.x, 0f, pl.at.z);
                if (Vector3.Distance(a, b) <= 14f)
                {
                    lastSavedZoneIdx = i;
                    float sec = DawnClock.Instance != null ? DawnClock.Instance.Minutes * 60f : 0f;
                    Checkpoints.Save(zoneKey, V3Places.Names[i], pc.transform.position, pc.transform.eulerAngles.y, sec);
                    break;
                }
            }
        }
    }
}
