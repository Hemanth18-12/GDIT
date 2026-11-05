using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Characters.Editor
{
    public static class CharacterBuilder
    {
        private const string PrefabBasePath = "Assets/_Project/Prefabs/Characters";
        private const string MaterialsBasePath = "Assets/_Project/Models/Characters/Materials";
        private const string ConfigBasePath = "Assets/_Project/Models/Characters";

        #region Material Factory

        public static Material GetCharacterMaterial(string matName)
        {
            if (!Directory.Exists(MaterialsBasePath))
            {
                Directory.CreateDirectory(MaterialsBasePath);
                AssetDatabase.Refresh();
            }

            string assetPath = $"{MaterialsBasePath}/{matName}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = matName;

            switch (matName)
            {
                case "mat_Char_TrenchCoat":
                    mat.SetColor("_BaseColor", new Color(0.72f, 0.60f, 0.44f)); // Khaki adventurer jacket
                    mat.SetFloat("_Smoothness", 0.20f);
                    break;

                case "mat_Char_DhotiKhaki":
                    mat.SetColor("_BaseColor", new Color(0.86f, 0.83f, 0.76f)); // Cream cotton dhoti
                    mat.SetFloat("_Smoothness", 0.15f);
                    break;

                case "mat_Char_LeatherBrown":
                    mat.SetColor("_BaseColor", new Color(0.30f, 0.16f, 0.08f)); // Dark leather straps/boots
                    mat.SetFloat("_Smoothness", 0.45f);
                    break;

                case "mat_Char_SkinTone":
                    mat.SetColor("_BaseColor", new Color(0.66f, 0.46f, 0.35f)); // Warm skin tone
                    mat.SetFloat("_Smoothness", 0.35f);
                    break;

                case "mat_Char_SaffronAccent":
                    mat.SetColor("_BaseColor", new Color(0.92f, 0.36f, 0.08f)); // Saffron sash/turban
                    mat.SetFloat("_Smoothness", 0.22f);
                    break;

                case "mat_Char_GoldBrass":
                    mat.SetColor("_BaseColor", new Color(0.88f, 0.72f, 0.28f)); // Brass buckles/tools
                    mat.SetFloat("_Smoothness", 0.85f);
                    mat.SetFloat("_Metallic", 0.90f);
                    break;

                case "mat_Char_DarkHair":
                    mat.SetColor("_BaseColor", new Color(0.12f, 0.10f, 0.09f)); // Dark hair
                    mat.SetFloat("_Smoothness", 0.25f);
                    break;

                case "mat_Char_PriestSilk":
                    mat.SetColor("_BaseColor", new Color(0.95f, 0.50f, 0.10f)); // Saffron silk dhoti
                    mat.SetFloat("_Smoothness", 0.25f);
                    break;

                case "mat_Char_PriestAngavastram":
                    mat.SetColor("_BaseColor", new Color(0.96f, 0.94f, 0.88f)); // White silk shawl
                    mat.SetFloat("_Smoothness", 0.20f);
                    break;

                case "mat_Char_RobberDark":
                    mat.SetColor("_BaseColor", new Color(0.18f, 0.16f, 0.20f)); // Midnight stealth tunic
                    mat.SetFloat("_Smoothness", 0.18f);
                    break;

                case "mat_Char_RobberRedMask":
                    mat.SetColor("_BaseColor", new Color(0.72f, 0.12f, 0.14f)); // Crimson robber cloth
                    mat.SetFloat("_Smoothness", 0.25f);
                    break;

                case "mat_Char_VillagerKurta":
                    mat.SetColor("_BaseColor", new Color(0.55f, 0.65f, 0.52f)); // Earthy green villager kurta
                    mat.SetFloat("_Smoothness", 0.15f);
                    break;

                default:
                    mat.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.6f));
                    break;
            }

            AssetDatabase.CreateAsset(mat, assetPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        #endregion

        #region Investigator Prefab Builder (Step 1)

        [MenuItem("Tools/Legacy/Characters/Build Investigator Prefab")]
        public static GameObject BuildInvestigatorPrefab()
        {
            EnsureDirectoryExists(PrefabBasePath);
            EnsureDirectoryExists(ConfigBasePath);

            string prefabPath = $"{PrefabBasePath}/Investigator.prefab";

            // Temporary GameObject in Scene to assemble
            GameObject root = new GameObject("Investigator");
            root.tag = "Player";

            // Character Controller
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0.001f;

            // Player Controller
            PlayerController pc = root.AddComponent<PlayerController>();

            // Visual Model Root
            GameObject model = new GameObject("Model");
            model.transform.parent = root.transform;
            model.transform.localPosition = Vector3.zero;

            Material coatMat = GetCharacterMaterial("mat_Char_TrenchCoat");
            Material dhotiMat = GetCharacterMaterial("mat_Char_DhotiKhaki");
            Material leatherMat = GetCharacterMaterial("mat_Char_LeatherBrown");
            Material skinMat = GetCharacterMaterial("mat_Char_SkinTone");
            Material saffronMat = GetCharacterMaterial("mat_Char_SaffronAccent");
            Material brassMat = GetCharacterMaterial("mat_Char_GoldBrass");

            // 1. Torso & Adventurer Coat
            CreatePartBox("Torso_Coat", model.transform, new Vector3(0, 1.25f, 0), new Vector3(0.44f, 0.45f, 0.30f), coatMat);
            CreatePartBox("CoatCollar_Left", model.transform, new Vector3(-0.14f, 1.48f, 0.08f), new Vector3(0.12f, 0.15f, 0.18f), coatMat);
            CreatePartBox("CoatCollar_Right", model.transform, new Vector3(0.14f, 1.48f, 0.08f), new Vector3(0.12f, 0.15f, 0.18f), coatMat);

            // 2. Dhoti / Pelvis
            CreatePartBox("Hips_Dhoti", model.transform, new Vector3(0, 0.92f, 0), new Vector3(0.42f, 0.28f, 0.28f), dhotiMat);

            // 3. Leather Belt & Buckle
            CreatePartBox("Belt", model.transform, new Vector3(0, 1.05f, 0), new Vector3(0.46f, 0.08f, 0.32f), leatherMat);
            CreatePartBox("Buckle", model.transform, new Vector3(0, 1.05f, 0.165f), new Vector3(0.10f, 0.09f, 0.02f), brassMat);

            // 4. Relic Satchel slung over shoulder
            CreatePartBox("Satchel_Bag", model.transform, new Vector3(0.24f, 0.95f, 0.05f), new Vector3(0.16f, 0.22f, 0.24f), leatherMat);
            GameObject strap = CreatePartBox("Satchel_Strap", model.transform, new Vector3(0.08f, 1.25f, 0.02f), new Vector3(0.04f, 0.55f, 0.32f), leatherMat);
            strap.transform.localRotation = Quaternion.Euler(0, 0, 32f);

            // 5. Head & Saffron Pagri
            CreatePartCylinder("Neck", model.transform, new Vector3(0, 1.50f, 0), new Vector3(0.16f, 0.08f, 0.16f), skinMat);
            CreatePartSphere("Head", model.transform, new Vector3(0, 1.68f, 0), new Vector3(0.30f, 0.34f, 0.30f), skinMat);
            CreatePartCylinder("Turban_Pagri", model.transform, new Vector3(0, 1.80f, 0), new Vector3(0.36f, 0.10f, 0.36f), saffronMat);
            CreatePartSphere("Pagri_Finial", model.transform, new Vector3(0, 1.87f, 0), new Vector3(0.18f, 0.12f, 0.18f), saffronMat);

            // 6. Arms & Hands
            CreatePartCylinder("Arm_Left_Upper", model.transform, new Vector3(-0.30f, 1.30f, 0), new Vector3(0.12f, 0.28f, 0.12f), coatMat);
            CreatePartCylinder("Arm_Left_Lower", model.transform, new Vector3(-0.30f, 1.05f, 0), new Vector3(0.11f, 0.24f, 0.11f), skinMat);
            CreatePartSphere("Hand_Left", model.transform, new Vector3(-0.30f, 0.88f, 0), new Vector3(0.12f, 0.12f, 0.12f), skinMat);

            CreatePartCylinder("Arm_Right_Upper", model.transform, new Vector3(0.30f, 1.30f, 0), new Vector3(0.12f, 0.28f, 0.12f), coatMat);
            CreatePartCylinder("Arm_Right_Lower", model.transform, new Vector3(0.30f, 1.05f, 0), new Vector3(0.11f, 0.24f, 0.11f), skinMat);
            CreatePartSphere("Hand_Right", model.transform, new Vector3(0.30f, 0.88f, 0), new Vector3(0.12f, 0.12f, 0.12f), skinMat);

            // Magnifying Glass / Loupe in right hand
            CreatePartCylinder("Loupe_Rim", model.transform, new Vector3(0.30f, 0.82f, 0.15f), new Vector3(0.16f, 0.02f, 0.16f), brassMat);
            CreatePartCylinder("Loupe_Handle", model.transform, new Vector3(0.30f, 0.85f, 0.08f), new Vector3(0.03f, 0.10f, 0.03f), leatherMat);

            // 7. Legs & Boots
            CreatePartCylinder("Leg_Left_Thigh", model.transform, new Vector3(-0.13f, 0.65f, 0), new Vector3(0.14f, 0.32f, 0.14f), dhotiMat);
            CreatePartBox("Boot_Left", model.transform, new Vector3(-0.13f, 0.24f, 0.03f), new Vector3(0.14f, 0.44f, 0.24f), leatherMat);

            CreatePartCylinder("Leg_Right_Thigh", model.transform, new Vector3(0.13f, 0.65f, 0), new Vector3(0.14f, 0.32f, 0.14f), dhotiMat);
            CreatePartBox("Boot_Right", model.transform, new Vector3(0.13f, 0.24f, 0.03f), new Vector3(0.14f, 0.44f, 0.24f), leatherMat);

            // Save as Prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            // Create or update CharacterConfig
            string configPath = $"{ConfigBasePath}/InvestigatorConfig.asset";
            CharacterConfig config = AssetDatabase.LoadAssetAtPath<CharacterConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CharacterConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }

            config.SetConfig(Constants.Investigator, "Investigator Aryan", true, prefab, 4.8f);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CharacterBuilder] Successfully created Investigator prefab at '{prefabPath}' and config at '{configPath}'.");
            return prefab;
        }

        [MenuItem("Tools/Legacy/Characters/Build CharacterTest Scene")]
        public static void BuildCharacterTestScene()
        {
            // First ensure Investigator prefab exists
            GameObject investigatorPrefab = BuildInvestigatorPrefab();

            string scenesDevPath = "Assets/_Project/Scenes/Dev";
            EnsureDirectoryExists(scenesDevPath);
            string scenePath = $"{scenesDevPath}/CharacterTest.unity";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Atmosphere & Lighting
            GameObject lightRoot = new GameObject("--- LIGHTING & ATMOSPHERE ---");

            GameObject sunObj = new GameObject("Directional_TestSun");
            sunObj.transform.parent = lightRoot.transform;
            sunObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.88f, 0.68f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.40f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.85f, 0.75f, 0.65f);
            RenderSettings.fogDensity = 0.01f;

            // 2. Arena Geometry
            GameObject arenaRoot = new GameObject("--- TEST ARENA ---");
            Material floorMat = GetCharacterMaterial("mat_Char_DhotiKhaki");
            Material leatherMat = GetCharacterMaterial("mat_Char_LeatherBrown");
            Material brassMat = GetCharacterMaterial("mat_Char_GoldBrass");

            // Main Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "TestArena_Floor";
            floor.transform.parent = arenaRoot.transform;
            floor.transform.localPosition = new Vector3(0, -0.25f, 0);
            floor.transform.localScale = new Vector3(32f, 0.5f, 32f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // Boundary Walls
            CreateArenaWall("Wall_North", arenaRoot.transform, new Vector3(0, 1.5f, -16f), new Vector3(32f, 3.0f, 1.0f), leatherMat);
            CreateArenaWall("Wall_South", arenaRoot.transform, new Vector3(0, 1.5f, 16f), new Vector3(32f, 3.0f, 1.0f), leatherMat);
            CreateArenaWall("Wall_East", arenaRoot.transform, new Vector3(16f, 1.5f, 0), new Vector3(1.0f, 3.0f, 32f), leatherMat);
            CreateArenaWall("Wall_West", arenaRoot.transform, new Vector3(-16f, 1.5f, 0), new Vector3(1.0f, 3.0f, 32f), leatherMat);

            // Stepped Platform to test step-up and jumping physics
            for (int s = 0; s < 4; s++)
            {
                float y = (s + 1) * 0.3f;
                float z = -4f - s * 1.0f;
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"TestStep_{s + 1}";
                step.transform.parent = arenaRoot.transform;
                step.transform.localPosition = new Vector3(0, y * 0.5f, z);
                step.transform.localScale = new Vector3(6.0f, y, 1.0f);
                step.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
            }

            // Test Interactables
            CreateTestInteractable(arenaRoot.transform, "interactable_test_altar", "Inspect ancient relic dais", new Vector3(-4f, 0.5f, 3f), 2.5f);
            CreateTestInteractable(arenaRoot.transform, "interactable_test_lamp", "Light sacred brass lamp", new Vector3(4f, 0.5f, 3f), 2.2f);

            // 3. System & Player
            GameObject sysRoot = new GameObject("--- SYSTEM ---");

            // CharacterManager & WorldDevTools
            GameObject cmObj = new GameObject("CharacterManager");
            cmObj.transform.parent = sysRoot.transform;
            CharacterManager cm = cmObj.AddComponent<CharacterManager>();
            cmObj.AddComponent<WorldDevTools>();

            string configPath = $"{ConfigBasePath}/InvestigatorConfig.asset";
            CharacterConfig invConfig = AssetDatabase.LoadAssetAtPath<CharacterConfig>(configPath);
            if (invConfig != null) cm.RegisterConfig(invConfig);

            // Spawn Player Instance directly in test scene
            GameObject playerObj = (GameObject)PrefabUtility.InstantiatePrefab(investigatorPrefab);
            playerObj.name = "Player_Investigator";
            playerObj.transform.position = new Vector3(0, 0, 0);
            playerObj.transform.rotation = Quaternion.identity;

            // Main Camera with OrbitCamera
            GameObject camObj = new GameObject("Main Camera");
            camObj.transform.parent = sysRoot.transform;
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0, 2.5f, -4.5f);
            camObj.transform.rotation = Quaternion.Euler(15f, 0, 0);

            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            camObj.AddComponent<AudioListener>();

            OrbitCamera orbit = camObj.AddComponent<OrbitCamera>();
            orbit.SetTarget(playerObj.transform);

            PlayerController pc = playerObj.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.SetCameraTransform(cam.transform);
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[CharacterBuilder] Successfully created CharacterTest scene at '{scenePath}'.");
        }

        private static void CreateArenaWall(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.parent = parent;
            wall.transform.localPosition = pos;
            wall.transform.localScale = size;
            if (mat != null) wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateTestInteractable(Transform parent, string id, string prompt, Vector3 pos, float radius)
        {
            GameObject intObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            intObj.name = $"Interactable_{id}";
            intObj.transform.parent = parent;
            intObj.transform.localPosition = pos;
            intObj.transform.localScale = new Vector3(1.0f, 0.5f, 1.0f);

            InteractableObject io = intObj.AddComponent<InteractableObject>();
            io.Setup(id, prompt, radius);
        }

        [MenuItem("Tools/Legacy/Characters/Place Player In Courtyard")]
        public static void PlacePlayerInCourtyard()
        {
            string scenePath = "Assets/_Project/Scenes/Environments/Courtyard.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning("[CharacterBuilder] Courtyard scene not found.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Ensure Investigator Prefab
            GameObject investigatorPrefab = BuildInvestigatorPrefab();

            // Find player_start spawn point
            Transform spawnTrans = null;
            SpawnPoint[] spawns = UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude);
            foreach (var sp in spawns)
            {
                if (sp.SpawnId == "player_start")
                {
                    spawnTrans = sp.transform;
                    break;
                }
            }

            Vector3 spawnPos = spawnTrans != null ? spawnTrans.position : new Vector3(0, 0, 16f);
            Quaternion spawnRot = spawnTrans != null ? spawnTrans.rotation : Quaternion.Euler(0, 180f, 0);

            // Remove existing player if any
            GameObject existingPlayer = GameObject.FindWithTag("Player");
            if (existingPlayer != null)
            {
                UnityEngine.Object.DestroyImmediate(existingPlayer);
            }

            // Instantiate Player
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(investigatorPrefab);
            player.name = "Player_Investigator";
            player.transform.position = spawnPos;
            player.transform.rotation = spawnRot;

            // Setup Camera
            GameObject camObj = GameObject.FindWithTag("MainCamera");
            if (camObj != null)
            {
                OrbitCamera orbit = camObj.GetComponent<OrbitCamera>();
                if (orbit == null) orbit = camObj.AddComponent<OrbitCamera>();
                orbit.SetTarget(player.transform);

                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.SetCameraTransform(camObj.transform);
            }

            // Ensure CharacterManager is present in scene
            CharacterManager cm = UnityEngine.Object.FindAnyObjectByType<CharacterManager>();
            if (cm == null)
            {
                GameObject sysObj = GameObject.Find("--- SYSTEM ---");
                GameObject cmObj = new GameObject("CharacterManager");
                if (sysObj != null) cmObj.transform.parent = sysObj.transform;
                cm = cmObj.AddComponent<CharacterManager>();
            }

            // Register all character configs
            string[] configs = { "InvestigatorConfig", "PriestConfig", "VillagerConfig", "Robber1Config", "Robber2Config" };
            foreach (string cn in configs)
            {
                CharacterConfig cfg = AssetDatabase.LoadAssetAtPath<CharacterConfig>($"{ConfigBasePath}/{cn}.asset");
                if (cfg != null) cm.RegisterConfig(cfg);
            }

            // Spawn Priest NPC in Courtyard if not present
            GameObject priestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBasePath}/Priest.prefab");
            if (priestPrefab != null && GameObject.Find("NPC_Priest") == null)
            {
                Transform priestSp = FindSpawnPoint(spawns, "priest_spawn");
                Vector3 pPos = priestSp != null ? priestSp.position : new Vector3(3f, 0, 2f);
                Quaternion pRot = priestSp != null ? priestSp.rotation : Quaternion.Euler(0, -90f, 0);
                GameObject priest = (GameObject)PrefabUtility.InstantiatePrefab(priestPrefab);
                priest.name = "NPC_Priest";
                priest.transform.position = pPos;
                priest.transform.rotation = pRot;
            }

            // Spawn Villager NPC in Courtyard if not present
            GameObject villagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBasePath}/Villager.prefab");
            if (villagerPrefab != null && GameObject.Find("NPC_Villager") == null)
            {
                Transform villagerSp = FindSpawnPoint(spawns, "villager_spawn");
                Vector3 vPos = villagerSp != null ? villagerSp.position : new Vector3(-8f, 0, 10f);
                Quaternion vRot = villagerSp != null ? villagerSp.rotation : Quaternion.Euler(0, 90f, 0);
                GameObject villager = (GameObject)PrefabUtility.InstantiatePrefab(villagerPrefab);
                villager.name = "NPC_Villager";
                villager.transform.position = vPos;
                villager.transform.rotation = vRot;
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[CharacterBuilder] Successfully placed Player at player_start {spawnPos} and NPCs in Courtyard scene.");
        }

        private static Transform FindSpawnPoint(SpawnPoint[] spawns, string spawnId)
        {
            if (spawns == null) return null;
            foreach (var sp in spawns)
            {
                if (sp != null && sp.SpawnId == spawnId) return sp.transform;
            }
            return null;
        }

        public static void BuildCharacterTestSceneBatch()
        {
            BuildCharacterTestScene();
            PlacePlayerInCourtyard();
        }

        #endregion

        #region Priest Prefab Builder (Step 3)

        [MenuItem("Tools/Legacy/Characters/Build Priest Prefab")]
        public static GameObject BuildPriestPrefab()
        {
            EnsureDirectoryExists(PrefabBasePath);
            EnsureDirectoryExists(ConfigBasePath);

            string prefabPath = $"{PrefabBasePath}/Priest.prefab";

            GameObject root = new GameObject("Priest");
            root.tag = "Untagged";

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.88f, 0);

            GameObject model = new GameObject("Model");
            model.transform.parent = root.transform;

            Material silkMat = GetCharacterMaterial("mat_Char_PriestSilk");
            Material angavastramMat = GetCharacterMaterial("mat_Char_PriestAngavastram");
            Material skinMat = GetCharacterMaterial("mat_Char_SkinTone");
            Material brassMat = GetCharacterMaterial("mat_Char_GoldBrass");
            Material woodMat = GetCharacterMaterial("mat_Char_LeatherBrown");

            // Dhoti & Upper Bare Torso with Sacred Thread (Janeu)
            CreatePartBox("Hips_Dhoti", model.transform, new Vector3(0, 0.85f, 0), new Vector3(0.44f, 0.35f, 0.30f), silkMat);
            CreatePartBox("Torso", model.transform, new Vector3(0, 1.25f, 0), new Vector3(0.42f, 0.45f, 0.28f), skinMat);

            // Angavastram (Silk Shawl draped across chest)
            GameObject shawl = CreatePartBox("Shawl_Drape", model.transform, new Vector3(-0.06f, 1.26f, 0.02f), new Vector3(0.18f, 0.52f, 0.30f), angavastramMat);
            shawl.transform.localRotation = Quaternion.Euler(0, 0, 18f);

            // Sacred Thread (Janeu)
            GameObject janeu = CreatePartBox("Janeu_Thread", model.transform, new Vector3(0.04f, 1.25f, 0.03f), new Vector3(0.02f, 0.48f, 0.29f), angavastramMat);
            janeu.transform.localRotation = Quaternion.Euler(0, 0, -28f);

            // Head, Tilak, Shaved Crown with Choti Tuft
            CreatePartCylinder("Neck", model.transform, new Vector3(0, 1.50f, 0), new Vector3(0.16f, 0.08f, 0.16f), skinMat);
            CreatePartSphere("Head", model.transform, new Vector3(0, 1.68f, 0), new Vector3(0.30f, 0.34f, 0.30f), skinMat);
            CreatePartBox("Tilak_Sandalwood", model.transform, new Vector3(0, 1.74f, 0.145f), new Vector3(0.06f, 0.12f, 0.02f), angavastramMat);
            CreatePartSphere("Choti_Tuft", model.transform, new Vector3(0, 1.82f, -0.14f), new Vector3(0.10f, 0.14f, 0.10f), woodMat);

            // Arms holding brass Aarti Thali
            CreatePartCylinder("Arm_L_Upper", model.transform, new Vector3(-0.28f, 1.28f, 0.08f), new Vector3(0.11f, 0.26f, 0.11f), skinMat).transform.localRotation = Quaternion.Euler(35f, 0, 0);
            CreatePartCylinder("Arm_L_Forearm", model.transform, new Vector3(-0.20f, 1.10f, 0.26f), new Vector3(0.10f, 0.24f, 0.10f), skinMat).transform.localRotation = Quaternion.Euler(75f, -20f, 0);

            CreatePartCylinder("Arm_R_Upper", model.transform, new Vector3(0.28f, 1.28f, 0.08f), new Vector3(0.11f, 0.26f, 0.11f), skinMat).transform.localRotation = Quaternion.Euler(35f, 0, 0);
            CreatePartCylinder("Arm_R_Forearm", model.transform, new Vector3(0.20f, 1.10f, 0.26f), new Vector3(0.10f, 0.24f, 0.10f), skinMat).transform.localRotation = Quaternion.Euler(75f, 20f, 0);

            // Brass Aarti Offering Plate
            CreatePartCylinder("Aarti_Plate", model.transform, new Vector3(0, 1.05f, 0.38f), new Vector3(0.32f, 0.03f, 0.32f), brassMat);
            CreatePartSphere("Aarti_Flame", model.transform, new Vector3(0, 1.12f, 0.38f), new Vector3(0.10f, 0.15f, 0.10f), GetCharacterMaterial("mat_Char_SaffronAccent"));

            // Legs in silk dhoti
            CreatePartCylinder("Leg_L", model.transform, new Vector3(-0.13f, 0.42f, 0), new Vector3(0.15f, 0.42f, 0.15f), silkMat);
            CreatePartCylinder("Leg_R", model.transform, new Vector3(0.13f, 0.42f, 0), new Vector3(0.15f, 0.42f, 0.15f), silkMat);
            CreatePartBox("Foot_L", model.transform, new Vector3(-0.13f, 0.06f, 0.05f), new Vector3(0.12f, 0.10f, 0.22f), skinMat);
            CreatePartBox("Foot_R", model.transform, new Vector3(0.13f, 0.06f, 0.05f), new Vector3(0.12f, 0.10f, 0.22f), skinMat);

            // NPC Helpers: Face player & Dialogue trigger
            root.AddComponent<NPCFacePlayer>();
            NPCDialogueTrigger dt = root.AddComponent<NPCDialogueTrigger>();
            dt.Setup(Constants.Priest, "Priest Madhav", new string[] {
                "Investigator Aryan! Calamity has struck our sacred temple!",
                "The thieves violated the sanctum and ripped the consecrated idols from the altar pedestal.",
                "Search the ransacked shrine room... they fled in a hurry and surely dropped clues."
            }, 2.8f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            string configPath = $"{ConfigBasePath}/PriestConfig.asset";
            CharacterConfig config = AssetDatabase.LoadAssetAtPath<CharacterConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CharacterConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }
            config.SetConfig(Constants.Priest, "Priest Madhav", false, prefab, 3.2f);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CharacterBuilder] Successfully created Priest prefab at '{prefabPath}'.");
            return prefab;
        }

        #endregion

        #region Villager Prefab Builder (Step 3)

        [MenuItem("Tools/Legacy/Characters/Build Villager Prefab")]
        public static GameObject BuildVillagerPrefab()
        {
            EnsureDirectoryExists(PrefabBasePath);
            EnsureDirectoryExists(ConfigBasePath);

            string prefabPath = $"{PrefabBasePath}/Villager.prefab";

            GameObject root = new GameObject("Villager");
            root.tag = "Untagged";

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.72f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.86f, 0);

            GameObject model = new GameObject("Model");
            model.transform.parent = root.transform;

            Material kurtaMat = GetCharacterMaterial("mat_Char_VillagerKurta");
            Material dhotiMat = GetCharacterMaterial("mat_Char_DhotiKhaki");
            Material gamuchaMat = GetCharacterMaterial("mat_Char_SaffronAccent");
            Material skinMat = GetCharacterMaterial("mat_Char_SkinTone");
            Material hairMat = GetCharacterMaterial("mat_Char_DarkHair");

            // Kurta Torso
            CreatePartBox("Torso_Kurta", model.transform, new Vector3(0, 1.22f, 0), new Vector3(0.42f, 0.44f, 0.28f), kurtaMat);
            CreatePartBox("Kurta_Skirt", model.transform, new Vector3(0, 0.90f, 0), new Vector3(0.44f, 0.24f, 0.30f), kurtaMat);

            // Gamucha Towel on Shoulder
            GameObject towel = CreatePartBox("Gamucha_Towel", model.transform, new Vector3(0.20f, 1.34f, 0.02f), new Vector3(0.12f, 0.32f, 0.18f), gamuchaMat);
            towel.transform.localRotation = Quaternion.Euler(0, 0, -12f);

            // Head & Hair
            CreatePartCylinder("Neck", model.transform, new Vector3(0, 1.48f, 0), new Vector3(0.15f, 0.08f, 0.15f), skinMat);
            CreatePartSphere("Head", model.transform, new Vector3(0, 1.66f, 0), new Vector3(0.29f, 0.33f, 0.29f), skinMat);
            CreatePartSphere("Hair", model.transform, new Vector3(0, 1.76f, -0.05f), new Vector3(0.31f, 0.16f, 0.31f), hairMat);

            // Arms (expressive posture)
            CreatePartCylinder("Arm_L_Upper", model.transform, new Vector3(-0.28f, 1.25f, 0), new Vector3(0.11f, 0.26f, 0.11f), kurtaMat);
            CreatePartCylinder("Arm_L_Forearm", model.transform, new Vector3(-0.28f, 1.00f, 0.06f), new Vector3(0.10f, 0.24f, 0.10f), skinMat);
            CreatePartSphere("Hand_L", model.transform, new Vector3(-0.28f, 0.86f, 0.10f), new Vector3(0.11f, 0.11f, 0.11f), skinMat);

            CreatePartCylinder("Arm_R_Upper", model.transform, new Vector3(0.28f, 1.25f, 0), new Vector3(0.11f, 0.26f, 0.11f), kurtaMat);
            CreatePartCylinder("Arm_R_Forearm", model.transform, new Vector3(0.28f, 1.00f, 0.06f), new Vector3(0.10f, 0.24f, 0.10f), skinMat);
            CreatePartSphere("Hand_R", model.transform, new Vector3(0.28f, 0.86f, 0.10f), new Vector3(0.11f, 0.11f, 0.11f), skinMat);

            // Lungi & Feet
            CreatePartBox("Hips_Lungi", model.transform, new Vector3(0, 0.70f, 0), new Vector3(0.40f, 0.28f, 0.28f), dhotiMat);
            CreatePartCylinder("Leg_L", model.transform, new Vector3(-0.13f, 0.38f, 0), new Vector3(0.13f, 0.38f, 0.13f), dhotiMat);
            CreatePartCylinder("Leg_R", model.transform, new Vector3(0.13f, 0.38f, 0), new Vector3(0.13f, 0.38f, 0.13f), dhotiMat);
            CreatePartBox("Foot_L", model.transform, new Vector3(-0.13f, 0.05f, 0.04f), new Vector3(0.12f, 0.10f, 0.22f), skinMat);
            CreatePartBox("Foot_R", model.transform, new Vector3(0.13f, 0.05f, 0.04f), new Vector3(0.12f, 0.10f, 0.22f), skinMat);

            // NPC Helpers: Face player & Dialogue trigger
            root.AddComponent<NPCFacePlayer>();
            NPCDialogueTrigger dt = root.AddComponent<NPCDialogueTrigger>();
            dt.Setup(Constants.Villager, "Villager Ramesh", new string[] {
                "Sahib! I saw two dark shadows fleeing from the temple gates into the night mist!",
                "One was carrying a heavy bulging sack, and the other had a red mask tied across his face.",
                "They hurried down towards the ancient overgrown jungle path by the stream!"
            }, 2.8f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            string configPath = $"{ConfigBasePath}/VillagerConfig.asset";
            CharacterConfig config = AssetDatabase.LoadAssetAtPath<CharacterConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CharacterConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }
            config.SetConfig(Constants.Villager, "Villager Ramesh", false, prefab, 3.8f);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CharacterBuilder] Successfully created Villager prefab at '{prefabPath}'.");
            return prefab;
        }

        #endregion

        #region Robber1 Prefab Builder (Step 3)

        [MenuItem("Tools/Legacy/Characters/Build Robber1 Prefab")]
        public static GameObject BuildRobber1Prefab()
        {
            EnsureDirectoryExists(PrefabBasePath);
            EnsureDirectoryExists(ConfigBasePath);

            string prefabPath = $"{PrefabBasePath}/Robber1.prefab";

            GameObject root = new GameObject("Robber1");
            root.tag = "Untagged";

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.76f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.88f, 0);

            GameObject model = new GameObject("Model");
            model.transform.parent = root.transform;

            Material stealthMat = GetCharacterMaterial("mat_Char_RobberDark");
            Material redMaskMat = GetCharacterMaterial("mat_Char_RobberRedMask");
            Material leatherMat = GetCharacterMaterial("mat_Char_LeatherBrown");
            Material brassMat = GetCharacterMaterial("mat_Char_GoldBrass");
            Material skinMat = GetCharacterMaterial("mat_Char_SkinTone");

            // Midnight Stealth Tunic & Belt
            CreatePartBox("Torso_Stealth", model.transform, new Vector3(0, 1.24f, 0), new Vector3(0.42f, 0.44f, 0.28f), stealthMat);
            CreatePartBox("Belt_Pouch", model.transform, new Vector3(0, 1.02f, 0), new Vector3(0.44f, 0.08f, 0.30f), leatherMat);
            CreatePartSphere("Stolen_JewelPouch", model.transform, new Vector3(-0.20f, 0.98f, 0.12f), new Vector3(0.18f, 0.18f, 0.18f), brassMat);

            // Head with Crimson Face Mask
            CreatePartCylinder("Neck", model.transform, new Vector3(0, 1.50f, 0), new Vector3(0.15f, 0.08f, 0.15f), stealthMat);
            CreatePartSphere("Head", model.transform, new Vector3(0, 1.68f, 0), new Vector3(0.30f, 0.34f, 0.30f), skinMat);
            CreatePartBox("RedCloth_FaceMask", model.transform, new Vector3(0, 1.62f, 0.08f), new Vector3(0.32f, 0.18f, 0.24f), redMaskMat);
            CreatePartSphere("Hood_Dark", model.transform, new Vector3(0, 1.75f, -0.04f), new Vector3(0.33f, 0.22f, 0.33f), stealthMat);

            // Stealth Arms & Dagger
            CreatePartCylinder("Arm_L_Upper", model.transform, new Vector3(-0.29f, 1.25f, 0), new Vector3(0.11f, 0.28f, 0.11f), stealthMat);
            CreatePartCylinder("Arm_L_Forearm", model.transform, new Vector3(-0.29f, 1.00f, 0), new Vector3(0.10f, 0.24f, 0.10f), leatherMat);
            CreatePartSphere("Hand_L", model.transform, new Vector3(-0.29f, 0.85f, 0), new Vector3(0.11f, 0.11f, 0.11f), skinMat);

            CreatePartCylinder("Arm_R_Upper", model.transform, new Vector3(0.29f, 1.25f, 0), new Vector3(0.11f, 0.28f, 0.11f), stealthMat);
            CreatePartCylinder("Arm_R_Forearm", model.transform, new Vector3(0.29f, 1.00f, 0.10f), new Vector3(0.10f, 0.24f, 0.10f), leatherMat);
            CreatePartSphere("Hand_R", model.transform, new Vector3(0.29f, 0.85f, 0.15f), new Vector3(0.11f, 0.11f, 0.11f), skinMat);

            // Dagger in Right Hand
            CreatePartBox("Dagger_Blade", model.transform, new Vector3(0.29f, 0.82f, 0.32f), new Vector3(0.04f, 0.02f, 0.28f), brassMat);
            CreatePartBox("Dagger_Hilt", model.transform, new Vector3(0.29f, 0.82f, 0.18f), new Vector3(0.08f, 0.04f, 0.04f), leatherMat);

            // Dark Pants & Stealth Boots
            CreatePartBox("Hips_Dark", model.transform, new Vector3(0, 0.88f, 0), new Vector3(0.40f, 0.24f, 0.28f), stealthMat);
            CreatePartCylinder("Leg_L", model.transform, new Vector3(-0.13f, 0.55f, 0), new Vector3(0.13f, 0.32f, 0.13f), stealthMat);
            CreatePartBox("Boot_L", model.transform, new Vector3(-0.13f, 0.20f, 0.03f), new Vector3(0.13f, 0.40f, 0.22f), leatherMat);

            CreatePartCylinder("Leg_R", model.transform, new Vector3(0.13f, 0.55f, 0), new Vector3(0.13f, 0.32f, 0.13f), stealthMat);
            CreatePartBox("Boot_R", model.transform, new Vector3(0.13f, 0.20f, 0.03f), new Vector3(0.13f, 0.40f, 0.22f), leatherMat);

            // NPC Helpers: Face player & Dialogue trigger
            root.AddComponent<NPCFacePlayer>();
            NPCDialogueTrigger dt = root.AddComponent<NPCDialogueTrigger>();
            dt.Setup(Constants.Robber1, "Chhaya", new string[] {
                "Tsk. You followed our tracks this far into the jungle, detective?",
                "The sacred idols are already ours. You will not leave this moonlit path alive!"
            }, 3.0f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            string configPath = $"{ConfigBasePath}/Robber1Config.asset";
            CharacterConfig config = AssetDatabase.LoadAssetAtPath<CharacterConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CharacterConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }
            config.SetConfig(Constants.Robber1, "Chhaya (Robber 1)", false, prefab, 5.2f);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CharacterBuilder] Successfully created Robber1 prefab at '{prefabPath}'.");
            return prefab;
        }

        #endregion

        #region Robber2 Prefab Builder (Step 3)

        [MenuItem("Tools/Legacy/Characters/Build Robber2 Prefab")]
        public static GameObject BuildRobber2Prefab()
        {
            EnsureDirectoryExists(PrefabBasePath);
            EnsureDirectoryExists(ConfigBasePath);

            string prefabPath = $"{PrefabBasePath}/Robber2.prefab";

            GameObject root = new GameObject("Robber2");
            root.tag = "Untagged";

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.90f;
            cc.radius = 0.42f;
            cc.center = new Vector3(0, 0.95f, 0);

            GameObject model = new GameObject("Model");
            model.transform.parent = root.transform;

            Material leatherMat = GetCharacterMaterial("mat_Char_LeatherBrown");
            Material stealthMat = GetCharacterMaterial("mat_Char_RobberDark");
            Material redClothMat = GetCharacterMaterial("mat_Char_RobberRedMask");
            Material skinMat = GetCharacterMaterial("mat_Char_SkinTone");
            Material brassMat = GetCharacterMaterial("mat_Char_GoldBrass");
            Material sackMat = GetCharacterMaterial("mat_Char_DhotiKhaki");

            // Broad Heavy Torso & Leather Vest
            CreatePartBox("Torso_Heavy", model.transform, new Vector3(0, 1.35f, 0), new Vector3(0.55f, 0.50f, 0.36f), leatherMat);
            CreatePartBox("Belt_Heavy", model.transform, new Vector3(0, 1.10f, 0), new Vector3(0.56f, 0.12f, 0.38f), stealthMat);

            // Heavy Sack for Stolen Idols slung over back
            GameObject sack = CreatePartSphere("Stolen_IdolSack", model.transform, new Vector3(0.08f, 1.40f, -0.32f), new Vector3(0.48f, 0.65f, 0.44f), sackMat);
            sack.transform.localRotation = Quaternion.Euler(20f, 0, 10f);

            // Head, Red Armband, Fierce Beard
            CreatePartCylinder("Neck", model.transform, new Vector3(0, 1.62f, 0), new Vector3(0.20f, 0.10f, 0.20f), skinMat);
            CreatePartSphere("Head", model.transform, new Vector3(0, 1.82f, 0), new Vector3(0.34f, 0.38f, 0.34f), skinMat);
            CreatePartBox("Beard", model.transform, new Vector3(0, 1.74f, 0.14f), new Vector3(0.28f, 0.22f, 0.18f), GetCharacterMaterial("mat_Char_DarkHair"));

            // Brawny Arms & Crowbar / Iron Pry-Bar
            CreatePartCylinder("Arm_L_Upper", model.transform, new Vector3(-0.36f, 1.35f, 0), new Vector3(0.16f, 0.30f, 0.16f), skinMat);
            CreatePartCylinder("Arm_L_Armband", model.transform, new Vector3(-0.36f, 1.35f, 0), new Vector3(0.17f, 0.08f, 0.17f), redClothMat);
            CreatePartCylinder("Arm_L_Forearm", model.transform, new Vector3(-0.36f, 1.05f, 0), new Vector3(0.14f, 0.26f, 0.14f), skinMat);
            CreatePartSphere("Hand_L", model.transform, new Vector3(-0.36f, 0.88f, 0), new Vector3(0.14f, 0.14f, 0.14f), skinMat);

            CreatePartCylinder("Arm_R_Upper", model.transform, new Vector3(0.36f, 1.35f, 0), new Vector3(0.16f, 0.30f, 0.16f), skinMat);
            CreatePartCylinder("Arm_R_Armband", model.transform, new Vector3(0.36f, 1.35f, 0), new Vector3(0.17f, 0.08f, 0.17f), redClothMat);
            CreatePartCylinder("Arm_R_Forearm", model.transform, new Vector3(0.36f, 1.05f, 0.10f), new Vector3(0.14f, 0.26f, 0.14f), skinMat);
            CreatePartSphere("Hand_R", model.transform, new Vector3(0.36f, 0.88f, 0.16f), new Vector3(0.14f, 0.14f, 0.14f), skinMat);

            // Iron Crowbar / Prybar in Hand
            CreatePartCylinder("Crowbar_Shaft", model.transform, new Vector3(0.36f, 0.85f, 0.30f), new Vector3(0.05f, 0.85f, 0.05f), stealthMat).transform.localRotation = Quaternion.Euler(70f, 0, 0);

            // Heavy Legs & Boots
            CreatePartBox("Hips_Heavy", model.transform, new Vector3(0, 0.95f, 0), new Vector3(0.50f, 0.28f, 0.34f), stealthMat);
            CreatePartCylinder("Leg_L", model.transform, new Vector3(-0.16f, 0.60f, 0), new Vector3(0.17f, 0.35f, 0.17f), stealthMat);
            CreatePartBox("Boot_L", model.transform, new Vector3(-0.16f, 0.24f, 0.04f), new Vector3(0.17f, 0.44f, 0.26f), leatherMat);

            CreatePartCylinder("Leg_R", model.transform, new Vector3(0.16f, 0.60f, 0), new Vector3(0.17f, 0.35f, 0.17f), stealthMat);
            CreatePartBox("Boot_R", model.transform, new Vector3(0.16f, 0.24f, 0.04f), new Vector3(0.17f, 0.44f, 0.26f), leatherMat);

            // NPC Helpers: Face player & Dialogue trigger
            root.AddComponent<NPCFacePlayer>();
            NPCDialogueTrigger dt = root.AddComponent<NPCDialogueTrigger>();
            dt.Setup(Constants.Robber2, "Vikram", new string[] {
                "Back off, detective! These golden deities will fetch a fortune!",
                "Take one more step and my iron crowbar will crack your skull!"
            }, 3.0f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            string configPath = $"{ConfigBasePath}/Robber2Config.asset";
            CharacterConfig config = AssetDatabase.LoadAssetAtPath<CharacterConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CharacterConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }
            config.SetConfig(Constants.Robber2, "Vikram (Robber 2)", false, prefab, 4.0f);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CharacterBuilder] Successfully created Robber2 prefab at '{prefabPath}'.");
            return prefab;
        }

        #endregion

        #region Master Build All Characters

        [MenuItem("Tools/Legacy/Characters/Build All Character Prefabs")]
        public static void BuildAllCharacterPrefabs()
        {
            BuildInvestigatorPrefab();
            BuildPriestPrefab();
            BuildVillagerPrefab();
            BuildRobber1Prefab();
            BuildRobber2Prefab();

            // Register all configs in CharacterManager if present in open scene
            CharacterManager cm = UnityEngine.Object.FindAnyObjectByType<CharacterManager>();
            if (cm != null)
            {
                string[] configNames = { "InvestigatorConfig", "PriestConfig", "VillagerConfig", "Robber1Config", "Robber2Config" };
                foreach (string cn in configNames)
                {
                    CharacterConfig cfg = AssetDatabase.LoadAssetAtPath<CharacterConfig>($"{ConfigBasePath}/{cn}.asset");
                    if (cfg != null) cm.RegisterConfig(cfg);
                }
            }

            Debug.Log("[CharacterBuilder] Successfully built all 5 character prefabs (Investigator, Priest, Villager, Robber1, Robber2)!");
        }

        public static void BuildAllCharactersBatch()
        {
            BuildAllCharacterPrefabs();
        }

        public static void BuildAllAndPlacePlayerBatch()
        {
            BuildAllCharacterPrefabs();
            BuildCharacterTestScene();
            PlacePlayerInCourtyard();
        }

        #endregion

        #region Helper Geometry Methods

        public static GameObject CreatePartBox(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.parent = parent;
            obj.transform.localPosition = localPos;
            obj.transform.localScale = size;
            obj.transform.localRotation = Quaternion.identity;

            if (mat != null) obj.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Remove internal colliders since root CharacterController handles physics
            BoxCollider col = obj.GetComponent<BoxCollider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            return obj;
        }

        public static GameObject CreatePartCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.parent = parent;
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            obj.transform.localRotation = Quaternion.identity;

            if (mat != null) obj.GetComponent<MeshRenderer>().sharedMaterial = mat;

            CapsuleCollider col = obj.GetComponent<CapsuleCollider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            return obj;
        }

        public static GameObject CreatePartSphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.parent = parent;
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            obj.transform.localRotation = Quaternion.identity;

            if (mat != null) obj.GetComponent<MeshRenderer>().sharedMaterial = mat;

            SphereCollider col = obj.GetComponent<SphereCollider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            return obj;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        #endregion
    }
}
