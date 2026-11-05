using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.World;

namespace LegacyOfTemples.World.Editor
{
    public static class EnvironmentBuilder
    {
        private const string ScenesBasePath = "Assets/_Project/Scenes/Environments";
        private const string MaterialsBasePath = "Assets/_Project/Models/Environment/Materials";

        #region Material Factory

        public static Material GetMaterial(string matName)
        {
            if (!Directory.Exists(MaterialsBasePath))
            {
                Directory.CreateDirectory(MaterialsBasePath);
                AssetDatabase.Refresh();
            }

            string assetPath = $"{MaterialsBasePath}/{matName}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader);
            mat.name = matName;

            switch (matName)
            {
                case "mat_StoneTemple":
                    mat.SetColor("_BaseColor", new Color(0.72f, 0.65f, 0.58f));
                    mat.SetFloat("_Smoothness", 0.22f);
                    mat.SetFloat("_Metallic", 0.02f);
                    break;

                case "mat_StoneFloorWet":
                    mat.SetColor("_BaseColor", new Color(0.24f, 0.22f, 0.21f));
                    mat.SetFloat("_Smoothness", 0.82f);
                    mat.SetFloat("_Metallic", 0.15f);
                    break;

                case "mat_CarvedPillar":
                    mat.SetColor("_BaseColor", new Color(0.65f, 0.56f, 0.48f));
                    mat.SetFloat("_Smoothness", 0.28f);
                    mat.SetFloat("_Metallic", 0.05f);
                    break;

                case "mat_GoldBrass":
                    mat.SetColor("_BaseColor", new Color(0.88f, 0.72f, 0.28f));
                    mat.SetFloat("_Smoothness", 0.82f);
                    mat.SetFloat("_Metallic", 0.92f);
                    break;

                case "mat_Flame":
                    mat.SetColor("_BaseColor", new Color(1.0f, 0.65f, 0.15f));
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1.0f, 0.55f, 0.1f) * 4.0f);
                    break;

                case "mat_SaffronCloth":
                    mat.SetColor("_BaseColor", new Color(0.92f, 0.42f, 0.08f));
                    mat.SetFloat("_Smoothness", 0.15f);
                    break;

                case "mat_DarkRedCloth":
                    mat.SetColor("_BaseColor", new Color(0.68f, 0.12f, 0.14f));
                    mat.SetFloat("_Smoothness", 0.20f);
                    break;

                case "mat_MarigoldFlowers":
                    mat.SetColor("_BaseColor", new Color(1.0f, 0.68f, 0.02f));
                    mat.SetFloat("_Smoothness", 0.30f);
                    break;

                case "mat_DarkWood":
                    mat.SetColor("_BaseColor", new Color(0.32f, 0.22f, 0.14f));
                    mat.SetFloat("_Smoothness", 0.35f);
                    break;

                case "mat_LimestoneRelief":
                    mat.SetColor("_BaseColor", new Color(0.86f, 0.84f, 0.80f));
                    mat.SetFloat("_Smoothness", 0.32f);
                    break;

                case "mat_JungleMoss":
                    mat.SetColor("_BaseColor", new Color(0.18f, 0.34f, 0.15f));
                    mat.SetFloat("_Smoothness", 0.18f);
                    break;

                case "mat_JungleBark":
                    mat.SetColor("_BaseColor", new Color(0.26f, 0.18f, 0.12f));
                    mat.SetFloat("_Smoothness", 0.15f);
                    break;

                case "mat_JungleStream":
                    mat.SetColor("_BaseColor", new Color(0.12f, 0.32f, 0.46f));
                    mat.SetFloat("_Smoothness", 0.94f);
                    mat.SetFloat("_Metallic", 0.1f);
                    break;

                case "mat_MudPath":
                    mat.SetColor("_BaseColor", new Color(0.22f, 0.18f, 0.13f));
                    mat.SetFloat("_Smoothness", 0.55f);
                    break;

                case "mat_CrimeFootprint":
                    mat.SetColor("_BaseColor", new Color(0.45f, 0.28f, 0.18f));
                    mat.SetFloat("_Smoothness", 0.40f);
                    break;

