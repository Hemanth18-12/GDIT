using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.World
{
    [DefaultExecutionOrder(-100)]
    public class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance { get; private set; }

        [Header("Starting Configuration")]
        [SerializeField] private string initialScene = Constants.Courtyard;
        [SerializeField] private bool loadInitialOnStart = true;

        [Header("Runtime State")]
        [SerializeField] private string currentSceneName = string.Empty;
        public string CurrentSceneName => currentSceneName;

        private readonly Dictionary<string, Transform> activeSpawns = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        private bool isTransitioning = false;
        public bool IsTransitioning => isTransitioning;

        public event Action<float> OnFadeProgress; // 0 = transparent, 1 = solid black

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            if (loadInitialOnStart && !string.IsNullOrEmpty(initialScene))
            {
                // Check if already active or needs additive load
                Scene existing = SceneManager.GetSceneByName(initialScene);
                if (existing.isLoaded)
                {
                    currentSceneName = initialScene;
                    SceneManager.SetActiveScene(existing);
                    CacheSceneSpawns(existing);
                    EventBus.RaiseSceneChanged(currentSceneName);
                }
                else
                {
                    LoadScene(initialScene);
                }
            }
        }

        /// <summary>
        /// Asynchronously loads an environment scene additively and unloads the previous one.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (isTransitioning)
            {
                Debug.LogWarning($"[WorldManager] Cannot load {sceneName} while transition is active.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isTransitioning = true;

            // Fade to black
            yield return StartCoroutine(FadeRoutine(0f, 1f, 0.35f));

            // Unload old scene if loaded
            if (!string.IsNullOrEmpty(currentSceneName) && currentSceneName != sceneName)
            {
                Scene oldScene = SceneManager.GetSceneByName(currentSceneName);
                if (oldScene.isLoaded)
                {
                    AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(oldScene);
                    while (unloadOp != null && !unloadOp.isDone)
                    {
                        yield return null;
                    }
                }
            }

            activeSpawns.Clear();

            // Load new scene additively
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOp != null)
            {
                while (!loadOp.isDone)
                {
                    yield return null;
                }
            }

            Scene newScene = SceneManager.GetSceneByName(sceneName);
            if (newScene.IsValid() && newScene.isLoaded)
            {
                SceneManager.SetActiveScene(newScene);
                currentSceneName = sceneName;
                CacheSceneSpawns(newScene);

                Debug.Log($"[WorldManager] Loaded scene '{sceneName}'. Found {activeSpawns.Count} named spawn points.");
                EventBus.RaiseSceneChanged(sceneName);
            }
            else
            {
                Debug.LogError($"[WorldManager] Failed to load scene '{sceneName}'.");
            }

            // Small settle delay for physics/camera
            yield return new WaitForSeconds(0.1f);

            // Fade back in
            yield return StartCoroutine(FadeRoutine(1f, 0f, 0.35f));

            isTransitioning = false;
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(from, to, t);
                OnFadeProgress?.Invoke(alpha);
                yield return null;
            }
            OnFadeProgress?.Invoke(to);
        }

        private void CacheSceneSpawns(Scene scene)
        {
            activeSpawns.Clear();
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                SpawnPoint[] spawns = root.GetComponentsInChildren<SpawnPoint>(true);
                foreach (SpawnPoint sp in spawns)
                {
                    if (!string.IsNullOrEmpty(sp.SpawnId))
                    {
                        activeSpawns[sp.SpawnId] = sp.transform;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a named spawn point transform in the currently loaded scene.
        /// Supported names: "player_start", "robber_spawn", "priest_spawn", "villager_spawn"
        /// </summary>
        public Transform GetSpawn(string spawnName)
        {
            if (string.IsNullOrEmpty(spawnName))
            {
                spawnName = "player_start";
            }

            if (activeSpawns.TryGetValue(spawnName, out Transform found))
            {
                return found;
            }

            // Alias fallback checks
            if (spawnName.Equals("player_start", StringComparison.OrdinalIgnoreCase) ||
                spawnName.Equals(Constants.Investigator, StringComparison.OrdinalIgnoreCase) ||
                spawnName.Equals("player", StringComparison.OrdinalIgnoreCase))
            {
                if (activeSpawns.TryGetValue("player_start", out found)) return found;
                if (activeSpawns.TryGetValue(Constants.Investigator, out found)) return found;
            }

            if (spawnName.Equals("robber_spawn", StringComparison.OrdinalIgnoreCase) ||
                spawnName.Equals(Constants.Robber1, StringComparison.OrdinalIgnoreCase) ||
                spawnName.Equals(Constants.Robber2, StringComparison.OrdinalIgnoreCase))
            {
                if (activeSpawns.TryGetValue("robber_spawn", out found)) return found;
                if (activeSpawns.TryGetValue(Constants.Robber1, out found)) return found;
            }

            if (spawnName.Equals("priest_spawn", StringComparison.OrdinalIgnoreCase) ||
                spawnName.Equals(Constants.Priest, StringComparison.OrdinalIgnoreCase))
            {
                if (activeSpawns.TryGetValue("priest_spawn", out found)) return found;
                if (activeSpawns.TryGetValue(Constants.Priest, out found)) return found;
            }

            if (spawnName.Equals("villager_spawn", StringComparison.OrdinalIgnoreCase) ||
                spawnName.Equals(Constants.Villager, StringComparison.OrdinalIgnoreCase))
            {
                if (activeSpawns.TryGetValue("villager_spawn", out found)) return found;
                if (activeSpawns.TryGetValue(Constants.Villager, out found)) return found;
            }

            // Return first available spawn point or fallback
            using (var enumerator = activeSpawns.Values.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    return enumerator.Current;
                }
            }

            Debug.LogWarning($"[WorldManager] No spawn point found matching '{spawnName}'. Using WorldManager transform.");
            return transform;
        }

        public void UnloadCurrentScene()
        {
            if (!string.IsNullOrEmpty(currentSceneName))
            {
                SceneManager.UnloadSceneAsync(currentSceneName);
                currentSceneName = string.Empty;
                activeSpawns.Clear();
            }
        }
    }
}
