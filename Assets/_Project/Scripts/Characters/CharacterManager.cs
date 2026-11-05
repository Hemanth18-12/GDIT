using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Characters
{
    [DefaultExecutionOrder(-80)]
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private List<CharacterConfig> characterConfigs = new List<CharacterConfig>();
        [SerializeField] private bool autoSpawnOnStart = true;
        [SerializeField] private bool autoSpawnOnSceneChanged = true;

        [Header("Runtime References")]
        [SerializeField] private GameObject playerInstance;
        public GameObject Player => playerInstance;

        private readonly Dictionary<string, GameObject> activeNpcs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, GameObject> Npcs => activeNpcs;

        private readonly Dictionary<string, CharacterConfig> configMap = new Dictionary<string, CharacterConfig>(StringComparer.OrdinalIgnoreCase);

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

            RebuildConfigMap();
        }

        private void OnEnable()
        {
            EventBus.OnSceneChanged += HandleSceneChanged;
        }

        private void OnDisable()
        {
            EventBus.OnSceneChanged -= HandleSceneChanged;
        }

        private void Start()
        {
            if (autoSpawnOnStart)
            {
                string curScene = WorldManager.Instance != null && !string.IsNullOrEmpty(WorldManager.Instance.CurrentSceneName)
                    ? WorldManager.Instance.CurrentSceneName
                    : SceneManager.GetActiveScene().name;

                if (!string.IsNullOrEmpty(curScene))
                {
                    SpawnCharactersForScene(curScene);
                }
            }
        }

        public void RegisterConfig(CharacterConfig config)
        {
            if (config != null && !string.IsNullOrEmpty(config.CharacterId))
            {
                configMap[config.CharacterId] = config;
                if (!characterConfigs.Contains(config))
                {
                    characterConfigs.Add(config);
                }
            }
        }

        private void RebuildConfigMap()
        {
            configMap.Clear();
            foreach (var cfg in characterConfigs)
            {
                if (cfg != null && !string.IsNullOrEmpty(cfg.CharacterId))
                {
                    configMap[cfg.CharacterId] = cfg;
                }
            }
        }

        private void HandleSceneChanged(string sceneName)
        {
            if (autoSpawnOnSceneChanged)
            {
                SpawnCharactersForScene(sceneName);
            }
        }

        /// <summary>
        /// Spawns the player and relevant NPCs at the scene's designated SpawnPoints.
        /// </summary>
        public void SpawnCharactersForScene(string sceneName)
        {
            // 1. Position or Spawn Player at "player_start"
            Vector3 targetPos = Vector3.zero;
            Quaternion targetRot = Quaternion.identity;

            if (WorldManager.Instance != null)
            {
                Transform playerSpawn = WorldManager.Instance.GetSpawn("player_start");
                if (playerSpawn != null)
                {
                    targetPos = playerSpawn.position;
                    targetRot = playerSpawn.rotation;
                }
            }

            if (playerInstance == null)
            {
                SpawnPlayer(targetPos, targetRot);
            }
            else
            {
                CharacterController cc = playerInstance.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                playerInstance.transform.position = targetPos;
                playerInstance.transform.rotation = targetRot;
                if (cc != null) cc.enabled = true;
            }

            // 2. Clear previous NPCs
            ClearNpcs();

            // 3. Spawn scene-specific NPCs
            SpawnNpcsForScene(sceneName);
        }

        public GameObject SpawnPlayer(Vector3 position, Quaternion rotation)
        {
            GameObject prefab = GetPrefab(Constants.Investigator);
            if (prefab != null)
            {
                playerInstance = Instantiate(prefab, position, rotation);
                playerInstance.name = "Player_Investigator";
            }
            else
            {
                Debug.LogWarning("[CharacterManager] No Investigator prefab configured. Creating procedural player.");
                playerInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerInstance.name = "Player_Investigator";
                playerInstance.transform.position = position;
                playerInstance.transform.rotation = rotation;
            }

            if (transform.parent == null)
            {
                DontDestroyOnLoad(playerInstance);
            }

            return playerInstance;
        }

        public GameObject SpawnNpc(string characterId, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = GetPrefab(characterId);
            GameObject npcObj;

            if (prefab != null)
            {
                npcObj = Instantiate(prefab, position, rotation);
            }
            else
            {
                npcObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                npcObj.transform.position = position;
                npcObj.transform.rotation = rotation;
            }

            npcObj.name = $"NPC_{characterId}";
            activeNpcs[characterId] = npcObj;
            return npcObj;
        }

        public void SpawnNpcsForScene(string sceneName)
        {
            if (WorldManager.Instance == null) return;

            if (sceneName.Equals(Constants.Courtyard, StringComparison.OrdinalIgnoreCase))
            {
                Transform priestSp = WorldManager.Instance.GetSpawn("priest_spawn");
                if (priestSp != null) SpawnNpc(Constants.Priest, priestSp.position, priestSp.rotation);

                Transform villagerSp = WorldManager.Instance.GetSpawn("villager_spawn");
                if (villagerSp != null) SpawnNpc(Constants.Villager, villagerSp.position, villagerSp.rotation);
            }
            else if (sceneName.Equals(Constants.Sanctum, StringComparison.OrdinalIgnoreCase))
            {
                Transform priestSp = WorldManager.Instance.GetSpawn("priest_spawn");
                if (priestSp != null) SpawnNpc(Constants.Priest, priestSp.position, priestSp.rotation);
            }
            else if (sceneName.Equals(Constants.ShrineCrime, StringComparison.OrdinalIgnoreCase))
            {
                Transform villagerSp = WorldManager.Instance.GetSpawn("villager_spawn");
                if (villagerSp != null) SpawnNpc(Constants.Villager, villagerSp.position, villagerSp.rotation);
            }
            else if (sceneName.Equals(Constants.JunglePath, StringComparison.OrdinalIgnoreCase))
            {
                Transform robberSp = WorldManager.Instance.GetSpawn("robber_spawn");
                if (robberSp != null)
                {
                    SpawnNpc(Constants.Robber1, robberSp.position, robberSp.rotation);
                    SpawnNpc(Constants.Robber2, robberSp.position + robberSp.right * 1.5f, robberSp.rotation);
                }
            }
        }

        public void ClearNpcs()
        {
            foreach (var kvp in activeNpcs)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            activeNpcs.Clear();
        }

        public GameObject GetNpc(string characterId)
        {
            if (activeNpcs.TryGetValue(characterId, out GameObject npc))
            {
                return npc;
            }
            return null;
        }

        private GameObject GetPrefab(string characterId)
        {
            if (configMap.TryGetValue(characterId, out CharacterConfig cfg))
            {
                if (cfg != null && cfg.Prefab != null)
                {
                    return cfg.Prefab;
                }
            }
            return null;
        }
    }
}
