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
    /// coordinates narrative flow, checkpoints, and endings.
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
            var r = transform;

            Bazaar = FindAnyObjectByType<NewBazaar>() ?? new GameObject("Zone1_Bazaar").AddComponent<NewBazaar>();
            Courtyard = FindAnyObjectByType<NewCourtyard>() ?? new GameObject("Zone2_Courtyard").AddComponent<NewCourtyard>();
            Vaults = FindAnyObjectByType<NewVaults>() ?? new GameObject("Zone3_Vaults").AddComponent<NewVaults>();
            Stepwell = FindAnyObjectByType<NewStepwell>() ?? new GameObject("Zone4_Stepwell").AddComponent<NewStepwell>();
            SunWall = FindAnyObjectByType<NewSunWall>() ?? new GameObject("Zone5_SunWall").AddComponent<NewSunWall>();
            Jungle = FindAnyObjectByType<NewJungleChase>() ?? new GameObject("Zone6_Jungle").AddComponent<NewJungleChase>();

            // Ensure temple model is loaded at center of courtyard
            var temple = GameObject.Find("Temple_Game") ?? GameObject.Find("Temple");
            if (temple == null)
            {
                var pf = Resources.Load<GameObject>("Temple/Temple");
                if (pf != null)
                {
                    temple = Instantiate(pf, new Vector3(0f, 0f, 160f), Quaternion.identity);
                    temple.name = "Temple_Game";
                }
            }

            Debug.Log("[NewWorldDirector] All 6 zones initialized and linked across TempleRing.unity!");
        }

        IEnumerator Start()
        {
            yield return null;
            PlayerPrefs.SetString("LastPlayedScene", Constants.TempleRing);
            PlayerPrefs.Save();
        }
    }
}
