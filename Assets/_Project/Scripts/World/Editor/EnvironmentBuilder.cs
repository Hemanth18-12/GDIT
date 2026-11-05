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
            SetupStandardCameraAndManager(scene, new Vector3(0f, 3.0f, 14.0f), new Vector3(8f, 180f, 0f), Constants.Sanctum);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] Sanctum stub saved at '{scenePath}'.");
            UpdateBuildSettings();
        }

        [MenuItem("Tools/Legacy/Environments/Build PuzzleWall")]
        public static void BuildPuzzleWall()
        {
            EnsureDirectoryExists(ScenesBasePath);
            string scenePath = $"{ScenesBasePath}/PuzzleWall.unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupStandardCameraAndManager(scene, new Vector3(0f, 2.5f, 7.0f), new Vector3(5f, 180f, 0f), Constants.PuzzleWall);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] PuzzleWall stub saved at '{scenePath}'.");
            UpdateBuildSettings();
        }

        [MenuItem("Tools/Legacy/Environments/Build ShrineCrime")]
        public static void BuildShrineCrime()
        {
            EnsureDirectoryExists(ScenesBasePath);
            string scenePath = $"{ScenesBasePath}/ShrineCrime.unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupStandardCameraAndManager(scene, new Vector3(0f, 2.5f, 9.0f), new Vector3(5f, 180f, 0f), Constants.ShrineCrime);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] ShrineCrime stub saved at '{scenePath}'.");
            UpdateBuildSettings();
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