                default:
                    mat.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f));
                    break;
            }

            AssetDatabase.CreateAsset(mat, assetPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        #endregion

        #region Geometry & Object Helpers

        public static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, bool addCollider = true)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.parent = parent;
            obj.transform.localPosition = localPos;
            obj.transform.localScale = size;
            obj.transform.localRotation = Quaternion.identity;

            if (mat != null)
            {
                obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            if (!addCollider)
            {
                BoxCollider col = obj.GetComponent<BoxCollider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }

            return obj;
        }

        public static GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool addCollider = true)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.parent = parent;
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            obj.transform.localRotation = Quaternion.identity;

            if (mat != null)
            {
                obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            if (!addCollider)
            {
                CapsuleCollider col = obj.GetComponent<CapsuleCollider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }

            return obj;
        }

        public static GameObject CreateSphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool addCollider = true)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.parent = parent;
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            obj.transform.localRotation = Quaternion.identity;

            if (mat != null)
            {
                obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            if (!addCollider)
            {
                SphereCollider col = obj.GetComponent<SphereCollider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }

            return obj;
        }

        public static GameObject CreateSpawnPoint(Transform parent, string spawnId, Vector3 localPos, Vector3 forwardDir)
        {
            GameObject spObj = new GameObject($"Spawn_{spawnId}");
            spObj.transform.parent = parent;
            spObj.transform.localPosition = localPos;
            spObj.transform.localRotation = Quaternion.LookRotation(forwardDir.normalized, Vector3.up);

            SpawnPoint sp = spObj.AddComponent<SpawnPoint>();
            sp.SetSpawnId(spawnId);
            return spObj;
        }

        public static GameObject CreateInteractable(Transform parent, string id, string prompt, Vector3 localPos, float radius = 2.5f, Vector3? colliderSize = null)
        {
            GameObject intObj = new GameObject($"Interactable_{id}");
            intObj.transform.parent = parent;
            intObj.transform.localPosition = localPos;
            intObj.transform.localRotation = Quaternion.identity;

            InteractableObject io = intObj.AddComponent<InteractableObject>();
            io.Setup(id, prompt, radius);

            BoxCollider col = intObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = colliderSize ?? new Vector3(radius * 1.5f, 2.0f, radius * 1.5f);
            col.center = new Vector3(0, col.size.y * 0.5f, 0);

            return intObj;
        }

        public static GameObject CreateOilLamp(Transform parent, Vector3 localPos, string lampName = "OilLamp", bool addLight = true)
        {
            GameObject root = new GameObject(lampName);
            root.transform.parent = parent;
            root.transform.localPosition = localPos;

            Material brassMat = GetMaterial("mat_GoldBrass");
            Material flameMat = GetMaterial("mat_Flame");

            // Base stand & tiered pole
            CreateCylinder("BasePedestal", root.transform, new Vector3(0, 0.15f, 0), new Vector3(0.6f, 0.15f, 0.6f), brassMat, true);
            CreateCylinder("Pillar", root.transform, new Vector3(0, 0.85f, 0), new Vector3(0.18f, 0.7f, 0.18f), brassMat, true);
            CreateCylinder("OilBowl", root.transform, new Vector3(0, 1.55f, 0), new Vector3(0.7f, 0.1f, 0.7f), brassMat, false);
            CreateSphere("FlameMesh", root.transform, new Vector3(0, 1.72f, 0), new Vector3(0.22f, 0.32f, 0.22f), flameMat, false);

            if (addLight)
            {
                GameObject lightObj = new GameObject("LampLight");
                lightObj.transform.parent = root.transform;
                lightObj.transform.localPosition = new Vector3(0, 1.8f, 0);

                Light lt = lightObj.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.color = new Color(1.0f, 0.62f, 0.18f);
                lt.intensity = 2.2f;
                lt.range = 8.5f;
                lt.shadows = LightShadows.Soft;

                LightFlicker flicker = lightObj.AddComponent<LightFlicker>();
                flicker.Configure(1.4f, 2.6f, 13f);
            }

            return root;
        }

        public static GameObject CreateTorch(Transform parent, Vector3 localPos, Vector3 localEuler, string torchName = "WallTorch")
        {
            GameObject root = new GameObject(torchName);
            root.transform.parent = parent;
            root.transform.localPosition = localPos;
            root.transform.localEulerAngles = localEuler;

            Material darkWoodMat = GetMaterial("mat_DarkWood");
            Material brassMat = GetMaterial("mat_GoldBrass");
            Material flameMat = GetMaterial("mat_Flame");

            CreateCylinder("SconceBracket", root.transform, new Vector3(0, 0, 0.15f), new Vector3(0.08f, 0.25f, 0.08f), brassMat, false);
            CreateCylinder("Handle", root.transform, new Vector3(0, 0.35f, 0), new Vector3(0.12f, 0.35f, 0.12f), darkWoodMat, false);
            CreateCylinder("Cup", root.transform, new Vector3(0, 0.7f, 0), new Vector3(0.25f, 0.12f, 0.25f), brassMat, false);
            CreateSphere("TorchFlame", root.transform, new Vector3(0, 0.88f, 0), new Vector3(0.25f, 0.35f, 0.25f), flameMat, false);

            GameObject lightObj = new GameObject("TorchLight");
            lightObj.transform.parent = root.transform;
            lightObj.transform.localPosition = new Vector3(0, 0.95f, 0);

            Light lt = lightObj.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.color = new Color(1.0f, 0.58f, 0.14f);
            lt.intensity = 2.5f;
            lt.range = 9.0f;
            lt.shadows = LightShadows.Soft;

            LightFlicker flicker = lightObj.AddComponent<LightFlicker>();
            flicker.Configure(1.6f, 2.8f, 14f);

            return root;
        }

        public static void SetupStandardCameraAndManager(Scene scene, Vector3 camPos, Vector3 camEuler, string initialSceneName)
        {
            // System root
            GameObject sysRoot = new GameObject("--- SYSTEM ---");

            // Main Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.transform.parent = sysRoot.transform;
            camObj.transform.position = camPos;
            camObj.transform.eulerAngles = camEuler;
            camObj.tag = "MainCamera";

            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            camObj.AddComponent<AudioListener>();

            // World Manager & Dev Tools
            GameObject wmObj = new GameObject("WorldManager");
            wmObj.transform.parent = sysRoot.transform;
            wmObj.AddComponent<WorldManager>();
            wmObj.AddComponent<WorldDevTools>();
        }

        #endregion

        #region Courtyard Scene (Step 2)

        [MenuItem("Tools/Legacy/Environments/Build Courtyard")]
        public static void BuildCourtyard()
        {
            EnsureDirectoryExists(ScenesBasePath);
            string scenePath = $"{ScenesBasePath}/Courtyard.unity";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Atmosphere & System
            SetupStandardCameraAndManager(scene, new Vector3(0f, 5.0f, 23.0f), new Vector3(12f, 180f, 0f), Constants.Courtyard);

            // Lighting & Fog
            GameObject lightRoot = new GameObject("--- LIGHTING & ATMOSPHERE ---");

            GameObject sunObj = new GameObject("Directional_SunriseSun");
            sunObj.transform.parent = lightRoot.transform;
            sunObj.transform.rotation = Quaternion.Euler(18f, -40f, 0f);
            Light sunLight = sunObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1.0f, 0.78f, 0.58f);
            sunLight.intensity = 1.35f;
            sunLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.32f, 0.38f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.92f, 0.65f, 0.52f);

            // 2. Materials
            Material stoneMat = GetMaterial("mat_StoneTemple");
            Material wetFloorMat = GetMaterial("mat_StoneFloorWet");
            Material brassMat = GetMaterial("mat_GoldBrass");
            Material saffronMat = GetMaterial("mat_SaffronCloth");

            // 3. Environment Hierarchy
            GameObject envRoot = new GameObject("--- ENVIRONMENT ---");

            // Wet reflective stone plaza (60m x 60m)
            CreateBox("Plaza_WetStoneFloor", envRoot.transform, new Vector3(0, -0.25f, 0), new Vector3(60f, 0.5f, 60f), wetFloorMat, true);

            // Boundary Walls (Prakara)
            GameObject wallsRoot = new GameObject("Plaza_BoundaryWalls");
            wallsRoot.transform.parent = envRoot.transform;
            CreateBox("Wall_North", wallsRoot.transform, new Vector3(0, 1.5f, -30f), new Vector3(60f, 3.0f, 1.2f), stoneMat, true);
            CreateBox("Wall_South", wallsRoot.transform, new Vector3(0, 1.5f, 30f), new Vector3(60f, 3.0f, 1.2f), stoneMat, true);
            CreateBox("Wall_East", wallsRoot.transform, new Vector3(30f, 1.5f, 0), new Vector3(1.2f, 3.0f, 60f), stoneMat, true);
            CreateBox("Wall_West", wallsRoot.transform, new Vector3(-30f, 1.5f, 0), new Vector3(1.2f, 3.0f, 60f), stoneMat, true);

            // Grand Rekha Deula Temple Complex
            GameObject templeRoot = new GameObject("Temple_RekhaDeula");
            templeRoot.transform.parent = envRoot.transform;

            // Pista (Plinth base)
            CreateBox("Pista_Plinth", templeRoot.transform, new Vector3(0, 0.7f, -16f), new Vector3(24f, 1.4f, 24f), stoneMat, true);

            // Bada (Sanctum base tower)
            CreateBox("Bada_SanctumBase", templeRoot.transform, new Vector3(0, 5.9f, -16f), new Vector3(15f, 9.0f, 15f), stoneMat, true);

            // Pilasters / Rathas on Bada
            CreateBox("Ratha_North", templeRoot.transform, new Vector3(0, 5.9f, -23.6f), new Vector3(4f, 8.8f, 0.6f), stoneMat, true);
            CreateBox("Ratha_East", templeRoot.transform, new Vector3(7.6f, 5.9f, -16f), new Vector3(0.6f, 8.8f, 4f), stoneMat, true);
            CreateBox("Ratha_West", templeRoot.transform, new Vector3(-7.6f, 5.9f, -16f), new Vector3(0.6f, 8.8f, 4f), stoneMat, true);

            // Gandi (Curvilinear Sikhara Spire - 8 Corbelled Tiers)
            GameObject spireRoot = new GameObject("Gandi_SikharaTiers");
            spireRoot.transform.parent = templeRoot.transform;
            float[] tierWidths = { 14.2f, 13.2f, 12.0f, 10.7f, 9.4f, 8.0f, 6.6f, 5.2f };
            float curY = 11.4f;
            for (int i = 0; i < tierWidths.Length; i++)
            {
                CreateBox($"SpireTier_{i + 1}", spireRoot.transform, new Vector3(0, curY, -16f), new Vector3(tierWidths[i], 2.0f, tierWidths[i]), stoneMat, true);
                curY += 2.0f;
            }

            // Mastaka (Crown elements)
            GameObject mastakaRoot = new GameObject("Mastaka_Crown");
            mastakaRoot.transform.parent = templeRoot.transform;

            // Beki (Neck cylinder)
            CreateCylinder("Beki_Neck", mastakaRoot.transform, new Vector3(0, curY + 0.6f, -16f), new Vector3(4.5f, 0.6f, 4.5f), stoneMat, true);
            curY += 1.2f;

            // Amalaka (Ribbed fluted crown disc)
            CreateCylinder("Amalaka_Disc", mastakaRoot.transform, new Vector3(0, curY + 0.7f, -16f), new Vector3(6.8f, 0.7f, 6.8f), brassMat, true);
            curY += 1.4f;

            // Khapuri & Kalasa (Sacred Pot)
            CreateSphere("Khapuri_Dome", mastakaRoot.transform, new Vector3(0, curY + 0.5f, -16f), new Vector3(4.0f, 1.0f, 4.0f), stoneMat, false);
            CreateSphere("Kalasa_Urn", mastakaRoot.transform, new Vector3(0, curY + 1.6f, -16f), new Vector3(2.4f, 2.2f, 2.4f), brassMat, false);

            // Ayudha (Golden Finial) & Flag
            CreateCylinder("Ayudha_Finial", mastakaRoot.transform, new Vector3(0, curY + 3.2f, -16f), new Vector3(0.4f, 1.4f, 0.4f), brassMat, false);
            GameObject flagPole = CreateCylinder("FlagPole", mastakaRoot.transform, new Vector3(0.2f, curY + 4.2f, -16f), new Vector3(0.12f, 1.6f, 0.12f), brassMat, false);
            flagPole.transform.localRotation = Quaternion.Euler(0, 0, -15f);
            CreateBox("Dhvaja_SaffronFlag", flagPole.transform, new Vector3(1.0f, 0.8f, 0), new Vector3(2.0f, 0.8f, 0.05f), saffronMat, false);

            // Jagamohana (Porch Assembly Hall in Front)
            GameObject jagamohanaRoot = new GameObject("Jagamohana_PorchHall");
            jagamohanaRoot.transform.parent = templeRoot.transform;
            CreateBox("Jagamohana_Base", jagamohanaRoot.transform, new Vector3(0, 4.15f, -5.5f), new Vector3(11.0f, 5.5f, 9.0f), stoneMat, true);

            // Jagamohana Pyramidal Roof (Pida Deula tiers)
            CreateBox("PidaTier_1", jagamohanaRoot.transform, new Vector3(0, 7.3f, -5.5f), new Vector3(10.2f, 0.8f, 8.2f), stoneMat, true);
            CreateBox("PidaTier_2", jagamohanaRoot.transform, new Vector3(0, 8.1f, -5.5f), new Vector3(8.6f, 0.8f, 6.8f), stoneMat, true);
            CreateBox("PidaTier_3", jagamohanaRoot.transform, new Vector3(0, 8.9f, -5.5f), new Vector3(7.0f, 0.8f, 5.4f), stoneMat, true);

            // Sanctum Entrance Portal & Wooden Doors
            Material woodMat = GetMaterial("mat_DarkWood");
            CreateBox("PortalArch_Left", jagamohanaRoot.transform, new Vector3(-1.8f, 2.5f, -0.9f), new Vector3(0.8f, 4.0f, 0.6f), stoneMat, true);
            CreateBox("PortalArch_Right", jagamohanaRoot.transform, new Vector3(1.8f, 2.5f, -0.9f), new Vector3(0.8f, 4.0f, 0.6f), stoneMat, true);
            CreateBox("PortalArch_Top", jagamohanaRoot.transform, new Vector3(0, 4.7f, -0.9f), new Vector3(4.4f, 0.6f, 0.6f), stoneMat, true);
            CreateBox("SanctumDoor_Left", jagamohanaRoot.transform, new Vector3(-0.75f, 2.2f, -1.05f), new Vector3(1.4f, 3.4f, 0.15f), woodMat, true);
            CreateBox("SanctumDoor_Right", jagamohanaRoot.transform, new Vector3(0.75f, 2.2f, -1.05f), new Vector3(1.4f, 3.4f, 0.15f), woodMat, true);

            // Grand Stone Steps descending to Plaza
            GameObject stepsRoot = new GameObject("EntranceSteps");
            stepsRoot.transform.parent = templeRoot.transform;
            int numSteps = 5;
            float stepDepth = 0.7f;
            float stepHeight = 0.28f;
            for (int s = 0; s < numSteps; s++)
            {
                float zPos = -1.0f + s * stepDepth;
                float yPos = 1.4f - (s + 1) * stepHeight;
                CreateBox($"Step_{s + 1}", stepsRoot.transform, new Vector3(0, yPos + stepHeight * 0.5f, zPos + stepDepth * 0.5f), new Vector3(5.5f, stepHeight, stepDepth), stoneMat, true);
            }

            // Two Gajasimha (Guardian Lion) Statues Flanking the Steps
            GameObject lionsRoot = new GameObject("Gajasimha_GuardianLions");
            lionsRoot.transform.parent = envRoot.transform;
            BuildGajasimhaStatue("LionStatue_Left", lionsRoot.transform, new Vector3(-3.5f, 0, 1.8f), 0f, stoneMat, brassMat);
            BuildGajasimhaStatue("LionStatue_Right", lionsRoot.transform, new Vector3(3.5f, 0, 1.8f), 0f, stoneMat, brassMat);

            // Brass Oil Lamps (4 Flickering Warm Lights)
            GameObject lampsRoot = new GameObject("Brass_OilLamps");
            lampsRoot.transform.parent = envRoot.transform;
            CreateOilLamp(lampsRoot.transform, new Vector3(-6.0f, 0, 6.5f), "Deepastambha_LeftFront");
            CreateOilLamp(lampsRoot.transform, new Vector3(6.0f, 0, 6.5f), "Deepastambha_RightFront");
            CreateOilLamp(lampsRoot.transform, new Vector3(-4.0f, 1.4f, -0.6f), "Deepastambha_LeftPlinth");
            CreateOilLamp(lampsRoot.transform, new Vector3(4.0f, 1.4f, -0.6f), "Deepastambha_RightPlinth");

            // 4. Spawns
            GameObject spawnRoot = new GameObject("--- SPAWN POINTS ---");
            CreateSpawnPoint(spawnRoot.transform, "player_start", new Vector3(0f, 0f, 16f), new Vector3(0, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "priest_spawn", new Vector3(3f, 0f, 2f), new Vector3(-1, 0, 0));
            CreateSpawnPoint(spawnRoot.transform, "villager_spawn", new Vector3(-8f, 0f, 10f), new Vector3(1, 0, -0.5f));
            CreateSpawnPoint(spawnRoot.transform, "robber_spawn", new Vector3(12f, 0f, -12f), new Vector3(-1, 0, 0.5f));

            // 5. Interactables
            GameObject interactRoot = new GameObject("--- INTERACTABLES ---");
            CreateInteractable(interactRoot.transform, "courtyard_lion_left", "Inspect Gajasimha guardian statue", new Vector3(-3.5f, 0.8f, 1.8f), 2.5f);
            CreateInteractable(interactRoot.transform, "courtyard_lion_right", "Inspect Gajasimha guardian statue", new Vector3(3.5f, 0.8f, 1.8f), 2.5f);
            CreateInteractable(interactRoot.transform, "courtyard_lamp", "Examine brass oil lamp", new Vector3(-6.0f, 0.8f, 6.5f), 2.2f);
            CreateInteractable(interactRoot.transform, "courtyard_sanctum_entrance", "Enter the inner sanctum", new Vector3(0f, 1.4f, -1.2f), 3.0f);

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] Courtyard scene generated successfully at '{scenePath}'.");
            UpdateBuildSettings();
        }

        private static void BuildGajasimhaStatue(string name, Transform parent, Vector3 localPos, float rotY, Material stoneMat, Material accentMat)
        {
            GameObject lionObj = new GameObject(name);
            lionObj.transform.parent = parent;
            lionObj.transform.localPosition = localPos;
            lionObj.transform.localRotation = Quaternion.Euler(0, rotY, 0);

            // Pedestal Plinth
            CreateBox("Pedestal_Lower", lionObj.transform, new Vector3(0, 0.35f, 0), new Vector3(1.4f, 0.7f, 1.8f), stoneMat, true);
            CreateBox("Pedestal_Upper", lionObj.transform, new Vector3(0, 0.8f, 0), new Vector3(1.2f, 0.2f, 1.6f), accentMat, true);

            // Sculpted Guardian Lion (Upright Seated Odisha Gajasimha)
            GameObject body = CreateBox("Lion_Torso", lionObj.transform, new Vector3(0, 1.6f, -0.1f), new Vector3(0.9f, 1.4f, 1.1f), stoneMat, true);
            body.transform.localRotation = Quaternion.Euler(-15f, 0, 0);

            CreateSphere("Lion_Head", lionObj.transform, new Vector3(0, 2.45f, 0.25f), new Vector3(0.85f, 0.85f, 0.95f), stoneMat, true);
            CreateBox("Lion_SnoutMouth", lionObj.transform, new Vector3(0, 2.25f, 0.75f), new Vector3(0.55f, 0.4f, 0.45f), accentMat, false);
            CreateSphere("Lion_CrownFinial", lionObj.transform, new Vector3(0, 2.95f, 0.15f), new Vector3(0.45f, 0.5f, 0.45f), accentMat, false);

            // Front Paws
            CreateCylinder("Paw_Left", lionObj.transform, new Vector3(-0.35f, 1.15f, 0.55f), new Vector3(0.25f, 0.6f, 0.25f), stoneMat, false);
            CreateCylinder("Paw_Right", lionObj.transform, new Vector3(0.35f, 1.15f, 0.55f), new Vector3(0.25f, 0.6f, 0.25f), stoneMat, false);
        }

        #endregion

        #region Sanctum, PuzzleWall, ShrineCrime, JunglePath Stubs (to be completed in next steps)

        [MenuItem("Tools/Legacy/Environments/Build Sanctum")]
        public static void BuildSanctum()
        {
            EnsureDirectoryExists(ScenesBasePath);
            string scenePath = $"{ScenesBasePath}/Sanctum.unity";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. System & Atmosphere
            SetupStandardCameraAndManager(scene, new Vector3(0f, 3.2f, 13.5f), new Vector3(7f, 180f, 0f), Constants.Sanctum);

            GameObject lightRoot = new GameObject("--- LIGHTING & ATMOSPHERE ---");

            // Cool moonlight beam through high ceiling clerestory
            GameObject moonObj = new GameObject("Directional_MoonlightBeam");
            moonObj.transform.parent = lightRoot.transform;
            moonObj.transform.rotation = Quaternion.Euler(50f, 25f, 0f);
            Light moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.28f, 0.42f, 0.65f);
            moonLight.intensity = 0.35f;
            moonLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.05f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.024f;
            RenderSettings.fogColor = new Color(0.12f, 0.09f, 0.10f);

            // Light shaft volume towards the central altar
            GameObject shaft = CreateCylinder("Moonlight_ShaftVolume", lightRoot.transform, new Vector3(0, 4.8f, -7.0f), new Vector3(2.6f, 4.5f, 2.6f), GetMaterial("mat_LimestoneRelief"), false);
            shaft.transform.localRotation = Quaternion.Euler(15f, 0, 0);

            // 2. Materials
            Material stoneMat = GetMaterial("mat_StoneTemple");
            Material wetFloorMat = GetMaterial("mat_StoneFloorWet");
            Material pillarMat = GetMaterial("mat_CarvedPillar");
            Material brassMat = GetMaterial("mat_GoldBrass");
            Material redSilkMat = GetMaterial("mat_DarkRedCloth");
            Material saffronMat = GetMaterial("mat_SaffronCloth");
            Material flowerMat = GetMaterial("mat_MarigoldFlowers");
            Material darkWoodMat = GetMaterial("mat_DarkWood");

            // 3. Environment Hierarchy
            GameObject envRoot = new GameObject("--- ENVIRONMENT ---");

            // Sanctum Hall Floor
            CreateBox("Sanctum_Floor", envRoot.transform, new Vector3(0, -0.25f, 0), new Vector3(18f, 0.5f, 26f), wetFloorMat, true);

            // High Stone Perimeter Walls
            GameObject wallsRoot = new GameObject("Sanctum_Walls");
            wallsRoot.transform.parent = envRoot.transform;
            CreateBox("Wall_North", wallsRoot.transform, new Vector3(0, 4.25f, -13f), new Vector3(18f, 8.5f, 1.2f), stoneMat, true);
            CreateBox("Wall_East", wallsRoot.transform, new Vector3(9f, 4.25f, 0), new Vector3(1.2f, 8.5f, 26f), stoneMat, true);
            CreateBox("Wall_West", wallsRoot.transform, new Vector3(-9f, 4.25f, 0), new Vector3(1.2f, 8.5f, 26f), stoneMat, true);

            // South Entrance Wall with Open Portal
            CreateBox("Wall_South_Left", wallsRoot.transform, new Vector3(-5.5f, 4.25f, 13f), new Vector3(7f, 8.5f, 1.2f), stoneMat, true);
            CreateBox("Wall_South_Right", wallsRoot.transform, new Vector3(5.5f, 4.25f, 13f), new Vector3(7f, 8.5f, 1.2f), stoneMat, true);
            CreateBox("Wall_South_ArchTop", wallsRoot.transform, new Vector3(0, 6.75f, 13f), new Vector3(4.2f, 3.5f, 1.2f), stoneMat, true);

            // Vaulted Ceiling & Stone Beams
            CreateBox("Ceiling_Main", envRoot.transform, new Vector3(0, 8.75f, 0), new Vector3(18.5f, 0.5f, 26.5f), stoneMat, true);
            for (float bz = -10f; bz <= 10f; bz += 5f)
            {
                CreateBox($"CeilingBeam_Z{bz:F0}", envRoot.transform, new Vector3(0, 8.2f, bz), new Vector3(17.8f, 0.6f, 0.8f), stoneMat, true);
            }

            // 8 Carved Colonnade Pillars
            GameObject pillarsRoot = new GameObject("Sanctum_Pillars");
            pillarsRoot.transform.parent = envRoot.transform;
            float[] pillarZ = { -7.5f, -2.5f, 2.5f, 7.5f };
            for (int p = 0; p < pillarZ.Length; p++)
            {
                BuildCarvedPillar($"Pillar_Left_{p + 1}", pillarsRoot.transform, new Vector3(-4.2f, 0, pillarZ[p]), pillarMat, brassMat);
                BuildCarvedPillar($"Pillar_Right_{p + 1}", pillarsRoot.transform, new Vector3(4.2f, 0, pillarZ[p]), pillarMat, brassMat);
            }

            // Hanging Brass Temple Bells
            GameObject bellsRoot = new GameObject("Hanging_BrassBells");
            bellsRoot.transform.parent = envRoot.transform;
            BuildHangingBell("CeremonialBell_Center", bellsRoot.transform, new Vector3(0, 3.8f, -2.5f), brassMat, 1.0f);
            BuildHangingBell("AisleBell_Left", bellsRoot.transform, new Vector3(-4.2f, 4.2f, 2.5f), brassMat, 0.7f);
            BuildHangingBell("AisleBell_Right", bellsRoot.transform, new Vector3(4.2f, 4.2f, 2.5f), brassMat, 0.7f);

            // Silk Ceremonial Curtains
            GameObject curtainsRoot = new GameObject("Sanctum_SilkCurtains");
            curtainsRoot.transform.parent = envRoot.transform;
            CreateBox("BackCurtain_Red", curtainsRoot.transform, new Vector3(0, 4.2f, -12.3f), new Vector3(8.0f, 6.5f, 0.08f), redSilkMat, false);
            CreateBox("SideDrape_Left", curtainsRoot.transform, new Vector3(-4.2f, 4.0f, -10.0f), new Vector3(0.08f, 6.0f, 3.5f), saffronMat, false);
            CreateBox("SideDrape_Right", curtainsRoot.transform, new Vector3(4.2f, 4.0f, -10.0f), new Vector3(0.08f, 6.0f, 3.5f), saffronMat, false);

            // Marigold Garlands
            GameObject garlandsRoot = new GameObject("Marigold_Garlands");
            garlandsRoot.transform.parent = envRoot.transform;
            // Garland draped around left pillar
            CreateCylinder("Garland_Pillar1", garlandsRoot.transform, new Vector3(-4.2f, 2.5f, -7.5f), new Vector3(0.95f, 0.12f, 0.95f), flowerMat, false);
            CreateCylinder("Garland_Pillar2", garlandsRoot.transform, new Vector3(4.2f, 2.5f, -7.5f), new Vector3(0.95f, 0.12f, 0.95f), flowerMat, false);
            // Discarded torn garland on floor near altar
            CreateBox("Garland_FallenPiece1", garlandsRoot.transform, new Vector3(1.1f, 0.05f, -5.8f), new Vector3(1.2f, 0.08f, 0.25f), flowerMat, false);
            CreateBox("Garland_FallenPiece2", garlandsRoot.transform, new Vector3(1.4f, 0.05f, -5.5f), new Vector3(0.6f, 0.08f, 0.45f), flowerMat, false);

            // Central Altar Platform (Vedi) & EMPTY Idol Pedestal
            GameObject altarRoot = new GameObject("CentralAltar_Sanctum");
            altarRoot.transform.parent = envRoot.transform;

            // Raised platform
            CreateBox("Altar_Plinth", altarRoot.transform, new Vector3(0, 0.35f, -7.5f), new Vector3(5.2f, 0.7f, 5.2f), stoneMat, true);
            CreateBox("Altar_Step", altarRoot.transform, new Vector3(0, 0.18f, -4.6f), new Vector3(3.2f, 0.35f, 0.6f), stoneMat, true);

            // Empty Idol Pedestal (Violation Scene)
            GameObject pedestal = new GameObject("Empty_IdolPedestal");
            pedestal.transform.parent = altarRoot.transform;
            pedestal.transform.localPosition = new Vector3(0, 0.7f, -7.5f);

            CreateBox("PedestalBase", pedestal.transform, new Vector3(0, 0.25f, 0), new Vector3(1.8f, 0.5f, 1.5f), darkWoodMat, true);
            CreateBox("PedestalTopRim", pedestal.transform, new Vector3(0, 0.55f, 0), new Vector3(1.6f, 0.1f, 1.3f), brassMat, true);
            // Empty hollow depression where the idol was ripped off
            CreateBox("StolenIdol_EmptySocket", pedestal.transform, new Vector3(0, 0.62f, 0), new Vector3(0.85f, 0.04f, 0.65f), wetFloorMat, false);
            // Fallen sacred brass offering bowl
            CreateSphere("OfferingBowl_Overturned", pedestal.transform, new Vector3(0.65f, 0.58f, 0.3f), new Vector3(0.28f, 0.12f, 0.28f), brassMat, false);

            // 4 Flickering Warm Oil Lamps (Max real-time lights <= 8)
            GameObject lampsRoot = new GameObject("Sanctum_OilLamps");
            lampsRoot.transform.parent = envRoot.transform;
            CreateOilLamp(lampsRoot.transform, new Vector3(-2.2f, 0.7f, -7.5f), "AltarLamp_Left");
            CreateOilLamp(lampsRoot.transform, new Vector3(2.2f, 0.7f, -7.5f), "AltarLamp_Right");
            CreateOilLamp(lampsRoot.transform, new Vector3(-4.2f, 0f, 0.0f), "AisleLamp_Left");
            CreateOilLamp(lampsRoot.transform, new Vector3(4.2f, 0f, 0.0f), "AisleLamp_Right");

            // 4. Spawns
            GameObject spawnRoot = new GameObject("--- SPAWN POINTS ---");
            CreateSpawnPoint(spawnRoot.transform, "player_start", new Vector3(0f, 0f, 11f), new Vector3(0, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "priest_spawn", new Vector3(2.5f, 0f, -1.0f), new Vector3(-1, 0, 0));
            CreateSpawnPoint(spawnRoot.transform, "robber_spawn", new Vector3(-4.0f, 0f, -6.0f), new Vector3(1, 0, 0.5f));
            CreateSpawnPoint(spawnRoot.transform, "villager_spawn", new Vector3(-3.0f, 0f, 8.0f), new Vector3(1, 0, -0.5f));

            // 5. Interactables
            GameObject interactRoot = new GameObject("--- INTERACTABLES ---");
            CreateInteractable(interactRoot.transform, "sanctum_empty_altar", "Examine empty idol pedestal", new Vector3(0f, 1.0f, -7.5f), 2.5f);
            CreateInteractable(interactRoot.transform, "sanctum_hanging_bell", "Ring temple bell", new Vector3(0f, 3.2f, -2.5f), 2.2f);
            CreateInteractable(interactRoot.transform, "sanctum_fallen_garland", "Inspect discarded marigold garland", new Vector3(1.2f, 0.2f, -5.6f), 2.0f);

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] Sanctum scene generated successfully at '{scenePath}'.");
            UpdateBuildSettings();
        }

        private static void BuildCarvedPillar(string name, Transform parent, Vector3 localPos, Material pillarMat, Material accentMat)
        {
            GameObject pillarObj = new GameObject(name);
            pillarObj.transform.parent = parent;
            pillarObj.transform.localPosition = localPos;

            // Plinth Base
            CreateBox("Plinth_Lower", pillarObj.transform, new Vector3(0, 0.3f, 0), new Vector3(1.3f, 0.6f, 1.3f), pillarMat, true);
            CreateBox("Plinth_Upper", pillarObj.transform, new Vector3(0, 0.7f, 0), new Vector3(1.1f, 0.2f, 1.1f), accentMat, true);

            // Carved Shaft
            CreateCylinder("Shaft_Main", pillarObj.transform, new Vector3(0, 4.0f, 0), new Vector3(0.85f, 3.1f, 0.85f), pillarMat, true);

            // Decorative Mid-Rings
            CreateCylinder("Ring_Mid", pillarObj.transform, new Vector3(0, 3.8f, 0), new Vector3(0.98f, 0.15f, 0.98f), accentMat, false);
            CreateCylinder("Ring_Upper", pillarObj.transform, new Vector3(0, 6.2f, 0), new Vector3(0.98f, 0.15f, 0.98f), accentMat, false);

            // Capital Bracket
            CreateBox("Capital_Lower", pillarObj.transform, new Vector3(0, 7.3f, 0), new Vector3(1.2f, 0.3f, 1.2f), accentMat, true);
            CreateBox("Capital_Bracket", pillarObj.transform, new Vector3(0, 7.7f, 0), new Vector3(1.5f, 0.5f, 1.5f), pillarMat, true);
        }

        private static void BuildHangingBell(string name, Transform parent, Vector3 localPos, Material brassMat, float scale = 1.0f)
        {
            GameObject bellRoot = new GameObject(name);
            bellRoot.transform.parent = parent;
            bellRoot.transform.localPosition = localPos;

            // Suspension Chain
            CreateCylinder("Chain", bellRoot.transform, new Vector3(0, 1.5f * scale, 0), new Vector3(0.06f * scale, 1.5f * scale, 0.06f * scale), brassMat, false);

            // Bell Body
            CreateCylinder("BellCrown", bellRoot.transform, new Vector3(0, 0.3f * scale, 0), new Vector3(0.5f * scale, 0.25f * scale, 0.5f * scale), brassMat, true);
            CreateCylinder("BellFlange", bellRoot.transform, new Vector3(0, -0.05f * scale, 0), new Vector3(0.8f * scale, 0.15f * scale, 0.8f * scale), brassMat, true);

            // Clapper
            CreateSphere("Clapper", bellRoot.transform, new Vector3(0, -0.2f * scale, 0), new Vector3(0.2f * scale, 0.2f * scale, 0.2f * scale), brassMat, false);
        }

        [MenuItem("Tools/Legacy/Environments/Build PuzzleWall")]
        public static void BuildPuzzleWall()
        {
            EnsureDirectoryExists(ScenesBasePath);
            string scenePath = $"{ScenesBasePath}/PuzzleWall.unity";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. System & Atmosphere
            SetupStandardCameraAndManager(scene, new Vector3(0f, 2.8f, 6.5f), new Vector3(6f, 180f, 0f), Constants.PuzzleWall);

            GameObject lightRoot = new GameObject("--- LIGHTING & ATMOSPHERE ---");

            // Spotlight illuminating the central carved puzzle wall
            GameObject spotObj = new GameObject("Spotlight_PuzzleWallFocus");
            spotObj.transform.parent = lightRoot.transform;
            spotObj.transform.position = new Vector3(0f, 5.5f, 2.5f);
            spotObj.transform.rotation = Quaternion.Euler(35f, 180f, 0f);
            Light spotLight = spotObj.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = new Color(1.0f, 0.88f, 0.62f);
            spotLight.intensity = 3.2f;
            spotLight.range = 10.5f;
            spotLight.spotAngle = 55f;
            spotLight.shadows = LightShadows.Soft;

            // Directional fill
            GameObject fillObj = new GameObject("Directional_ChamberFill");
            fillObj.transform.parent = lightRoot.transform;
            fillObj.transform.rotation = Quaternion.Euler(55f, 30f, 0f);
            Light fillLight = fillObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.25f, 0.32f, 0.45f);
            fillLight.intensity = 0.25f;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.10f, 0.14f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.015f;
            RenderSettings.fogColor = new Color(0.14f, 0.11f, 0.13f);

            // 2. Materials
            Material stoneMat = GetMaterial("mat_StoneTemple");
            Material wetFloorMat = GetMaterial("mat_StoneFloorWet");
            Material pillarMat = GetMaterial("mat_CarvedPillar");
            Material reliefMat = GetMaterial("mat_LimestoneRelief");
            Material brassMat = GetMaterial("mat_GoldBrass");
            Material darkWoodMat = GetMaterial("mat_DarkWood");
            Material saffronMat = GetMaterial("mat_SaffronCloth");

            // 3. Environment Hierarchy
            GameObject envRoot = new GameObject("--- ENVIRONMENT ---");

            // Chamber Floor (16m x 12m)
            CreateBox("PuzzleChamber_Floor", envRoot.transform, new Vector3(0, -0.25f, 3.5f), new Vector3(16f, 0.5f, 12f), wetFloorMat, true);

            // Perimeter Walls & Ceiling
            GameObject wallsRoot = new GameObject("PuzzleChamber_Walls");
            wallsRoot.transform.parent = envRoot.transform;
            CreateBox("Wall_East", wallsRoot.transform, new Vector3(8f, 3.5f, 3.5f), new Vector3(1.2f, 7.0f, 12f), stoneMat, true);
            CreateBox("Wall_West", wallsRoot.transform, new Vector3(-8f, 3.5f, 3.5f), new Vector3(1.2f, 7.0f, 12f), stoneMat, true);
            CreateBox("Wall_South", wallsRoot.transform, new Vector3(0, 3.5f, 9.5f), new Vector3(16f, 7.0f, 1.2f), stoneMat, true);
            CreateBox("Chamber_Ceiling", envRoot.transform, new Vector3(0, 7.25f, 3.5f), new Vector3(16.5f, 0.5f, 12.5f), stoneMat, true);

            // Massive Carved Puzzle Wall (North Wall at z = -2.5f)
            GameObject pWallRoot = new GameObject("Carved_PuzzleWallStructure");
            pWallRoot.transform.parent = envRoot.transform;

            // Main wall body
            CreateBox("PuzzleWall_MainBacking", pWallRoot.transform, new Vector3(0, 3.5f, -2.5f), new Vector3(16f, 7.0f, 1.5f), stoneMat, true);

            // Ornamental framing pillars and lintel
            CreateBox("FramePillar_Left", pWallRoot.transform, new Vector3(-5.2f, 3.5f, -1.6f), new Vector3(1.2f, 7.0f, 0.6f), pillarMat, true);
            CreateBox("FramePillar_Right", pWallRoot.transform, new Vector3(5.2f, 3.5f, -1.6f), new Vector3(1.2f, 7.0f, 0.6f), pillarMat, true);
            CreateBox("FrameLintel_Top", pWallRoot.transform, new Vector3(0, 6.6f, -1.6f), new Vector3(11.6f, 0.8f, 0.6f), pillarMat, true);

            // Wall Carved Relief Ledge
            GameObject ledgeObj = CreateBox("PuzzleWall_LedgeShelf", pWallRoot.transform, new Vector3(0, 0.85f, -1.25f), new Vector3(7.2f, 0.7f, 1.2f), stoneMat, true);

            // Saffron tapestries flanking the puzzle wall
            CreateBox("WallBanner_Left", pWallRoot.transform, new Vector3(-6.2f, 3.8f, -1.65f), new Vector3(1.0f, 4.2f, 0.05f), saffronMat, false);
            CreateBox("WallBanner_Right", pWallRoot.transform, new Vector3(6.2f, 3.8f, -1.65f), new Vector3(1.0f, 4.2f, 0.05f), saffronMat, false);

            // 4. Central Rotating Lotus / Chakra Wheel (wheel_center)
            BuildPuzzleChakraWheel(pWallRoot.transform, new Vector3(0, 3.6f, -1.6f), brassMat, reliefMat);

            // 5. Four Carved Relief Panels: Lotus, Conch, Wheel, Kalash
            BuildReliefPanel(pWallRoot.transform, "panel_lotus", "Inspect Lotus Relief Panel", new Vector3(-2.8f, 4.7f, -1.65f), "lotus", stoneMat, reliefMat);
            BuildReliefPanel(pWallRoot.transform, "panel_conch", "Inspect Conch Relief Panel", new Vector3(2.8f, 4.7f, -1.65f), "conch", stoneMat, reliefMat);
            BuildReliefPanel(pWallRoot.transform, "panel_wheel", "Inspect Wheel Relief Panel", new Vector3(-2.8f, 2.3f, -1.65f), "wheel", stoneMat, reliefMat);
            BuildReliefPanel(pWallRoot.transform, "panel_kalash", "Inspect Kalash Relief Panel", new Vector3(2.8f, 2.3f, -1.65f), "kalash", stoneMat, reliefMat);

            // 6. Ancient Scroll on Ledge
            BuildScrollOnLedge(pWallRoot.transform, new Vector3(0, 1.25f, -1.15f), darkWoodMat, reliefMat);

            // 7. Flanking Braziers (2 Flickering Warm Lights)
            GameObject lampsRoot = new GameObject("PuzzleChamber_Braziers");
            lampsRoot.transform.parent = envRoot.transform;
            CreateOilLamp(lampsRoot.transform, new Vector3(-4.8f, 0, 0.5f), "Brazier_Left");
            CreateOilLamp(lampsRoot.transform, new Vector3(4.8f, 0, 0.5f), "Brazier_Right");

            // 8. Spawns
            GameObject spawnRoot = new GameObject("--- SPAWN POINTS ---");
            CreateSpawnPoint(spawnRoot.transform, "player_start", new Vector3(0f, 0f, 5.0f), new Vector3(0, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "priest_spawn", new Vector3(-3.5f, 0f, 4.0f), new Vector3(0.5f, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "robber_spawn", new Vector3(3.5f, 0f, 2.0f), new Vector3(-0.5f, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "villager_spawn", new Vector3(0f, 0f, 8.0f), new Vector3(0, 0, -1));

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] PuzzleWall scene generated successfully at '{scenePath}'.");
            UpdateBuildSettings();
        }

        private static void BuildPuzzleChakraWheel(Transform parent, Vector3 localPos, Material brassMat, Material accentMat)
        {
            GameObject wheelObj = new GameObject("ChakraWheel_Structure");
            wheelObj.transform.parent = parent;
            wheelObj.transform.localPosition = localPos;

            // Outer carved rim disc
            GameObject rim = CreateCylinder("OuterRim", wheelObj.transform, Vector3.zero, new Vector3(2.5f, 0.15f, 2.5f), brassMat, true);
            rim.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            // Inner center boss hub
            GameObject hub = CreateCylinder("CenterHub", wheelObj.transform, new Vector3(0, 0, -0.05f), new Vector3(0.85f, 0.22f, 0.85f), accentMat, true);
            hub.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            // 8 radiating lotus/chakra spokes
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                GameObject spoke = CreateBox($"Spoke_{i + 1}", wheelObj.transform, Vector3.zero, new Vector3(0.14f, 2.2f, 0.12f), brassMat, false);
                spoke.transform.localRotation = Quaternion.Euler(0, 0, angle);
            }

            // Central Sacred Gem / Boss
            GameObject gem = CreateSphere("CenterGem", wheelObj.transform, new Vector3(0, 0, -0.15f), new Vector3(0.4f, 0.4f, 0.3f), GetMaterial("mat_Flame"), false);

            // Interactable component for wheel_center
            InteractableObject io = wheelObj.AddComponent<InteractableObject>();
            io.Setup("wheel_center", "Turn Sacred Chakra Wheel", 2.5f);

            BoxCollider col = wheelObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2.8f, 2.8f, 1.2f);
        }

        private static void BuildReliefPanel(Transform parent, string id, string prompt, Vector3 localPos, string motifType, Material baseMat, Material reliefMat)
        {
            GameObject panelRoot = new GameObject($"ReliefPanel_{id}");
            panelRoot.transform.parent = parent;
            panelRoot.transform.localPosition = localPos;

            // Carved stone panel tablet base
            CreateBox("PanelTablet", panelRoot.transform, Vector3.zero, new Vector3(1.7f, 1.7f, 0.18f), baseMat, true);
            CreateBox("BorderMoulding", panelRoot.transform, new Vector3(0, 0, -0.05f), new Vector3(1.85f, 1.85f, 0.08f), reliefMat, false);

            // Carved Motif in Center
            GameObject motifObj = new GameObject($"Motif_{motifType}");
            motifObj.transform.parent = panelRoot.transform;
            motifObj.transform.localPosition = new Vector3(0, 0, -0.12f);

            switch (motifType)
            {
                case "lotus":
                    // Carved Lotus Petals
                    for (int p = 0; p < 6; p++)
                    {
                        float a = p * 60f;
                        GameObject petal = CreateSphere($"Petal_{p}", motifObj.transform, Vector3.zero, new Vector3(0.35f, 0.7f, 0.12f), reliefMat, false);
                        petal.transform.localRotation = Quaternion.Euler(0, 0, a);
                    }
                    CreateSphere("LotusCenter", motifObj.transform, Vector3.zero, new Vector3(0.35f, 0.35f, 0.18f), GetMaterial("mat_GoldBrass"), false);
                    break;

                case "conch":
                    // Carved Conch (Shankha) spiral
                    CreateSphere("ConchBody", motifObj.transform, new Vector3(-0.1f, -0.1f, 0), new Vector3(0.7f, 0.95f, 0.3f), reliefMat, false);
                    CreateCylinder("ConchTip", motifObj.transform, new Vector3(0.2f, 0.35f, 0), new Vector3(0.25f, 0.45f, 0.25f), reliefMat, false);
                    break;

                case "wheel":
                    // Sudarshana Sun-Wheel motif
                    CreateCylinder("WheelRim", motifObj.transform, Vector3.zero, new Vector3(0.9f, 0.08f, 0.9f), reliefMat, false).transform.localRotation = Quaternion.Euler(90f, 0, 0);
                    CreateSphere("WheelHub", motifObj.transform, Vector3.zero, new Vector3(0.35f, 0.35f, 0.15f), GetMaterial("mat_GoldBrass"), false);
                    break;

                case "kalash":
                    // Sacred Kalasha Urn with leaves
                    CreateSphere("KalashPot", motifObj.transform, new Vector3(0, -0.15f, 0), new Vector3(0.75f, 0.7f, 0.45f), reliefMat, false);
                    CreateCylinder("KalashNeck", motifObj.transform, new Vector3(0, 0.25f, 0), new Vector3(0.45f, 0.18f, 0.35f), reliefMat, false);
                    CreateSphere("CoconutTop", motifObj.transform, new Vector3(0, 0.45f, 0), new Vector3(0.35f, 0.4f, 0.35f), GetMaterial("mat_GoldBrass"), false);
                    break;
            }

            // Interactable Setup
            InteractableObject io = panelRoot.AddComponent<InteractableObject>();
            io.Setup(id, prompt, 2.2f);

            BoxCollider trigger = panelRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.0f, 2.0f, 1.2f);
        }

        private static void BuildScrollOnLedge(Transform parent, Vector3 localPos, Material woodMat, Material parchmentMat)
        {
            GameObject scrollRoot = new GameObject("Scroll_AncientTempleClue");
            scrollRoot.transform.parent = parent;
            scrollRoot.transform.localPosition = localPos;

            // Rolled wooden handles
            CreateCylinder("HandleLeft", scrollRoot.transform, new Vector3(-0.6f, 0, 0), new Vector3(0.08f, 0.35f, 0.08f), woodMat, true).transform.localRotation = Quaternion.Euler(0, 0, 90f);
            CreateCylinder("HandleRight", scrollRoot.transform, new Vector3(0.6f, 0, 0), new Vector3(0.08f, 0.35f, 0.08f), woodMat, true).transform.localRotation = Quaternion.Euler(0, 0, 90f);

            // Unrolled parchment sheet
            GameObject sheet = CreateBox("ParchmentSheet", scrollRoot.transform, new Vector3(0, 0.02f, 0), new Vector3(1.1f, 0.02f, 0.65f), parchmentMat, true);
            sheet.transform.localRotation = Quaternion.Euler(-12f, 0, 0);

            // Clue inscriptions simulation
            CreateBox("InscribedLine1", scrollRoot.transform, new Vector3(0, 0.04f, 0.12f), new Vector3(0.85f, 0.01f, 0.06f), GetMaterial("mat_DarkWood"), false);
            CreateBox("InscribedLine2", scrollRoot.transform, new Vector3(0, 0.04f, -0.02f), new Vector3(0.80f, 0.01f, 0.06f), GetMaterial("mat_DarkWood"), false);
            CreateBox("InscribedLine3", scrollRoot.transform, new Vector3(0, 0.04f, -0.15f), new Vector3(0.70f, 0.01f, 0.06f), GetMaterial("mat_DarkWood"), false);

            InteractableObject io = scrollRoot.AddComponent<InteractableObject>();
            io.Setup("scroll", "Read ancient temple scroll", 2.0f);

            BoxCollider col = scrollRoot.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.5f, 0.8f, 1.2f);
        }

        [MenuItem("Tools/Legacy/Environments/Build ShrineCrime")]
        public static void BuildShrineCrime()
        {
            EnsureDirectoryExists(ScenesBasePath);
            string scenePath = $"{ScenesBasePath}/ShrineCrime.unity";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. System & Atmosphere
            SetupStandardCameraAndManager(scene, new Vector3(0f, 2.8f, 9.5f), new Vector3(6f, 180f, 0f), Constants.ShrineCrime);

            GameObject lightRoot = new GameObject("--- LIGHTING & ATMOSPHERE ---");

            // Cool moonlight streaming through the shattered wooden doorway
            GameObject moonObj = new GameObject("Directional_MoonlightBrokenDoor");
            moonObj.transform.parent = lightRoot.transform;
            moonObj.transform.rotation = Quaternion.Euler(30f, 160f, 0f);
            Light moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.32f, 0.52f, 0.85f);
            moonLight.intensity = 0.85f;
            moonLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.08f, 0.12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.022f;
            RenderSettings.fogColor = new Color(0.08f, 0.10f, 0.16f);

            // 2. Materials
            Material stoneMat = GetMaterial("mat_StoneTemple");
            Material wetFloorMat = GetMaterial("mat_StoneFloorWet");
            Material pillarMat = GetMaterial("mat_CarvedPillar");
            Material brassMat = GetMaterial("mat_GoldBrass");
            Material darkWoodMat = GetMaterial("mat_DarkWood");
            Material redClothMat = GetMaterial("mat_DarkRedCloth");
            Material flowerMat = GetMaterial("mat_MarigoldFlowers");
            Material footprintMat = GetMaterial("mat_CrimeFootprint");

            // 3. Environment Hierarchy
            GameObject envRoot = new GameObject("--- ENVIRONMENT ---");

            // Ransacked Shrine Floor (16m x 18m)
            CreateBox("Shrine_Floor", envRoot.transform, new Vector3(0, -0.25f, 0), new Vector3(16f, 0.5f, 18f), wetFloorMat, true);

            // Perimeter Stone Walls
            GameObject wallsRoot = new GameObject("Shrine_Walls");
            wallsRoot.transform.parent = envRoot.transform;
            CreateBox("Wall_North", wallsRoot.transform, new Vector3(0, 3.25f, -9f), new Vector3(16f, 6.5f, 1.2f), stoneMat, true);
            CreateBox("Wall_East", wallsRoot.transform, new Vector3(8f, 3.25f, 0), new Vector3(1.2f, 6.5f, 18f), stoneMat, true);
            CreateBox("Wall_West", wallsRoot.transform, new Vector3(-8f, 3.25f, 0), new Vector3(1.2f, 6.5f, 18f), stoneMat, true);

            // Broken South Wall with Smashed Wooden Doors
            BuildShatteredDoor(wallsRoot.transform, new Vector3(0, 0, 9f), stoneMat, darkWoodMat);

            // Ceiling with Moonlight Gap
            CreateBox("Ceiling_Left", envRoot.transform, new Vector3(-4.5f, 6.75f, 0), new Vector3(7.5f, 0.5f, 18f), stoneMat, true);
            CreateBox("Ceiling_Right", envRoot.transform, new Vector3(4.5f, 6.75f, 0), new Vector3(7.5f, 0.5f, 18f), stoneMat, true);

            // Violated Deity Niche & Overturned Offering Table
            GameObject shrineNiche = new GameObject("Violated_DeityNiche");
            shrineNiche.transform.parent = envRoot.transform;
            CreateBox("NicheArch_Left", shrineNiche.transform, new Vector3(-1.8f, 2.5f, -8.3f), new Vector3(0.6f, 4.0f, 0.6f), stoneMat, true);
            CreateBox("NicheArch_Right", shrineNiche.transform, new Vector3(1.8f, 2.5f, -8.3f), new Vector3(0.6f, 4.0f, 0.6f), stoneMat, true);
            CreateBox("NicheArch_Top", shrineNiche.transform, new Vector3(0, 4.6f, -8.3f), new Vector3(4.2f, 0.6f, 0.6f), stoneMat, true);

            // Smashed offering pedestal
            GameObject brokenPlinth = CreateBox("Smashed_Plinth", shrineNiche.transform, new Vector3(0.3f, 0.4f, -7.2f), new Vector3(2.2f, 0.7f, 1.4f), stoneMat, true);
            brokenPlinth.transform.localRotation = Quaternion.Euler(0, 18f, 0);

            // 4. Overturned Brass Lamp (broken_lamp) with Flickering Light
            BuildOverturnedLamp(envRoot.transform, new Vector3(-2.2f, 0.15f, -2.5f), brassMat);

            // Wall Torch for contrast (warm flicking light)
            CreateTorch(envRoot.transform, new Vector3(7.3f, 2.8f, 2.0f), new Vector3(0, -90f, 0), "SmokingWallTorch");

            // 5. Torn Red Robber Cloth (red_cloth)
            BuildTornRedCloth(envRoot.transform, new Vector3(-7.3f, 1.8f, 1.2f), redClothMat, stoneMat);

            // 6. Muddy Footprints Trail (footprints)
            BuildFootprintsTrail(envRoot.transform, footprintMat);

            // 7. Scattered Marigold Petals and Debris
            GameObject debrisRoot = new GameObject("Scattered_DebrisAndPetals");
            debrisRoot.transform.parent = envRoot.transform;
            for (int i = 0; i < 12; i++)
            {
                float rx = UnityEngine.Random.Range(-3.0f, 3.0f);
                float rz = UnityEngine.Random.Range(-6.0f, 6.0f);
                float rRot = UnityEngine.Random.Range(0f, 360f);
                GameObject petal = CreateSphere($"PetalCluster_{i}", debrisRoot.transform, new Vector3(rx, 0.03f, rz), new Vector3(0.35f, 0.04f, 0.35f), flowerMat, false);
                petal.transform.localRotation = Quaternion.Euler(0, rRot, 0);
            }

            // 8. Spawns
            GameObject spawnRoot = new GameObject("--- SPAWN POINTS ---");
            CreateSpawnPoint(spawnRoot.transform, "player_start", new Vector3(0f, 0f, 8.0f), new Vector3(0, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "villager_spawn", new Vector3(3.2f, 0f, 9.5f), new Vector3(-0.5f, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "priest_spawn", new Vector3(3.0f, 0f, 0f), new Vector3(-1, 0, 0));
            CreateSpawnPoint(spawnRoot.transform, "robber_spawn", new Vector3(-4.0f, 0f, -8.0f), new Vector3(0.5f, 0, 1));

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] ShrineCrime scene generated successfully at '{scenePath}'.");
            UpdateBuildSettings();
        }

        private static void BuildShatteredDoor(Transform parent, Vector3 localPos, Material stoneMat, Material woodMat)
        {
            GameObject doorRoot = new GameObject("Shattered_WoodenDoorPortal");
            doorRoot.transform.parent = parent;
            doorRoot.transform.localPosition = localPos;

            // Stone portal jambs
            CreateBox("DoorJamb_Left", doorRoot.transform, new Vector3(-2.4f, 3.25f, 0), new Vector3(1.2f, 6.5f, 1.2f), stoneMat, true);
            CreateBox("DoorJamb_Right", doorRoot.transform, new Vector3(2.4f, 3.25f, 0), new Vector3(1.2f, 6.5f, 1.2f), stoneMat, true);
            CreateBox("DoorLintel_Top", doorRoot.transform, new Vector3(0, 5.75f, 0), new Vector3(3.6f, 1.5f, 1.2f), stoneMat, true);

            // Left broken door leaf (hanging loose)
            GameObject leftDoor = CreateBox("SmashedDoor_Left", doorRoot.transform, new Vector3(-1.4f, 2.2f, -0.4f), new Vector3(1.4f, 4.4f, 0.12f), woodMat, true);
            leftDoor.transform.localRotation = Quaternion.Euler(0, 65f, 12f);

            // Right door leaf (shattered outward)
            GameObject rightDoor = CreateBox("SmashedDoor_Right", doorRoot.transform, new Vector3(1.6f, 2.0f, 0.6f), new Vector3(1.4f, 4.0f, 0.12f), woodMat, true);
            rightDoor.transform.localRotation = Quaternion.Euler(-15f, -75f, 0);

            // Broken splinter planks on the ground
            GameObject plank1 = CreateBox("BrokenPlank_1", doorRoot.transform, new Vector3(0.4f, 0.05f, 0.8f), new Vector3(0.25f, 0.06f, 1.4f), woodMat, false);
            plank1.transform.localRotation = Quaternion.Euler(0, 35f, 0);
            GameObject plank2 = CreateBox("BrokenPlank_2", doorRoot.transform, new Vector3(-0.6f, 0.05f, 1.2f), new Vector3(0.2f, 0.06f, 1.1f), woodMat, false);
            plank2.transform.localRotation = Quaternion.Euler(0, -45f, 0);
        }

        private static void BuildOverturnedLamp(Transform parent, Vector3 localPos, Material brassMat)
        {
            GameObject lampRoot = new GameObject("BrokenOverturned_Lamp");
            lampRoot.transform.parent = parent;
            lampRoot.transform.localPosition = localPos;
            lampRoot.transform.localRotation = Quaternion.Euler(75f, 30f, 0);

            // Overturned stand
            CreateCylinder("LampStand", lampRoot.transform, Vector3.zero, new Vector3(0.16f, 0.8f, 0.16f), brassMat, true);
            CreateCylinder("BasePedestal", lampRoot.transform, new Vector3(0, -0.7f, 0), new Vector3(0.55f, 0.12f, 0.55f), brassMat, true);
            CreateCylinder("OilBowl", lampRoot.transform, new Vector3(0, 0.7f, 0), new Vector3(0.65f, 0.1f, 0.65f), brassMat, false);

            // Dark spilled oil puddle on the floor
            GameObject oilPuddle = CreateSphere("SpilledOilPuddle", parent, localPos + new Vector3(0.3f, 0.02f, 0), new Vector3(1.4f, 0.02f, 1.8f), GetMaterial("mat_StoneFloorWet"), false);

            // Embers and flickering light
            GameObject flame = CreateSphere("LampEmber", lampRoot.transform, new Vector3(0, 0.82f, 0), new Vector3(0.2f, 0.2f, 0.2f), GetMaterial("mat_Flame"), false);

            GameObject lightObj = new GameObject("BrokenLampLight");
            lightObj.transform.parent = flame.transform;
            lightObj.transform.localPosition = Vector3.zero;

            Light lt = lightObj.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.color = new Color(1.0f, 0.52f, 0.14f);
            lt.intensity = 1.8f;
            lt.range = 7.0f;
            lt.shadows = LightShadows.Soft;

            LightFlicker flicker = lightObj.AddComponent<LightFlicker>();
            flicker.Configure(1.1f, 2.0f, 16f);

            // Interactable
            InteractableObject io = lampRoot.AddComponent<InteractableObject>();
            io.Setup("broken_lamp", "Inspect overturned brass lamp", 2.2f);

            BoxCollider col = lampRoot.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.8f, 1.2f, 1.8f);
        }

        private static void BuildTornRedCloth(Transform parent, Vector3 localPos, Material redClothMat, Material stoneMat)
        {
            GameObject clothRoot = new GameObject("TornRobber_RedCloth");
            clothRoot.transform.parent = parent;
            clothRoot.transform.localPosition = localPos;

            // Jagged stone sconce where cloth got snagged
            CreateBox("StoneSconceHook", clothRoot.transform, Vector3.zero, new Vector3(0.25f, 0.35f, 0.45f), stoneMat, true);

            // Torn cloth shred
            GameObject clothMesh = CreateBox("ClothTorn_Shred", clothRoot.transform, new Vector3(0.15f, -0.4f, 0.2f), new Vector3(0.06f, 0.75f, 0.45f), redClothMat, false);
            clothMesh.transform.localRotation = Quaternion.Euler(15f, 20f, -10f);

            InteractableObject io = clothRoot.AddComponent<InteractableObject>();
            io.Setup("red_cloth", "Examine torn red robber cloth", 2.2f);

            BoxCollider col = clothRoot.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.6f, 1.6f, 1.6f);
        }

        private static void BuildFootprintsTrail(Transform parent, Material footprintMat)
        {
            GameObject trailRoot = new GameObject("Muddy_FootprintsTrail");
            trailRoot.transform.parent = parent;

            // Series of footprints leading toward the broken door
            Vector3[] printPositions = {
                new Vector3(0.2f, 0.02f, -5.5f),
                new Vector3(-0.1f, 0.02f, -4.2f),
                new Vector3(0.3f, 0.02f, -2.8f),
                new Vector3(-0.05f, 0.02f, -1.2f),
                new Vector3(0.4f, 0.02f, 0.5f),
                new Vector3(0.1f, 0.02f, 2.2f),
                new Vector3(0.5f, 0.02f, 4.0f),
                new Vector3(0.2f, 0.02f, 5.8f),
                new Vector3(0.7f, 0.02f, 7.5f)
            };

            for (int i = 0; i < printPositions.Length; i++)
            {
                GameObject fp = CreateBox($"Footprint_{i + 1}", trailRoot.transform, printPositions[i], new Vector3(0.22f, 0.02f, 0.45f), footprintMat, false);
                fp.transform.localRotation = Quaternion.Euler(0, (i % 2 == 0 ? 12f : -8f), 0);
            }

            // Interactable trigger in the middle of the trail
            InteractableObject io = trailRoot.AddComponent<InteractableObject>();
            io.Setup("footprints", "Examine muddy footprints", 2.6f);

            BoxCollider col = trailRoot.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0.3f, 0.5f, 1.0f);
            col.size = new Vector3(2.5f, 1.5f, 8.0f);
        }

        [MenuItem("Tools/Legacy/Environments/Build JunglePath")]
        public static void BuildJunglePath()
        {
            EnsureDirectoryExists(ScenesBasePath);
            string scenePath = $"{ScenesBasePath}/JunglePath.unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupStandardCameraAndManager(scene, new Vector3(0f, 3.5f, 20.0f), new Vector3(10f, 180f, 0f), Constants.JunglePath);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] JunglePath stub saved at '{scenePath}'.");
            UpdateBuildSettings();
        }

        [MenuItem("Tools/Legacy/Build All Environments")]
        public static void BuildAllEnvironments()
        {
            BuildCourtyard();
            BuildSanctum();
            BuildPuzzleWall();
            BuildShrineCrime();
            BuildJunglePath();
            UpdateBuildSettings();
            Debug.Log("[EnvironmentBuilder] All 5 environments built and registered successfully!");
        }

        #endregion

        #region Batch Mode Entry Points

        public static void BuildCourtyardBatch()
        {
            BuildCourtyard();
        }

        public static void BuildSanctumBatch()
        {
            BuildSanctum();
        }

        public static void BuildPuzzleWallBatch()
        {
            BuildPuzzleWall();
        }

        public static void BuildShrineCrimeBatch()
        {
            BuildShrineCrime();
        }

        public static void BuildJunglePathBatch()
        {
            BuildJunglePath();
        }

        public static void BuildAllEnvironmentsBatch()
        {
            BuildAllEnvironments();
        }

        #endregion

        #region Utilities

        public static void UpdateBuildSettings()
        {
            string[] sceneFiles = {
                $"{ScenesBasePath}/Courtyard.unity",
                $"{ScenesBasePath}/Sanctum.unity",
                $"{ScenesBasePath}/PuzzleWall.unity",
                $"{ScenesBasePath}/ShrineCrime.unity",
                $"{ScenesBasePath}/JunglePath.unity"
            };

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
            foreach (string sf in sceneFiles)
            {
                if (File.Exists(sf))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(sf, true));
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
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
