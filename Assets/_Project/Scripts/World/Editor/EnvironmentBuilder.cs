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
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;

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
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            bool isNew = false;
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = matName };
                isNew = true;
            }

            switch (matName)
            {
                case "mat_StoneTemple":
                    mat.SetColor("_BaseColor", new Color(0.725f, 0.651f, 0.549f)); // Authentic #B9A68C weathered Odisha sandstone
                    mat.SetFloat("_Smoothness", 0.22f);
                    mat.SetFloat("_Metallic", 0.02f);
                    AssignTexture(mat, "tex_WarmSandstoneCarved.png", "tex_WarmSandstone_Normal.png", new Vector2(4f, 4f));
                    break;

                case "mat_StoneFloorWet":
                    mat.SetColor("_BaseColor", new Color(0.58f, 0.55f, 0.50f)); // Warm sandy-grey stone plaza, NO blue tint
                    mat.SetFloat("_Smoothness", 0.32f); // Subtle wet sheen, NO mirror glare
                    mat.SetFloat("_Metallic", 0.0f); // Non-metallic natural stone
                    AssignTexture(mat, "tex_WetPlazaPaving.png", "tex_WetPlaza_Normal.png", new Vector2(10f, 10f));
                    break;

                case "mat_WarmTempleStone":
                    mat.SetColor("_BaseColor", new Color(0.76f, 0.68f, 0.58f)); // Warm golden Odisha sandstone
                    mat.SetFloat("_Smoothness", 0.22f);
                    mat.SetFloat("_Metallic", 0.02f);
                    AssignTexture(mat, "tex_WarmSandstoneCarved.png", "tex_WarmSandstone_Normal.png", new Vector2(4f, 4f));
                    break;

                case "mat_LightStone":
                    mat.SetColor("_BaseColor", new Color(0.86f, 0.79f, 0.70f)); // Carved light sandstone with deity reliefs
                    mat.SetFloat("_Smoothness", 0.25f);
                    mat.SetFloat("_Metallic", 0.02f);
                    AssignTexture(mat, "tex_CarvedDeityRelief.png", "tex_CarvedDeity_Normal.png", new Vector2(2f, 2f));
                    break;

                case "mat_DarkStone":
                    mat.SetColor("_BaseColor", new Color(0.55f, 0.49f, 0.42f)); // Weathered foundation stone (~40% lighter warm stone, not black)
                    mat.SetFloat("_Smoothness", 0.25f);
                    mat.SetFloat("_Metallic", 0.05f);
                    break;

                case "mat_SacredMetal":
                    mat.SetColor("_BaseColor", new Color(0.92f, 0.76f, 0.30f)); // Temple brass/gold finials
                    mat.SetFloat("_Smoothness", 0.85f);
                    mat.SetFloat("_Metallic", 0.88f);
                    break;

                case "mat_SanctumDarkness":
                    mat.SetColor("_BaseColor", new Color(0.09f, 0.08f, 0.08f)); // Deep sanctum interior
                    mat.SetFloat("_Smoothness", 0.10f);
                    mat.SetFloat("_Metallic", 0.0f);
                    break;

                case "mat_TempleRed":
                    mat.SetColor("_BaseColor", new Color(0.72f, 0.16f, 0.14f)); // Ritual vermilion red
                    mat.SetFloat("_Smoothness", 0.25f);
                    mat.SetFloat("_Metallic", 0.05f);
                    break;

                case "mat_TempleWood":
                    mat.SetColor("_BaseColor", new Color(0.30f, 0.18f, 0.10f)); // Seasoned teak doors
                    mat.SetFloat("_Smoothness", 0.35f);
                    mat.SetFloat("_Metallic", 0.0f);
                    break;

                case "mat_CourtyardStone":
                    mat.SetColor("_BaseColor", new Color(0.52f, 0.48f, 0.44f)); // Warm grey stone
                    mat.SetFloat("_Smoothness", 0.28f);
                    mat.SetFloat("_Metallic", 0.0f);
                    break;

                case "mat_InvestigationClue":
                    mat.SetColor("_BaseColor", new Color(0.95f, 0.85f, 0.45f));
                    mat.SetFloat("_Smoothness", 0.45f);
                    mat.SetFloat("_Metallic", 0.1f);
                    break;

                case "mat_CarvedPillar":
                    mat.SetColor("_BaseColor", new Color(0.65f, 0.56f, 0.48f));
                    mat.SetFloat("_Smoothness", 0.28f);
                    mat.SetFloat("_Metallic", 0.05f);
                    AssignTexture(mat, "tex_CarvedDeityRelief.png", "tex_CarvedDeity_Normal.png", new Vector2(1f, 3f));
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

            if (isNew)
            {
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void AssignTexture(Material mat, string baseTexName, string normalTexName, Vector2 tiling)
        {
            if (!string.IsNullOrEmpty(baseTexName))
            {
                Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Project/Models/Environment/Textures/{baseTexName}");
                if (baseTex != null)
                {
                    mat.SetTexture("_BaseMap", baseTex);
                    mat.SetTextureScale("_BaseMap", tiling);
                }
            }
            if (!string.IsNullOrEmpty(normalTexName))
            {
                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Project/Models/Environment/Textures/{normalTexName}");
                if (normalTex != null)
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.SetTextureScale("_BumpMap", tiling);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }
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

        public static GameObject CreatePortalTrigger(Transform parent, string name, Vector3 localPos, Vector3 size)
        {
            GameObject portal = new GameObject(name);
            portal.transform.parent = parent;
            portal.transform.localPosition = localPos;
            portal.transform.localRotation = Quaternion.identity;
            BoxCollider col = portal.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            col.center = Vector3.zero;
            return portal;
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

            var camData = (camObj.TryGetComponent<UniversalAdditionalCameraData>(out var __c) ? __c : camObj.AddComponent<UniversalAdditionalCameraData>());
            camData.renderPostProcessing = true;

            camObj.AddComponent<OrbitCamera>();

            // World Manager & Dev Tools
            GameObject wmObj = new GameObject("WorldManager");
            wmObj.transform.parent = sysRoot.transform;
            var worldManager = wmObj.AddComponent<WorldManager>();
            var worldSettings = new SerializedObject(worldManager);
            worldSettings.FindProperty("initialScene").stringValue = initialSceneName;
            worldSettings.ApplyModifiedPropertiesWithoutUndo();
            wmObj.AddComponent<WorldDevTools>();

            // Character Manager
            GameObject cmObj = new GameObject("CharacterManager");
            cmObj.transform.parent = sysRoot.transform;
            cmObj.AddComponent<CharacterManager>();

            // UI Manager
            GameObject uiObj = new GameObject("UIManager");
            uiObj.transform.parent = sysRoot.transform;
            uiObj.AddComponent<LegacyOfTemples.UI.UIManager>();
        }

        #endregion

        #region Courtyard Scene (Step 2)

        public static Material GetOrCreateSunriseSkybox()
        {
            string path = $"{MaterialsBasePath}/mat_SunriseSkybox.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = false;
            if (mat == null)
            {
                Shader skyShader = Shader.Find("Skybox/Procedural");
                if (skyShader != null)
                {
                    mat = new Material(skyShader) { name = "mat_SunriseSkybox" };
                    isNew = true;
                }
            }

            if (mat != null)
            {
                mat.SetFloat("_SunDisk", 2); // High quality
                mat.SetFloat("_SunSize", 0.045f);
                mat.SetFloat("_AtmosphereThickness", 1.45f); // Morning sunrise haze
                mat.SetColor("_SkyTint", new Color(0.96f, 0.60f, 0.46f)); // Orange-pink sunrise
                mat.SetColor("_GroundColor", new Color(0.38f, 0.28f, 0.24f)); // Warm dark ground
                mat.SetFloat("_Exposure", 1.15f);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(mat, path);
                }
                else
                {
                    EditorUtility.SetDirty(mat);
                }
                AssetDatabase.SaveAssets();
            }
            return mat;
        }

        public static VolumeProfile GetOrCreateCourtyardVolumeProfile()
        {
            string profilePath = $"{ScenesBasePath}/Courtyard_VolumeProfile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            bool isNew = false;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "Courtyard_VolumeProfile";
                isNew = true;
            }

            profile.components.RemoveAll(component => component == null);

            // Bloom
            if (!profile.TryGet<Bloom>(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.threshold.Override(1.2f);
            bloom.intensity.Override(0.16f);
            bloom.scatter.Override(0.70f);
            bloom.tint.Override(new Color(1.0f, 0.88f, 0.76f));

            // Color Adjustments
            if (!profile.TryGet<ColorAdjustments>(out ColorAdjustments colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
            }
            colorAdj.postExposure.Override(-0.35f);
            colorAdj.contrast.Override(14f);
            colorAdj.colorFilter.Override(new Color(1.0f, 0.96f, 0.92f)); // Warm sunrise peach
            colorAdj.saturation.Override(12f);

            // Tonemapping (ACES filmic tone mapping prevents big glare)
            if (!profile.TryGet<Tonemapping>(out Tonemapping tone))
            {
                tone = profile.Add<Tonemapping>(true);
            }
            tone.mode.Override(TonemappingMode.ACES);

            // Vignette
            if (!profile.TryGet<Vignette>(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.35f);

            if (isNew)
            {
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            else
            {
                EditorUtility.SetDirty(profile);
            }
            foreach (var component in profile.components)
            {
                if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
                EditorUtility.SetDirty(component);
            }
            AssetDatabase.SaveAssets();
            return profile;
        }

        [MenuItem("Tools/Legacy/Environments/Build Courtyard")]
        public static void BuildCourtyard()
        {
            EnsureDirectoryExists(ScenesBasePath);
            string scenePath = $"{ScenesBasePath}/Courtyard.unity";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Atmosphere & System
            SetupStandardCameraAndManager(scene, new Vector3(0f, 4.0f, 21.0f), new Vector3(10f, 180f, 0f), Constants.Courtyard);

            // Sunrise Skybox, Lighting & Fog
            GameObject lightRoot = new GameObject("--- LIGHTING & ATMOSPHERE ---");

            Material sunriseSky = GetOrCreateSunriseSkybox();
            if (sunriseSky != null)
            {
                RenderSettings.skybox = sunriseSky;
            }

            GameObject sunObj = new GameObject("Directional_SunriseSun");
            sunObj.transform.parent = lightRoot.transform;
            sunObj.transform.rotation = Quaternion.Euler(14f, -40f, 0f); // Low sun angle near horizon
            Light sunLight = sunObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1.0f, 0.85f, 0.63f); // Warm golden sunrise (#FFD9A0)
            sunLight.intensity = 2.4f; // Intensity 2-3 as requested
            sunLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.54f, 0.44f, 0.36f); // Warm dawn ambient fill
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.fogColor = new Color(0.96f, 0.78f, 0.62f); // Soft orange sunrise mist

            // URP Post Processing Volume
            GameObject volObj = new GameObject("Courtyard_PostProcessingVolume");
            volObj.transform.parent = lightRoot.transform;
            Volume vol = volObj.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1.0f;
            vol.sharedProfile = GetOrCreateCourtyardVolumeProfile();

            // 2. Materials
            Material stoneMat = GetMaterial("mat_StoneTemple");
            Material wetFloorMat = GetMaterial("mat_StoneFloorWet");
            Material brassMat = GetMaterial("mat_GoldBrass");

            // 3. Environment Hierarchy
            GameObject envRoot = new GameObject("--- ENVIRONMENT ---");

            // Warm grey stone plaza with subtle wet reflections (60m x 60m)
            CreateBox("Plaza_WetStoneFloor", envRoot.transform, new Vector3(0, -0.25f, 0), new Vector3(60f, 0.5f, 60f), wetFloorMat, true);

            // Boundary Walls (Prakara)
            GameObject wallsRoot = new GameObject("Plaza_BoundaryWalls");
            wallsRoot.transform.parent = envRoot.transform;
            CreateBox("Wall_North", wallsRoot.transform, new Vector3(0, 1.5f, -30f), new Vector3(60f, 3.0f, 1.2f), stoneMat, true);
            CreateBox("Wall_South", wallsRoot.transform, new Vector3(0, 1.5f, 30f), new Vector3(60f, 3.0f, 1.2f), stoneMat, true);
            CreateBox("Wall_East", wallsRoot.transform, new Vector3(30f, 1.5f, 0), new Vector3(1.2f, 3.0f, 60f), stoneMat, true);
            CreateBox("Wall_West", wallsRoot.transform, new Vector3(-30f, 1.5f, 0), new Vector3(1.2f, 3.0f, 60f), stoneMat, true);

            // Authentic Jagannath Temple Model (FBX)
            GameObject templePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/Temple_Jagannath.fbx");
            if (templePrefab != null)
            {
                GameObject templeInst = (GameObject)PrefabUtility.InstantiatePrefab(templePrefab, envRoot.transform);
                templeInst.name = "Temple_Jagannath";
                templeInst.transform.localPosition = new Vector3(0f, 0f, -20f);
                templeInst.transform.localRotation = Quaternion.identity;
                templeInst.transform.localScale = Vector3.one;

                Material warmStone = GetMaterial("mat_WarmTempleStone");
                Material lightStone = GetMaterial("mat_LightStone");
                Material darkStone = GetMaterial("mat_DarkStone");
                Material sacredMetal = GetMaterial("mat_SacredMetal");
                Material sanctumDark = GetMaterial("mat_SanctumDarkness");
                Material templeRed = GetMaterial("mat_TempleRed");
                Material templeWood = GetMaterial("mat_TempleWood");
                Material courtyardStone = GetMaterial("mat_CourtyardStone");
                Material clueMat = GetMaterial("mat_InvestigationClue");

                foreach (var r in templeInst.GetComponentsInChildren<Renderer>(true))
                {
                    string n = r.name.ToUpperInvariant();
                    if (n.Contains("VIMANA_FINAL") || n.Contains("AMALAKA") || n.Contains("CROWN_RING") || n.Contains("BELL") || n.Contains("LAMP_FLAME"))
                        r.sharedMaterial = sacredMetal;
                    else if (n.Contains("SANCTUM") || n.Contains("INTERIOR_FLOOR"))
                        r.sharedMaterial = sanctumDark;
                    else if (n.Contains("GATE") || n.Contains("DOOR") || n.Contains("WOOD"))
                        r.sharedMaterial = templeWood;
                    else if (n.Contains("RED") || n.Contains("FLAG") || n.Contains("CLOTH"))
                        r.sharedMaterial = templeRed;
                    else if (n.Contains("ROOF") || n.Contains("STEP") || n.Contains("LINTEL") || n.Contains("CROWN") || n.Contains("PILLAR"))
                        r.sharedMaterial = lightStone;
                    else if (n.Contains("BASE") || n.Contains("WALL") || n.Contains("DARK"))
                        r.sharedMaterial = darkStone;
                    else if (n.Contains("CLUE") || n.Contains("MAP"))
                        r.sharedMaterial = clueMat;
                    else
                        r.sharedMaterial = warmStone;

                    // Add MeshCollider on structural walls and pillars, KEEP ENTRANCE PORTAL WIDE OPEN
                    if (n.Contains("_BODY") || n.Contains("_BASE") || n.Contains("_WALL") || n.Contains("_TOWER") || n.Contains("PILLAR") || n.Contains("FLOOR"))
                    {
                        if (!n.Contains("MAIN_GATE") && !n.Contains("MAIN_ENTRANCE"))
                        {
                            MeshCollider mc = r.gameObject.GetComponent<MeshCollider>();
                            if (mc == null) mc = r.gameObject.AddComponent<MeshCollider>();
                            mc.convex = false;
                        }
                    }
                }

                // Rotate temple entrance gates wide open for unimpeded walking access
                Transform gateL = templeInst.transform.Find("MAIN_GATE_LEFT");
                if (gateL != null) gateL.localRotation = Quaternion.Euler(0f, -75f, 0f);
                Transform gateR = templeInst.transform.Find("MAIN_GATE_RIGHT");
                if (gateR != null) gateR.localRotation = Quaternion.Euler(0f, 75f, 0f);
            }

            // Grand Stone Steps ascending from Plaza to the Sanctum Portal
            GameObject stepsRoot = new GameObject("EntranceSteps");
            stepsRoot.transform.parent = envRoot.transform;
            int numSteps = 5;
            float stepDepth = 0.75f;
            float stepHeight = 0.24f;
            Material lightStoneMat = GetMaterial("mat_LightStone");
            for (int s = 0; s < numSteps; s++)
            {
                float zPos = -1.0f + s * stepDepth;
                float yPos = 1.2f - (s + 1) * stepHeight;
                CreateBox($"Step_{s + 1}", stepsRoot.transform, new Vector3(0, yPos + stepHeight * 0.5f, zPos + stepDepth * 0.5f), new Vector3(5.5f, stepHeight, stepDepth), lightStoneMat, true);
            }

            // Smooth ramp collider over steps for seamless character locomotion without sticking
            GameObject stairRamp = CreateBox("StairRamp_Smooth", stepsRoot.transform, new Vector3(0, 0.62f, 0.85f), new Vector3(5.6f, 0.2f, 4.2f), null, true);
            stairRamp.transform.localRotation = Quaternion.Euler(-18.5f, 0f, 0f);
            var rampMr = stairRamp.GetComponent<MeshRenderer>();
            if (rampMr != null) rampMr.enabled = false;
            // The old invisible ramp rose in the opposite direction to the visible
            // stairs. Use the actual 24 cm treads with the player's 30 cm step offset.
            stairRamp.GetComponent<Collider>().enabled = false;

            // Two Gajasimha (Guardian Lion) Statues Flanking the Steps
            GameObject lionsRoot = new GameObject("Gajasimha_GuardianLions");
            lionsRoot.transform.parent = envRoot.transform;
            BuildGajasimhaStatue("LionStatue_Left", lionsRoot.transform, new Vector3(-3.5f, 0, 1.8f), 0f, stoneMat, brassMat);
            BuildGajasimhaStatue("LionStatue_Right", lionsRoot.transform, new Vector3(3.5f, 0, 1.8f), 0f, stoneMat, brassMat);

            // Sacred Parshvadevata Deity Niches (Carved Odisha reliefs flanking temple portal)
            GameObject deityRoot = new GameObject("Parshvadevata_DeityNiches");
            deityRoot.transform.parent = envRoot.transform;
            CreateBox("DeityNiche_Left_Narasimha", deityRoot.transform, new Vector3(-2.8f, 1.4f, 0.5f), new Vector3(1.2f, 2.2f, 0.6f), lightStoneMat, true);
            CreateBox("DeityNiche_Right_Varaha", deityRoot.transform, new Vector3(2.8f, 1.4f, 0.5f), new Vector3(1.2f, 2.2f, 0.6f), lightStoneMat, true);

            // Brass Oil Lamps (4 Flickering Warm Lights - Max 8 Real-time lights: 1 Sun + 4 Lamps = 5)
            GameObject lampsRoot = new GameObject("Brass_OilLamps");
            lampsRoot.transform.parent = envRoot.transform;
            CreateOilLamp(lampsRoot.transform, new Vector3(-4.5f, 0, 10.0f), "Deepastambha_LeftCourtyard");
            CreateOilLamp(lampsRoot.transform, new Vector3(4.5f, 0, 10.0f), "Deepastambha_RightCourtyard");
            CreateOilLamp(lampsRoot.transform, new Vector3(-3.2f, 1.2f, -0.6f), "Deepastambha_LeftPlinth");
            CreateOilLamp(lampsRoot.transform, new Vector3(3.2f, 1.2f, -0.6f), "Deepastambha_RightPlinth");

            // Dress the Courtyard with secondary shrines, banners, stalls, rangoli, and palm trees
            DressCourtyardSurroundings(envRoot, stoneMat, lightStoneMat, brassMat);

            // 4. Spawns
            GameObject spawnRoot = new GameObject("--- SPAWN POINTS ---");
            CreateSpawnPoint(spawnRoot.transform, "player_start", new Vector3(0f, 0.05f, 15f), new Vector3(0, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "priest_spawn", new Vector3(1.8f, 0.05f, 11.5f), new Vector3(-0.3f, 0, 1.0f));
            CreateSpawnPoint(spawnRoot.transform, "villager_spawn", new Vector3(-1.8f, 0.05f, 11.5f), new Vector3(0.3f, 0, 1.0f));
            CreateSpawnPoint(spawnRoot.transform, "robber_spawn", new Vector3(12f, 0.05f, -12f), new Vector3(-1, 0, 0.5f));

            // 5. Interactables & Portals
            GameObject interactRoot = new GameObject("--- INTERACTABLES ---");
            CreateInteractable(interactRoot.transform, "courtyard_lion_left", "Inspect Gajasimha guardian statue", new Vector3(-3.5f, 0.8f, 1.8f), 2.5f);
            CreateInteractable(interactRoot.transform, "courtyard_lion_right", "Inspect Gajasimha guardian statue", new Vector3(3.5f, 0.8f, 1.8f), 2.5f);
            CreateInteractable(interactRoot.transform, "deity_niche_narasimha", "Examine sacred Narasimha carved stone relief", new Vector3(-2.8f, 1.2f, 0.5f), 2.5f);
            CreateInteractable(interactRoot.transform, "deity_niche_varaha", "Examine sacred Varaha carved stone relief", new Vector3(2.8f, 1.2f, 0.5f), 2.5f);
            CreateInteractable(interactRoot.transform, "courtyard_lamp", "Examine brass oil lamp", new Vector3(-4.5f, 0.8f, 10.0f), 2.2f);

            // Portal ascending the steps to the Inner Sanctum (walk-in trigger)
            GameObject sanctumPortal = CreateBox("Portal_To_Sanctum", interactRoot.transform, new Vector3(0f, 1.4f, -1.2f), new Vector3(3.5f, 2.8f, 1.0f), GetMaterial("mat_SanctumDarkness"), true);
            BoxCollider pcol = sanctumPortal.GetComponent<BoxCollider>();
            if (pcol != null) pcol.isTrigger = true;
            SceneTransitionTrigger stSanctum = sanctumPortal.AddComponent<SceneTransitionTrigger>();
            stSanctum.Setup("Sanctum", "Inner Sanctum Sanctorum", false);

            // 6. Spawn Real Characters (Visible immediately in front of Detective Aryan)
            GameObject charRoot = new GameObject("Characters");

            GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Investigator.prefab");
            if (invPrefab != null)
            {
                GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(invPrefab, charRoot.transform);
                playerInst.transform.position = new Vector3(0f, 0.05f, 15f);
                playerInst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                OrbitCamera cam = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
                if (cam != null)
                {
                    cam.SetTarget(playerInst.transform);
                }
            }

            GameObject priestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Priest.prefab");
            if (priestPrefab != null)
            {
                GameObject priestInst = (GameObject)PrefabUtility.InstantiatePrefab(priestPrefab, charRoot.transform);
                priestInst.transform.position = new Vector3(1.8f, 0.05f, 11.5f);
                priestInst.transform.rotation = Quaternion.Euler(0f, 195f, 0f);
            }

            GameObject villagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Villager.prefab");
            if (villagerPrefab != null)
            {
                GameObject villagerInst = (GameObject)PrefabUtility.InstantiatePrefab(villagerPrefab, charRoot.transform);
                villagerInst.transform.position = new Vector3(-1.8f, 0.05f, 11.5f);
                villagerInst.transform.rotation = Quaternion.Euler(0f, 165f, 0f);
            }

            // 7. Opening Cinematic Director
            GameObject cineObj = new GameObject("OpeningCinematicDirector");
            cineObj.AddComponent<LegacyOfTemples.Gameplay.OpeningCinematic>();

            // 8. Persistent Gameplay Management Systems
            GameObject sysRoot = new GameObject("--- GAMEPLAY SYSTEMS ---");
            sysRoot.AddComponent<InvestigationJournal>();
            sysRoot.AddComponent<TempleMapOverlay>();
            sysRoot.AddComponent<TutorialPromptManager>();
            sysRoot.AddComponent<HelpHintSystem>();
            sysRoot.AddComponent<GameLevelManager>();
            sysRoot.AddComponent<ShowcaseDirector>();
            sysRoot.AddComponent<StoryDialogueSystem>();

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] Courtyard scene generated successfully with Visual Upgrade at '{scenePath}'.");
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

        private static void DressCourtyardSurroundings(GameObject envRoot, Material stoneMat, Material lightStoneMat, Material brassMat)
        {
            GameObject dressRoot = new GameObject("Courtyard_Decorations_And_Props");
            dressRoot.transform.parent = envRoot.transform;

            Material woodMat = GetMaterial("mat_TempleWood");
            Material saffronMat = GetMaterial("mat_SaffronCloth");
            Material flowerMat = GetMaterial("mat_MarigoldFlowers");
            Material barkMat = GetMaterial("mat_JungleBark");
            Material foliageMat = GetMaterial("mat_JungleMoss");

            // 1. Four Corner Shrines (Miniature Deula Shrines at courtyard corners)
            Vector3[] cornerPositions = new Vector3[]
            {
                new Vector3(-20f, 0f, -20f),
                new Vector3(20f, 0f, -20f),
                new Vector3(-20f, 0f, 20f),
                new Vector3(20f, 0f, 20f)
            };

            for (int i = 0; i < cornerPositions.Length; i++)
            {
                GameObject shrine = new GameObject($"SecondaryShrine_{i + 1}");
                shrine.transform.parent = dressRoot.transform;
                shrine.transform.localPosition = cornerPositions[i];

                CreateBox("Plinth", shrine.transform, new Vector3(0, 0.4f, 0), new Vector3(4.5f, 0.8f, 4.5f), stoneMat, true);
                CreateBox("Sanctuary", shrine.transform, new Vector3(0, 2.2f, 0), new Vector3(3.4f, 2.8f, 3.4f), lightStoneMat, true);
                CreateBox("Roof_Tier1", shrine.transform, new Vector3(0, 3.9f, 0), new Vector3(3.0f, 0.6f, 3.0f), stoneMat, false);
                CreateBox("Roof_Tier2", shrine.transform, new Vector3(0, 4.5f, 0), new Vector3(2.4f, 0.6f, 2.4f), lightStoneMat, false);
                CreateBox("Roof_Tier3", shrine.transform, new Vector3(0, 5.1f, 0), new Vector3(1.7f, 0.6f, 1.7f), stoneMat, false);
                CreateCylinder("Amalaka", shrine.transform, new Vector3(0, 5.6f, 0), new Vector3(1.2f, 0.35f, 1.2f), brassMat, false);
                CreateSphere("Kalasha", shrine.transform, new Vector3(0, 6.0f, 0), new Vector3(0.6f, 0.7f, 0.6f), brassMat, false);

                GameObject pole = CreateCylinder("Flagpole", shrine.transform, new Vector3(0, 7.2f, 0), new Vector3(0.08f, 1.6f, 0.08f), woodMat, false);
                CreateBox("FlagBanner", pole.transform, new Vector3(0.4f, 0.5f, 0), new Vector3(0.8f, 0.5f, 0.02f), saffronMat, false);
            }

            // 2. Flower & Offering Stalls
            Vector3[] stallPositions = new Vector3[] { new Vector3(-8.5f, 0f, 14.0f), new Vector3(8.5f, 0f, 14.0f) };
            for (int s = 0; s < stallPositions.Length; s++)
            {
                GameObject stall = new GameObject($"OfferingStall_{s + 1}");
                stall.transform.parent = dressRoot.transform;
                stall.transform.localPosition = stallPositions[s];

                CreateBox("Table", stall.transform, new Vector3(0, 0.45f, 0), new Vector3(3.2f, 0.9f, 1.4f), woodMat, true);
                CreateCylinder("MarigoldBasket_1", stall.transform, new Vector3(-0.9f, 0.98f, 0), new Vector3(0.65f, 0.16f, 0.65f), flowerMat, false);
                CreateCylinder("MarigoldBasket_2", stall.transform, new Vector3(0f, 0.98f, 0), new Vector3(0.65f, 0.16f, 0.65f), flowerMat, false);
                CreateCylinder("IncenseBowl", stall.transform, new Vector3(0.9f, 0.98f, 0), new Vector3(0.45f, 0.22f, 0.45f), brassMat, false);
                CreateBox("AwningCloth", stall.transform, new Vector3(0, 2.2f, 0), new Vector3(3.4f, 0.08f, 1.6f), saffronMat, false);
            }

            // 3. Central Plaza Rangoli Mandala
            CreateCylinder("Plaza_Rangoli_Mandala", dressRoot.transform, new Vector3(0f, 0.02f, 8.5f), new Vector3(6.5f, 0.02f, 6.5f), lightStoneMat, false);

            // 4. Perimeter Palm Trees
            Vector3[] treePositions = new Vector3[]
            {
                new Vector3(-27f, 0f, -15f),
                new Vector3(-27f, 0f, 0f),
                new Vector3(-27f, 0f, 15f),
                new Vector3(27f, 0f, -15f),
                new Vector3(27f, 0f, 0f),
                new Vector3(27f, 0f, 15f),
                new Vector3(-15f, 0f, 27f),
                new Vector3(15f, 0f, 27f),
                new Vector3(-15f, 0f, -27f),
                new Vector3(15f, 0f, -27f)
            };

            for (int t = 0; t < treePositions.Length; t++)
            {
                GameObject tree = new GameObject($"PerimeterPalm_{t + 1}");
                tree.transform.parent = dressRoot.transform;
                tree.transform.localPosition = treePositions[t];

                CreateCylinder("Trunk", tree.transform, new Vector3(0, 3.8f, 0), new Vector3(0.45f, 7.6f, 0.45f), barkMat, true);
                CreateSphere("Canopy", tree.transform, new Vector3(0, 7.8f, 0), new Vector3(3.8f, 1.2f, 3.8f), foliageMat, false);
            }
        }

        [MenuItem("Tools/Legacy/Environments/Verify Courtyard Visuals")]
        public static bool VerifyCourtyardVisuals()
        {
            Debug.Log("[Verification] Starting Courtyard Visuals & Systems Verification...");
            string scenePath = $"{ScenesBasePath}/Courtyard.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Verification FAILED] Could not open Courtyard scene at {scenePath}");
                return false;
            }

            bool passed = true;

            // 1. Check Temple_Jagannath
            GameObject temple = GameObject.Find("Temple_Jagannath");
            if (temple == null)
            {
                Debug.LogError("[Verification FAILED] Temple_Jagannath not found in scene!");
                passed = false;
            }
            else
            {
                var renderers = temple.GetComponentsInChildren<Renderer>(true);
                var colliders = temple.GetComponentsInChildren<Collider>(true);
                Debug.Log($"[Verification PASSED] Temple_Jagannath found with {renderers.Length} renderers and {colliders.Length} colliders.");
                if (renderers.Length < 100)
                {
                    Debug.LogError($"[Verification FAILED] Temple_Jagannath has only {renderers.Length} renderers (expected ~182)!");
                    passed = false;
                }
            }

            // 2. Check EntranceSteps
            GameObject steps = GameObject.Find("EntranceSteps");
            if (steps == null || steps.GetComponentsInChildren<Collider>().Length < 5)
            {
                Debug.LogError("[Verification FAILED] Entrance steps missing or incomplete!");
                passed = false;
            }
            else
            {
                Debug.Log($"[Verification PASSED] EntranceSteps verified with {steps.GetComponentsInChildren<Collider>().Length} step colliders.");
            }

            // 3. Check Plaza Floor & Material
            GameObject plaza = GameObject.Find("Plaza_WetStoneFloor");
            if (plaza == null)
            {
                Debug.LogError("[Verification FAILED] Plaza_WetStoneFloor not found!");
                passed = false;
            }
            else
            {
                Renderer r = plaza.GetComponent<Renderer>();
                Material m = r != null ? r.sharedMaterial : null;
                if (m == null || !m.name.Contains("mat_StoneFloorWet"))
                {
                    Debug.LogError("[Verification FAILED] Plaza floor is missing mat_StoneFloorWet!");
                    passed = false;
                }
                else
                {
                    Color baseCol = m.GetColor("_BaseColor");
                    float smoothness = m.GetFloat("_Smoothness");
                    float metallic = m.GetFloat("_Metallic");
                    Debug.Log($"[Verification PASSED] Plaza Floor Material: Color=({baseCol.r:F2}, {baseCol.g:F2}, {baseCol.b:F2}), Smoothness={smoothness:F2}, Metallic={metallic:F2} (warm grey stone, subtle sheen, no blue mirror/glare).");
                }
            }

            // 4. Check Sunrise Mood: Skybox, Sun, Fog, Volume
            if (RenderSettings.skybox == null || !RenderSettings.skybox.name.Contains("Sunrise"))
            {
                Debug.LogError("[Verification FAILED] Skybox is not sunrise skybox!");
                passed = false;
            }
            else
            {
                Debug.Log($"[Verification PASSED] Sunrise Skybox configured: {RenderSettings.skybox.name}");
            }

            GameObject sun = GameObject.Find("Directional_SunriseSun");
            if (sun == null)
            {
                Debug.LogError("[Verification FAILED] Directional_SunriseSun missing!");
                passed = false;
            }
            else
            {
                Light sunLt = sun.GetComponent<Light>();
                float sunPitch = sun.transform.eulerAngles.x;
                Debug.Log($"[Verification PASSED] Sunrise Sun: pitch={sunPitch:F1}°, color={sunLt.color}, intensity={sunLt.intensity}.");
            }

            if (!RenderSettings.fog || RenderSettings.fogDensity <= 0.001f)
            {
                Debug.LogError("[Verification FAILED] Sunrise Fog is not properly configured!");
                passed = false;
            }
            else
            {
                Debug.Log($"[Verification PASSED] Sunrise Fog: color={RenderSettings.fogColor}, density={RenderSettings.fogDensity}.");
            }

            // URP Volume
            Volume vol = UnityEngine.Object.FindAnyObjectByType<Volume>();
            if (vol == null || vol.sharedProfile == null)
            {
                Debug.LogError("[Verification FAILED] Post-processing Volume or Profile missing!");
                passed = false;
            }
            else
            {
                bool hasBloom = vol.sharedProfile.Has<Bloom>();
                bool hasColor = vol.sharedProfile.Has<ColorAdjustments>();
                bool hasTone = vol.sharedProfile.Has<Tonemapping>();
                Debug.Log($"[Verification PASSED] Post-processing Volume verified with Bloom={hasBloom}, ColorAdjustments={hasColor}, Tonemapping={hasTone}.");
            }

            // Camera post-processing
            Camera cam = Camera.main;
            if (cam != null)
            {
                var camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null || !camData.renderPostProcessing)
                {
                    Debug.LogError("[Verification FAILED] Camera renderPostProcessing is false!");
                    passed = false;
                }
                else
                {
                    Debug.Log("[Verification PASSED] Main Camera has URP renderPostProcessing enabled.");
                }
            }

            // 5. Lights budget <= 8
            Light[] allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            int activeLights = 0;
            foreach (var l in allLights)
            {
                if (l.enabled && l.gameObject.activeInHierarchy) activeLights++;
            }
            Debug.Log($"[Verification PASSED] Real-time lights count: {activeLights} (Limit: max 8 for 60 FPS).");
            if (activeLights > 8)
            {
                Debug.LogError($"[Verification FAILED] Too many real-time lights: {activeLights} > 8!");
                passed = false;
            }

            // 6. Characters
            GameObject inv = GameObject.Find("Investigator");
            if (inv == null || inv.GetComponent<PlayerController>() == null)
            {
                Debug.LogError("[Verification FAILED] Investigator (Player) not properly placed in Courtyard!");
                passed = false;
            }
            else
            {
                Debug.Log($"[Verification PASSED] Investigator positioned at {inv.transform.position}.");
            }

            if (passed)
            {
                Debug.Log("[Verification SUCCESS] All Courtyard visual upgrade and system requirements verified successfully!");
            }
            else
            {
                Debug.LogError("[Verification FAILED] Some Courtyard requirements failed validation!");
            }

            return passed;
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
            SetupStandardCameraAndManager(scene, new Vector3(0f, 2.8f, 9.5f), new Vector3(8f, 180f, 0f), Constants.Sanctum);

            GameObject lightRoot = new GameObject("--- LIGHTING & ATMOSPHERE ---");

            // Cool moonlight beam through high ceiling clerestory
            GameObject moonObj = new GameObject("Directional_MoonlightBeam");
            moonObj.transform.parent = lightRoot.transform;
            moonObj.transform.rotation = Quaternion.Euler(50f, 25f, 0f);
            Light moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.45f, 0.58f, 0.82f);
            moonLight.intensity = 1.0f;
            moonLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.22f, 0.28f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.010f;
            RenderSettings.fogColor = new Color(0.16f, 0.14f, 0.20f);

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

            // Central Altar Platform (Vedi) & Authentic Lord Jagannath Sacred Deity
            GameObject altarRoot = new GameObject("CentralAltar_Sanctum");
            altarRoot.transform.parent = envRoot.transform;

            // Raised platform
            CreateBox("Altar_Plinth", altarRoot.transform, new Vector3(0, 0.35f, -7.5f), new Vector3(5.2f, 0.7f, 5.2f), stoneMat, true);
            CreateBox("Altar_Step", altarRoot.transform, new Vector3(0, 0.18f, -4.6f), new Vector3(3.2f, 0.35f, 0.6f), stoneMat, true);

            // Sacred Lord Jagannath Deity on Altar (Non-interactable background shrine deity, pristine and respected)
            GameObject idolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/Idol_Jagannath.fbx");
            if (idolPrefab != null)
            {
                GameObject idolInst = (GameObject)PrefabUtility.InstantiatePrefab(idolPrefab, altarRoot.transform);
                idolInst.name = "LordJagannath_SacredDeityShrine";
                idolInst.transform.localPosition = new Vector3(0f, 0.7f, -7.5f);
                idolInst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                idolInst.transform.localScale = Vector3.one * 1.35f;

                foreach (var light in idolInst.GetComponentsInChildren<Light>(true))
                {
                    UnityEngine.Object.DestroyImmediate(light);
                }

                Material warmStone = GetMaterial("mat_WarmTempleStone");
                Material lightStone = GetMaterial("mat_LightStone");
                Material darkStone = GetMaterial("mat_DarkStone");
                Material sacredMetal = GetMaterial("mat_SacredMetal");
                Material sanctumDark = GetMaterial("mat_SanctumDarkness");
                Material templeRed = GetMaterial("mat_TempleRed");

                foreach (var r in idolInst.GetComponentsInChildren<Renderer>(true))
                {
                    string n = r.name.ToUpperInvariant();
                    if (n.Contains("EYE") || n.Contains("WHITE") || n.Contains("SUDARSHANA"))
                        r.sharedMaterial = lightStone;
                    else if (n.Contains("RED") || n.Contains("LIP") || n.Contains("TIKA") || n.Contains("CLOTH"))
                        r.sharedMaterial = templeRed;
                    else if (n.Contains("CROWN") || n.Contains("JEWEL") || n.Contains("GOLD"))
                        r.sharedMaterial = sacredMetal;
                    else if (n.Contains("BODY") || n.Contains("FACE") || n.Contains("BLACK"))
                        r.sharedMaterial = sanctumDark;
                    else
                        r.sharedMaterial = darkStone;
                }
            }

            // Breached Relic Vault & Treasury Chamber (Side arcade at x = -5.5f, z = -2.5f)
            GameObject vaultRoot = new GameObject("RelicVault_BreachScene");
            vaultRoot.transform.parent = envRoot.transform;

            // Breached Ancient Relic Chest
            GameObject chestObj = CreateBox("Breached_RelicChest", vaultRoot.transform, new Vector3(-5.5f, 0.45f, -2.5f), new Vector3(2.0f, 0.85f, 1.2f), darkWoodMat, true);
            ClueInspectable chestClue = chestObj.AddComponent<ClueInspectable>();
            chestClue.Setup(
                "clue_breached_case",
                "Breached Sacred Relic Chest",
                "Sanctum Vault Breach",
                "An ancient cedar-wood chest lined with gold silk. The three custom velvet compartments are completely empty: The Sacred Pradip, The Sandstone Rosette, and The Tala Patra Manuscript have been stolen.",
                "The sacred Jagannath deity on the high altar was untouched in divine sanctity. Only these three portable historic treasures were targeted.",
                2.6f
            );

            // Broken Brass Lock on ground near chest
            GameObject lockObj = CreateBox("Clue_BrokenBrassLock", vaultRoot.transform, new Vector3(-4.6f, 0.08f, -2.2f), new Vector3(0.35f, 0.12f, 0.25f), brassMat, true);
            ClueInspectable lockClue = lockObj.AddComponent<ClueInspectable>();
            lockClue.Setup(
                "clue_broken_lock",
                "Broken Brass Sanctum Lock",
                "Sanctum Vault Breach",
                "A heavy cast-brass sanctuary lock, cracked clean in two. The latch tongue shows sharp indentation marks from cold steel chisels, forced open with violent leverage.",
                "The intruders came equipped with heavy iron masonry tools. This was not vandalism; it was an organized, targeted heist.",
                2.2f
            );

            // Muddy bootprints trailing toward south corridor
            GameObject bootprintsObj = CreateBox("Clue_MuddyBootprints", vaultRoot.transform, new Vector3(-3.8f, 0.02f, 1.2f), new Vector3(1.2f, 0.04f, 4.0f), GetMaterial("mat_StoneFloorWet"), true);
            ClueInspectable bootClue = bootprintsObj.AddComponent<ClueInspectable>();
            bootClue.Setup(
                "clue_muddy_footprints",
                "Red Clay Muddy Bootprints",
                "Escape Trail",
                "Fresh boot impressions coated in wet reddish laterite soil, leading directly from the relic vault toward the southern corridor.",
                "The red laterite clay only occurs along the outer shrine and jungle paths. The thieves fled directly toward the outer ruins.",
                2.4f
            );

            // 4 Flickering Warm Oil Lamps (Max real-time lights <= 8)
            GameObject lampsRoot = new GameObject("Sanctum_OilLamps");
            lampsRoot.transform.parent = envRoot.transform;
            CreateOilLamp(lampsRoot.transform, new Vector3(-2.2f, 0.7f, -7.5f), "AltarLamp_Left");
            CreateOilLamp(lampsRoot.transform, new Vector3(2.2f, 0.7f, -7.5f), "AltarLamp_Right");
            CreateOilLamp(lampsRoot.transform, new Vector3(-4.2f, 0f, 0.0f), "AisleLamp_Left");
            CreateOilLamp(lampsRoot.transform, new Vector3(4.2f, 0f, 0.0f), "AisleLamp_Right");

            // 4. Spawns
            GameObject spawnRoot = new GameObject("--- SPAWN POINTS ---");
            CreateSpawnPoint(spawnRoot.transform, "player_start", new Vector3(0f, 0.05f, 5.0f), new Vector3(0, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "priest_spawn", new Vector3(1.5f, 0.05f, -4.5f), new Vector3(-0.5f, 0, 1));
            CreateSpawnPoint(spawnRoot.transform, "robber_spawn", new Vector3(-4.0f, 0.05f, -6.0f), new Vector3(1, 0, 0.5f));
            CreateSpawnPoint(spawnRoot.transform, "villager_spawn", new Vector3(-3.0f, 0.05f, 8.0f), new Vector3(1, 0, -0.5f));

            // 5. Portals & Other Interactables
            GameObject interactRoot = new GameObject("--- INTERACTABLES ---");
            CreateInteractable(interactRoot.transform, "sanctum_hanging_bell", "Ring temple bell", new Vector3(0f, 3.2f, -2.5f), 2.2f);
            CreateInteractable(interactRoot.transform, "sanctum_fallen_garland", "Inspect discarded marigold garland", new Vector3(1.2f, 0.2f, -5.6f), 2.0f);

            // Portal returning south to Courtyard (smooth walk-through trigger)
            GameObject returnPortal = CreatePortalTrigger(interactRoot.transform, "Portal_BackToCourtyard", new Vector3(0f, 1.4f, 13.0f), new Vector3(4.0f, 2.8f, 1.0f));
            SceneTransitionTrigger stCourtyard = returnPortal.AddComponent<SceneTransitionTrigger>();
            stCourtyard.Setup("Courtyard", "Courtyard Plaza", false);

            // Portal leading south to Outer Shrine (Level 2)
            GameObject shrinePortal = CreatePortalTrigger(interactRoot.transform, "Portal_To_ShrineCrime", new Vector3(-7.5f, 1.4f, 6.0f), new Vector3(1.0f, 2.8f, 3.0f));
            SceneTransitionTrigger stShrine = shrinePortal.AddComponent<SceneTransitionTrigger>();
            stShrine.Setup("ShrineCrime", "Outer Shrine Ruins (Level 2)", true, "LevelUnlocked_2");

            // 6. Spawn Real Characters
            GameObject charRoot = new GameObject("Characters");

            GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Investigator.prefab");
            if (invPrefab != null)
            {
                GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(invPrefab, charRoot.transform);
                playerInst.transform.position = new Vector3(0f, 0.05f, 10.5f);
                playerInst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                OrbitCamera cam = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
                if (cam != null)
                {
                    cam.SetTarget(playerInst.transform);
                }
            }

            GameObject priestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Priest.prefab");
            if (priestPrefab != null)
            {
                GameObject priestInst = (GameObject)PrefabUtility.InstantiatePrefab(priestPrefab, charRoot.transform);
                priestInst.transform.position = new Vector3(1.5f, 0.05f, -4.5f);
                priestInst.transform.rotation = Quaternion.Euler(0f, 210f, 0f);
            }

            GameObject villagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Villager.prefab");
            if (villagerPrefab != null)
            {
                GameObject villagerInst = (GameObject)PrefabUtility.InstantiatePrefab(villagerPrefab, charRoot.transform);
                villagerInst.transform.position = new Vector3(-1.8f, 0.05f, 6.0f);
                villagerInst.transform.rotation = Quaternion.Euler(0f, 150f, 0f);
            }

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
            BuildPuzzleChakraWheel(pWallRoot.transform, new Vector3(0, 3.6f, -1.6f), brassMat, reliefMat, stoneMat);

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

            // 9. Spawn Characters in PuzzleWall (Investigator & Pujari Ananda)
            GameObject charRoot = new GameObject("Characters");

            GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Investigator.prefab");
            if (invPrefab != null)
            {
                GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(invPrefab, charRoot.transform);
                playerInst.transform.position = new Vector3(0f, 0.05f, 5.0f);
                playerInst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                OrbitCamera cam = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
                if (cam != null)
                {
                    cam.SetTarget(playerInst.transform);
                }
            }

            GameObject priestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Priest.prefab");
            if (priestPrefab != null)
            {
                GameObject priestInst = (GameObject)PrefabUtility.InstantiatePrefab(priestPrefab, charRoot.transform);
                priestInst.transform.position = new Vector3(2.5f, 0.05f, 3.8f);
                priestInst.transform.rotation = Quaternion.Euler(0f, -40f, 0f);

                NPCDialogueTrigger dt = priestInst.GetComponent<NPCDialogueTrigger>();
                if (dt != null)
                {
                    dt.Setup(
                        Constants.Priest,
                        "Pujari Ananda",
                        new string[]
                        {
                            "My grandfather spoke of a wall that opens when four symbols face the rising sun.",
                            "Scroll shows order: lotus, conch, wheel, kalash, then turn the centre wheel.",
                            "Aryan: The passage is open. They went this way."
                        },
                        2.8f
                    );
                }
            }

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] PuzzleWall scene generated successfully at '{scenePath}'.");
            UpdateBuildSettings();
        }

        private static void BuildPuzzleChakraWheel(Transform parent, Vector3 localPos, Material brassMat, Material accentMat, Material stoneMat)
        {
            GameObject wheelObj = new GameObject("Carved_SuryaChakraWall_Mechanism");
            wheelObj.transform.parent = parent;
            wheelObj.transform.localPosition = localPos;

            // 1. Outer Wheel Disc (12 solar spokes)
            GameObject outerWheel = new GameObject("OuterWheelDisc");
            outerWheel.transform.parent = wheelObj.transform;
            outerWheel.transform.localPosition = Vector3.zero;

            GameObject rim = CreateCylinder("OuterRim", outerWheel.transform, Vector3.zero, new Vector3(2.8f, 0.15f, 2.8f), brassMat, true);
            rim.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                GameObject spoke = CreateBox($"SolarSpoke_{i + 1}", outerWheel.transform, Vector3.zero, new Vector3(0.12f, 2.6f, 0.12f), brassMat, false);
                spoke.transform.localRotation = Quaternion.Euler(0, 0, angle);
            }

            // 2. Middle Wheel Disc (8 lunar quadrants)
            GameObject middleWheel = new GameObject("MiddleWheelDisc");
            middleWheel.transform.parent = wheelObj.transform;
            middleWheel.transform.localPosition = new Vector3(0, 0, -0.04f);

            GameObject midRim = CreateCylinder("MiddleRim", middleWheel.transform, Vector3.zero, new Vector3(1.8f, 0.18f, 1.8f), accentMat, true);
            midRim.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                GameObject lunarNotch = CreateBox($"LunarNotch_{i + 1}", middleWheel.transform, Vector3.zero, new Vector3(0.16f, 1.6f, 0.14f), accentMat, false);
                lunarNotch.transform.localRotation = Quaternion.Euler(0, 0, angle);
            }

            // 3. Inner Wheel Disc (Lotus Core)
            GameObject innerWheel = new GameObject("InnerWheelDisc");
            innerWheel.transform.parent = wheelObj.transform;
            innerWheel.transform.localPosition = new Vector3(0, 0, -0.08f);

            GameObject hub = CreateCylinder("CenterHub", innerWheel.transform, Vector3.zero, new Vector3(0.9f, 0.22f, 0.9f), brassMat, true);
            hub.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            for (int p = 0; p < 6; p++)
            {
                float a = p * 60f;
                GameObject petal = CreateSphere($"LotusPetal_{p}", innerWheel.transform, Vector3.zero, new Vector3(0.35f, 0.65f, 0.15f), accentMat, false);
                petal.transform.localRotation = Quaternion.Euler(0, 0, a);
            }

            // Stone Gateway Door that slides open when solved
            GameObject doorObj = CreateBox("StoneGateway_Door", parent, new Vector3(0, 2.5f, 0.5f), new Vector3(3.2f, 5.0f, 0.8f), stoneMat, true);

            // Attach ChakraPuzzleController
            ChakraPuzzleController controller = wheelObj.AddComponent<ChakraPuzzleController>();
            var outerField = typeof(ChakraPuzzleController).GetField("outerWheel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (outerField != null) outerField.SetValue(controller, outerWheel.transform);

            var midField = typeof(ChakraPuzzleController).GetField("middleWheel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (midField != null) midField.SetValue(controller, middleWheel.transform);

            var innerField = typeof(ChakraPuzzleController).GetField("innerWheel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (innerField != null) innerField.SetValue(controller, innerWheel.transform);

            var doorField = typeof(ChakraPuzzleController).GetField("stoneGatewayDoor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (doorField != null) doorField.SetValue(controller, doorObj.transform);

            BoxCollider col = wheelObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(3.2f, 3.2f, 1.5f);

            // Portal behind door leading to JunglePath
            GameObject junglePortal = CreatePortalTrigger(parent, "Portal_To_JunglePath", new Vector3(0, 2.5f, 2.0f), new Vector3(3.0f, 4.5f, 1.0f));
            SceneTransitionTrigger stJungle = junglePortal.AddComponent<SceneTransitionTrigger>();
            stJungle.Setup("JunglePath", "Overgrown Jungle Path (Level 3)", true, "LevelUnlocked_3");
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
            SetupStandardCameraAndManager(scene, new Vector3(0f, 2.6f, 6.5f), new Vector3(6f, 180f, 0f), Constants.ShrineCrime);

            GameObject lightRoot = new GameObject("--- LIGHTING & ATMOSPHERE ---");

            // Cool moonlight streaming through the shattered wooden doorway
            GameObject moonObj = new GameObject("Directional_MoonlightBrokenDoor");
            moonObj.transform.parent = lightRoot.transform;
            moonObj.transform.rotation = Quaternion.Euler(30f, 160f, 0f);
            Light moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.42f, 0.62f, 0.90f);
            moonLight.intensity = 1.1f;
            moonLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.22f, 0.28f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.12f, 0.14f, 0.20f);

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
            CreateSpawnPoint(spawnRoot.transform, "player_start", new Vector3(0f, 0.05f, 2.5f), new Vector3(0, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "villager_spawn", new Vector3(3.2f, 0.05f, 5.5f), new Vector3(-0.5f, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "priest_spawn", new Vector3(3.0f, 0.05f, 0f), new Vector3(-1, 0, 0));
            CreateSpawnPoint(spawnRoot.transform, "robber_spawn", new Vector3(-4.0f, 0.05f, -8.0f), new Vector3(0.5f, 0, 1));

            // 9. Clues, Interactables & Portals
            GameObject interactRoot = new GameObject("--- INTERACTABLES ---");

            GameObject fabricObj = CreateBox("Clue_TornFabric", interactRoot.transform, new Vector3(-7.3f, 1.8f, 1.2f), new Vector3(0.4f, 0.5f, 0.1f), redClothMat, true);
            ClueInspectable fabricClue = fabricObj.AddComponent<ClueInspectable>();
            fabricClue.Setup(
                "clue_fabric_fragment",
                "Torn Dark Cloak Thread",
                "Perpetrator Evidence",
                "A shred of midnight-black woven wool caught on a sharp sandstone projection of the ruined plinth. Strong scent of burning torch pitch.",
                "Matches the dark traveler cloaks worn by black-market antiquities smugglers. The thieves were running recklessly in the dark.",
                2.5f
            );

            GameObject mapObj = CreateBox("Clue_SmugglerMap", interactRoot.transform, new Vector3(2.5f, 0.35f, -3.2f), new Vector3(0.6f, 0.1f, 0.45f), GetMaterial("mat_LimestoneRelief"), true);
            ClueInspectable mapClue = mapObj.AddComponent<ClueInspectable>();
            mapClue.Setup(
                "clue_ancient_map",
                "Smuggler's Route Map",
                "Flight Path",
                "A crumpled parchment dropped near the overturned stone urn. A hand-drawn sketch detailing an escape through the ancient Surya Chakra Wall into the jungle.",
                "The thieves knew the ancient wall seals the secret trail through the jungle ravine. We must pursue them to the Chakra Wall!",
                2.5f
            );

            CreateInteractable(interactRoot.transform, "broken_lamp", "Examine shattered oil lamp and spilled oil", new Vector3(-2.2f, 0.4f, -2.5f), 2.5f);
            CreateInteractable(interactRoot.transform, "footprints", "Examine muddy footprints fleeing out the door", new Vector3(0.5f, 0.2f, 5.0f), 2.5f);

            // Portal leading west to PuzzleWall
            GameObject puzzlePortal = CreatePortalTrigger(interactRoot.transform, "Portal_To_PuzzleWall", new Vector3(-7.5f, 1.4f, -4.5f), new Vector3(1.0f, 2.8f, 3.0f));
            SceneTransitionTrigger stPuzzle = puzzlePortal.AddComponent<SceneTransitionTrigger>();
            stPuzzle.Setup("PuzzleWall", "Ancient Surya Chakra Wall", true);

            // Portal returning to Courtyard
            GameObject returnPortal = CreatePortalTrigger(interactRoot.transform, "Portal_BackToCourtyard", new Vector3(0f, 1.4f, 10.0f), new Vector3(3.5f, 2.8f, 1.0f));
            SceneTransitionTrigger stCourtyard = returnPortal.AddComponent<SceneTransitionTrigger>();
            stCourtyard.Setup("Courtyard", "Courtyard Plaza", true);

            // 10. Spawn Characters
            GameObject charRoot = new GameObject("Characters");

            GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Investigator.prefab");
            if (invPrefab != null)
            {
                GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(invPrefab, charRoot.transform);
                playerInst.transform.position = new Vector3(0f, 0.05f, 7.5f);
                playerInst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                OrbitCamera cam = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
                if (cam != null)
                {
                    cam.SetTarget(playerInst.transform);
                }
            }

            GameObject priestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Priest.prefab");
            if (priestPrefab != null)
            {
                GameObject priestInst = (GameObject)PrefabUtility.InstantiatePrefab(priestPrefab, charRoot.transform);
                priestInst.transform.position = new Vector3(2.5f, 0.05f, 4.0f);
                priestInst.transform.rotation = Quaternion.Euler(0f, 210f, 0f);
            }

            GameObject villagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Villager.prefab");
            if (villagerPrefab != null)
            {
                GameObject villagerInst = (GameObject)PrefabUtility.InstantiatePrefab(villagerPrefab, charRoot.transform);
                villagerInst.transform.position = new Vector3(-2.5f, 0.05f, 4.0f);
                villagerInst.transform.rotation = Quaternion.Euler(0f, 150f, 0f);
            }

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

            // 1. System & Atmosphere
            SetupStandardCameraAndManager(scene, new Vector3(0f, 3.5f, 16.0f), new Vector3(8f, 180f, 0f), Constants.JunglePath);

            GameObject lightRoot = new GameObject("--- LIGHTING & ATMOSPHERE ---");

            // Cool moonlight directional light piercing the dense canopy
            GameObject moonObj = new GameObject("Directional_JungleMoonlight");
            moonObj.transform.parent = lightRoot.transform;
            moonObj.transform.rotation = Quaternion.Euler(35f, 130f, 0f);
            Light moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.48f, 0.68f, 0.95f);
            moonLight.intensity = 1.2f;
            moonLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.26f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.10f, 0.16f, 0.22f);

            // 2. Materials
            Material mudMat = GetMaterial("mat_MudPath");
            Material streamMat = GetMaterial("mat_JungleStream");
            Material stoneMat = GetMaterial("mat_StoneTemple");
            Material mossMat = GetMaterial("mat_JungleMoss");
            Material barkMat = GetMaterial("mat_JungleBark");
            Material brassMat = GetMaterial("mat_GoldBrass");
            Material footprintMat = GetMaterial("mat_CrimeFootprint");

            // 3. Environment Hierarchy
            GameObject envRoot = new GameObject("--- ENVIRONMENT ---");

            // Main Jungle Ground (40m x 60m)
            CreateBox("Jungle_GroundTerrain", envRoot.transform, new Vector3(0, -0.25f, 0), new Vector3(40f, 0.5f, 60f), mudMat, true);

            // Natural Ravine Boundary Boulders (colliders preventing falling off)
            BuildRavineBoulders(envRoot.transform, stoneMat, mossMat);

            // Flowing Water Stream Cutting Across Path at z = 0
            GameObject streamRoot = new GameObject("Jungle_StreamChannel");
            streamRoot.transform.parent = envRoot.transform;
            CreateBox("StreamWaterSurface", streamRoot.transform, new Vector3(0, 0.05f, 0), new Vector3(32f, 0.1f, 4.5f), streamMat, false);

            // Stream Stepping Stones / Crossing Rocks
            CreateCylinder("CrossingRock_1", streamRoot.transform, new Vector3(-0.6f, 0.15f, -1.2f), new Vector3(1.1f, 0.25f, 1.1f), stoneMat, true);
            CreateCylinder("CrossingRock_2", streamRoot.transform, new Vector3(0.5f, 0.15f, 0.0f), new Vector3(1.2f, 0.25f, 1.2f), mossMat, true);
            CreateCylinder("CrossingRock_3", streamRoot.transform, new Vector3(-0.4f, 0.15f, 1.3f), new Vector3(1.1f, 0.25f, 1.1f), stoneMat, true);

            // Ruined Mossy Temple Steps leading up slope to north
            BuildRuinedStaircase(envRoot.transform, stoneMat, mossMat);

            // Jungle Banyan Trees & Creeping Vines
            BuildJungleTrees(envRoot.transform, barkMat, mossMat);

            // Distant Glowing Temple Spire on the Horizon
            BuildDistantGlowingTemple(envRoot.transform, stoneMat, brassMat, lightRoot.transform);

            // Torches along the jungle trail (3 Flickering Warm Lights)
            GameObject torchesRoot = new GameObject("Jungle_TrailTorches");
            torchesRoot.transform.parent = envRoot.transform;
            CreateTorch(torchesRoot.transform, new Vector3(-3.2f, 0.2f, 2.8f), Vector3.zero, "Torch_StreamBank");
            CreateTorch(torchesRoot.transform, new Vector3(3.2f, 1.2f, -3.2f), Vector3.zero, "Torch_StepBase");
            CreateTorch(torchesRoot.transform, new Vector3(-2.2f, 2.6f, -13.5f), Vector3.zero, "Torch_HideoutRidge");

            // Trail of Robber Footprints
            BuildJungleFootprintsTrail(envRoot.transform, footprintMat);

            // Low Ground Mist Layers
            GameObject mistRoot = new GameObject("LowGround_MistLayers");
            mistRoot.transform.parent = envRoot.transform;
            CreateBox("MistLayer_Stream", mistRoot.transform, new Vector3(0, 0.2f, 0), new Vector3(28f, 0.04f, 6.0f), mossMat, false);
            CreateBox("MistLayer_Ruins", mistRoot.transform, new Vector3(0, 1.8f, -8.0f), new Vector3(22f, 0.04f, 12.0f), mossMat, false);

            // 4. Spawns
            GameObject spawnRoot = new GameObject("--- SPAWN POINTS ---");
            CreateSpawnPoint(spawnRoot.transform, "player_start", new Vector3(0f, 0f, 18.0f), new Vector3(0, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "robber_spawn", new Vector3(2.5f, 0f, -14.0f), new Vector3(-0.5f, 0, 1));
            CreateSpawnPoint(spawnRoot.transform, "villager_spawn", new Vector3(-4.0f, 0f, 16.0f), new Vector3(0.5f, 0, -1));
            CreateSpawnPoint(spawnRoot.transform, "priest_spawn", new Vector3(3.5f, 0f, 15.0f), new Vector3(-0.5f, 0, -1));

            // 5. Ruined Jungle Altar & The 3 Stolen Relics (Fictional & respectful story)
            GameObject relicsRoot = new GameObject("RuinedAltar_StolenRelics");
            relicsRoot.transform.parent = envRoot.transform;

            // Forest Altar Plinth
            CreateBox("ForestAltar_Plinth", relicsRoot.transform, new Vector3(0f, 2.1f, -14.0f), new Vector3(4.5f, 0.8f, 2.5f), stoneMat, true);

            // 1. Sacred Pradip Brass Vessel
            GameObject pradipObj = CreateCylinder("Relic_SacredPradip", relicsRoot.transform, new Vector3(-1.1f, 2.65f, -14.0f), new Vector3(0.35f, 0.45f, 0.35f), brassMat, true);
            ClueInspectable pradipClue = pradipObj.AddComponent<ClueInspectable>();
            pradipClue.Setup(
                "relic_pradip",
                "Sacred Pradip Brass Vessel",
                "Stolen Relic",
                "An intricately engraved 16th-century ceremonial brass lamp with seven wick spouts and a peacock finial. Smelling of sandalwood and ghee.",
                "One of the three sacred temple treasures recovered from Bhaskar and Kishore's hideout!",
                2.5f
            );

            // 2. Carved Sandstone Rosette
            GameObject rosetteObj = CreateCylinder("Relic_SandstoneRosette", relicsRoot.transform, new Vector3(0f, 2.7f, -14.0f), new Vector3(0.55f, 0.12f, 0.55f), GetMaterial("mat_WarmTempleStone"), true);
            rosetteObj.transform.localRotation = Quaternion.Euler(45f, 0, 0);
            ClueInspectable rosetteClue = rosetteObj.AddComponent<ClueInspectable>();
            rosetteClue.Setup(
                "relic_rosette",
                "Carved Sandstone Rosette",
                "Stolen Relic",
                "A rare red sandstone medallion carved with celestial Surya solar motifs. Pried from the high vimana frieze.",
                "The second stolen treasure recovered before it could be smuggled across borders!",
                2.5f
            );

            // 3. Ancient Tala Patra Palm-Leaf Manuscript
            GameObject manuscriptObj = CreateBox("Relic_PalmLeafManuscript", relicsRoot.transform, new Vector3(1.1f, 2.6f, -14.0f), new Vector3(0.7f, 0.08f, 0.25f), GetMaterial("mat_DarkWood"), true);
            ClueInspectable manuClue = manuscriptObj.AddComponent<ClueInspectable>();
            manuClue.Setup(
                "relic_manuscript",
                "Ancient Tala Patra Palm-Leaf Manuscript",
                "Stolen Relic",
                "Narrow palm leaves bound between seasoned teak wood covers with crimson silk cord. Inscribed with sacred Odia astronomical and architectural slokas.",
                "The third and final sacred treasure recovered! The heritage is fully restored!",
                2.5f
            );

            // 6. Spawn Real Characters: Investigator, Robber 1 (Bhaskar), Robber 2 (Kishore)
            GameObject charRoot = new GameObject("Characters");

            GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Investigator.prefab");
            if (invPrefab != null)
            {
                GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(invPrefab, charRoot.transform);
                playerInst.transform.position = new Vector3(0f, 0.05f, 18.0f);
                playerInst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                OrbitCamera cam = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
                if (cam != null)
                {
                    cam.SetTarget(playerInst.transform);
                }
            }

            GameObject r1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Robber1.prefab");
            if (r1Prefab != null)
            {
                GameObject r1Inst = (GameObject)PrefabUtility.InstantiatePrefab(r1Prefab, charRoot.transform);
                r1Inst.transform.position = new Vector3(1.6f, 2.45f, -13.5f);
                r1Inst.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            }

            GameObject r2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Robber2.prefab");
            if (r2Prefab != null)
            {
                GameObject r2Inst = (GameObject)PrefabUtility.InstantiatePrefab(r2Prefab, charRoot.transform);
                r2Inst.transform.position = new Vector3(-1.6f, 2.45f, -13.5f);
                r2Inst.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
            }

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] JunglePath scene generated successfully with Stolen Relics at '{scenePath}'.");
            UpdateBuildSettings();
        }

        private static void BuildRavineBoulders(Transform parent, Material stoneMat, Material mossMat)
        {
            GameObject bouldersRoot = new GameObject("Ravine_BoundaryColliders");
            bouldersRoot.transform.parent = parent;

            // West Ravine Cliffs (x = -14m)
            for (float z = -25f; z <= 25f; z += 6f)
            {
                float sy = UnityEngine.Random.Range(3.5f, 6.0f);
                float sx = UnityEngine.Random.Range(4.0f, 6.5f);
                CreateBox($"Cliff_West_{z:F0}", bouldersRoot.transform, new Vector3(-15.5f, sy * 0.5f, z), new Vector3(sx, sy, 7.5f), (z % 2 == 0 ? stoneMat : mossMat), true);
            }

            // East Ravine Cliffs (x = 14m)
            for (float z = -25f; z <= 25f; z += 6f)
            {
                float sy = UnityEngine.Random.Range(3.5f, 6.0f);
                float sx = UnityEngine.Random.Range(4.0f, 6.5f);
                CreateBox($"Cliff_East_{z:F0}", bouldersRoot.transform, new Vector3(15.5f, sy * 0.5f, z), new Vector3(sx, sy, 7.5f), (z % 2 == 0 ? mossMat : stoneMat), true);
            }

            // North and South Boundaries
            CreateBox("Cliff_North", bouldersRoot.transform, new Vector3(0, 3.5f, -28f), new Vector3(40f, 7.0f, 4.0f), stoneMat, true);
            CreateBox("Cliff_South", bouldersRoot.transform, new Vector3(0, 3.5f, 28f), new Vector3(40f, 7.0f, 4.0f), stoneMat, true);
        }

        private static void BuildRuinedStaircase(Transform parent, Material stoneMat, Material mossMat)
        {
            GameObject stairsRoot = new GameObject("Ruined_MossySteps");
            stairsRoot.transform.parent = parent;

            int stepCount = 7;
            float stepDepth = 1.1f;
            float stepRise = 0.35f;
            float startZ = -4.0f;

            for (int s = 0; s < stepCount; s++)
            {
                float curZ = startZ - s * stepDepth;
                float curY = (s + 1) * stepRise;
                Material sm = (s % 2 == 0 ? stoneMat : mossMat);
                CreateBox($"RuinedStep_{s + 1}", stairsRoot.transform, new Vector3(0, curY * 0.5f, curZ), new Vector3(6.5f, curY, stepDepth), sm, true);
            }

            // Upper ruin platform at top of stairs
            float topY = stepCount * stepRise;
            float topZ = startZ - stepCount * stepDepth;
            CreateBox("Ruin_UpperPlatform", stairsRoot.transform, new Vector3(0, topY * 0.5f, topZ - 3.0f), new Vector3(9.0f, topY, 6.0f), stoneMat, true);

            // Flanking Ruined Pillars with dangling ivy
            GameObject pLeft = CreateBox("RuinedPillar_Left", stairsRoot.transform, new Vector3(-3.2f, topY + 2.0f, topZ), new Vector3(1.1f, 4.0f, 1.1f), mossMat, true);
            pLeft.transform.localRotation = Quaternion.Euler(0, 0, -8f);

            GameObject pRight = CreateBox("RuinedPillar_Right", stairsRoot.transform, new Vector3(3.2f, topY + 1.5f, topZ), new Vector3(1.1f, 3.0f, 1.1f), stoneMat, true);
            pRight.transform.localRotation = Quaternion.Euler(6f, 0, 10f);

            // Fallen arch stone slab across stairs
            GameObject brokenLintel = CreateBox("BrokenLintel_Slab", stairsRoot.transform, new Vector3(1.8f, topY + 0.3f, topZ + 1.5f), new Vector3(3.2f, 0.45f, 0.8f), stoneMat, true);
            brokenLintel.transform.localRotation = Quaternion.Euler(15f, 35f, -10f);
        }

        private static void BuildJungleTrees(Transform parent, Material barkMat, Material mossMat)
        {
            GameObject treesRoot = new GameObject("Jungle_BanyanTreesAndVines");
            treesRoot.transform.parent = parent;

            Vector3[] treePos = {
                new Vector3(-8.5f, 0, 12f),
                new Vector3(8.5f, 0, 10f),
                new Vector3(-9.2f, 0, -2.5f),
                new Vector3(9.0f, 0, -1.8f),
                new Vector3(-7.5f, 2.5f, -11f),
                new Vector3(7.8f, 2.5f, -12f)
            };

            for (int t = 0; t < treePos.Length; t++)
            {
                GameObject tree = new GameObject($"BanyanTree_{t + 1}");
                tree.transform.parent = treesRoot.transform;
                tree.transform.localPosition = treePos[t];

                // Gnarled Trunk
                CreateCylinder("Trunk", tree.transform, new Vector3(0, 3.5f, 0), new Vector3(1.6f, 3.5f, 1.6f), barkMat, true);
                CreateCylinder("ButtressRoot1", tree.transform, new Vector3(0.8f, 1.2f, 0.4f), new Vector3(0.7f, 1.2f, 0.7f), barkMat, false);
                CreateCylinder("ButtressRoot2", tree.transform, new Vector3(-0.7f, 1.0f, -0.5f), new Vector3(0.6f, 1.0f, 0.6f), barkMat, false);

                // Spreading Foliage Canopy
                CreateSphere("Canopy_Main", tree.transform, new Vector3(0, 7.5f, 0), new Vector3(6.5f, 4.0f, 6.5f), mossMat, false);
                CreateSphere("Canopy_Sub1", tree.transform, new Vector3(2.0f, 6.8f, 1.5f), new Vector3(4.5f, 3.2f, 4.5f), mossMat, false);

                // Dangling jungle vines
                CreateCylinder("HangingVine1", tree.transform, new Vector3(1.5f, 4.2f, 0.8f), new Vector3(0.12f, 2.2f, 0.12f), mossMat, false);
                CreateCylinder("HangingVine2", tree.transform, new Vector3(-1.4f, 4.6f, -0.6f), new Vector3(0.10f, 2.5f, 0.10f), mossMat, false);
            }
        }

        private static void BuildDistantGlowingTemple(Transform parent, Material stoneMat, Material brassMat, Transform lightParent)
        {
            GameObject distRoot = new GameObject("Distant_GlowingTempleSilhouette");
            distRoot.transform.parent = parent;
            distRoot.transform.localPosition = new Vector3(0, 12f, -50f);

            // Tall Sikhara Tower rising above the dark jungle horizon
            CreateBox("TowerBase", distRoot.transform, new Vector3(0, 2f, 0), new Vector3(10f, 8f, 10f), stoneMat, false);
            CreateBox("TowerSpireMid", distRoot.transform, new Vector3(0, 8f, 0), new Vector3(7.5f, 6f, 7.5f), stoneMat, false);
            CreateBox("TowerSpirePeak", distRoot.transform, new Vector3(0, 13f, 0), new Vector3(5.0f, 5f, 5.0f), stoneMat, false);
            CreateCylinder("Amalaka_Crown", distRoot.transform, new Vector3(0, 16.5f, 0), new Vector3(3.5f, 0.8f, 3.5f), brassMat, false);
            CreateSphere("Kalasa_Finial", distRoot.transform, new Vector3(0, 18.0f, 0), new Vector3(1.4f, 1.8f, 1.4f), brassMat, false);

            // Warm golden glow illuminating the temple spire
            GameObject glowLightObj = new GameObject("PointLight_DistantTempleGlow");
            glowLightObj.transform.parent = lightParent;
            glowLightObj.transform.position = new Vector3(0, 24f, -46f);

            Light glowLt = glowLightObj.AddComponent<Light>();
            glowLt.type = LightType.Point;
            glowLt.color = new Color(1.0f, 0.72f, 0.25f);
            glowLt.intensity = 2.8f;
            glowLt.range = 35.0f;
            glowLt.shadows = LightShadows.None;
        }

        private static void BuildJungleFootprintsTrail(Transform parent, Material footprintMat)
        {
            GameObject trailRoot = new GameObject("Jungle_RobberMuddyTrail");
            trailRoot.transform.parent = parent;

            Vector3[] pathPoints = {
                new Vector3(0.3f, 0.02f, 10.0f),
                new Vector3(-0.2f, 0.02f, 7.5f),
                new Vector3(0.4f, 0.02f, 5.0f),
                new Vector3(-0.1f, 0.02f, 2.5f),
                new Vector3(0.3f, 0.02f, -1.8f),
                new Vector3(-0.2f, 0.5f, -4.5f),
                new Vector3(0.2f, 0.9f, -6.8f),
                new Vector3(-0.1f, 1.4f, -9.0f),
                new Vector3(0.5f, 1.8f, -11.5f),
                new Vector3(1.2f, 2.45f, -14.0f)
            };

            for (int i = 0; i < pathPoints.Length; i++)
            {
                GameObject fp = CreateBox($"TrailPrint_{i + 1}", trailRoot.transform, pathPoints[i], new Vector3(0.25f, 0.02f, 0.48f), footprintMat, false);
                fp.transform.localRotation = Quaternion.Euler(0, (i % 2 == 0 ? 15f : -10f), 0);
            }
        }

        [MenuItem("Tools/Legacy/Environments/Build Main Menu")]
        public static void BuildMainMenu()
        {
            EnsureDirectoryExists("Assets/_Project/Scenes/Dev");
            string scenePath = "Assets/_Project/Scenes/Dev/MainMenu.unity";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Atmosphere & System
            GameObject sysRoot = new GameObject("--- SYSTEM ---");
            GameObject camObj = new GameObject("Main Camera");
            camObj.transform.parent = sysRoot.transform;
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 55f;
            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<LegacyOfTemples.UI.MenuCameraController>();

            // UIManager
            GameObject uiObj = new GameObject("UIManager");
            uiObj.transform.parent = sysRoot.transform;
            uiObj.AddComponent<LegacyOfTemples.UI.MainMenuController>();

            // InputManager
            GameObject inputObj = new GameObject("InputManager");
            inputObj.transform.parent = sysRoot.transform;
            inputObj.AddComponent<LegacyOfTemples.Core.InputManager>();

            // Lighting & Atmosphere
            GameObject lightRoot = new GameObject("--- LIGHTING & ATMOSPHERE ---");
            Material sunriseSky = GetOrCreateSunriseSkybox();
            if (sunriseSky != null) RenderSettings.skybox = sunriseSky;

            GameObject sunObj = new GameObject("Directional_TwilightSun");
            sunObj.transform.parent = lightRoot.transform;
            sunObj.transform.rotation = Quaternion.Euler(18f, -40f, 0f);
            Light sunLight = sunObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1.0f, 0.70f, 0.45f);
            sunLight.intensity = 1.1f;
            sunLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.28f, 0.25f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.90f, 0.60f, 0.45f);

            // URP Post Processing Volume
            GameObject volObj = new GameObject("MainMenu_PostProcessingVolume");
            volObj.transform.parent = lightRoot.transform;
            Volume vol = volObj.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = GetOrCreateCourtyardVolumeProfile();

            // Environment
            GameObject envRoot = new GameObject("--- ENVIRONMENT ---");
            Material wetFloorMat = GetMaterial("mat_StoneFloorWet");
            Material stoneMat = GetMaterial("mat_StoneTemple");
            CreateBox("Plaza_Floor", envRoot.transform, new Vector3(0, -0.25f, 0), new Vector3(60f, 0.5f, 60f), wetFloorMat, true);

            // Temple Model
            GameObject templePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Environment/Temple_Jagannath.fbx");
            if (templePrefab != null)
            {
                GameObject templeInst = (GameObject)PrefabUtility.InstantiatePrefab(templePrefab, envRoot.transform);
                templeInst.name = "Temple_Jagannath";
                templeInst.transform.position = Vector3.zero;

                Material warmStone = GetMaterial("mat_WarmTempleStone");
                Material lightStone = GetMaterial("mat_LightStone");
                Material darkStone = GetMaterial("mat_DarkStone");
                Material sacredMetal = GetMaterial("mat_SacredMetal");

                foreach (var r in templeInst.GetComponentsInChildren<Renderer>(true))
                {
                    string n = r.name.ToUpperInvariant();
                    if (n.Contains("VIMANA_FINAL") || n.Contains("AMALAKA") || n.Contains("BELL"))
                        r.sharedMaterial = sacredMetal;
                    else if (n.Contains("ROOF") || n.Contains("STEP") || n.Contains("LINTEL"))
                        r.sharedMaterial = lightStone;
                    else if (n.Contains("BASE") || n.Contains("WALL"))
                        r.sharedMaterial = darkStone;
                    else
                        r.sharedMaterial = warmStone;
                }
            }

            // Flickering Oil Lamps
            GameObject lampsRoot = new GameObject("Brass_OilLamps");
            lampsRoot.transform.parent = envRoot.transform;
            CreateOilLamp(lampsRoot.transform, new Vector3(-5.0f, 0, 8.0f), "Deepastambha_Left");
            CreateOilLamp(lampsRoot.transform, new Vector3(5.0f, 0, 8.0f), "Deepastambha_Right");

            // Automated QA Verification Bot
            GameObject qaBotObj = new GameObject("GameQATestBot");
            qaBotObj.AddComponent<LegacyOfTemples.UI.GameQATestBot>();

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[EnvironmentBuilder] MainMenu scene generated successfully at '{scenePath}'.");
            UpdateBuildSettings();
        }

        [MenuItem("Tools/Legacy/Build All Environments")]
        public static void BuildAllEnvironments()
        {
            BuildMainMenu();
            BuildCourtyard();
            BuildSanctum();
            BuildPuzzleWall();
            BuildShrineCrime();
            BuildJunglePath();
            UpdateBuildSettings();
            Debug.Log("[EnvironmentBuilder] Main menu and all 5 environments built and registered successfully!");
        }

        #endregion

        #region Batch Mode Entry Points

        public static void BuildMainMenuBatch()
        {
            BuildMainMenu();
        }

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
                "Assets/_Project/Scenes/Dev/MainMenu.unity",
                $"{ScenesBasePath}/Courtyard.unity",
                $"{ScenesBasePath}/Sanctum.unity",
                $"{ScenesBasePath}/PuzzleWall.unity",
                $"{ScenesBasePath}/ShrineCrime.unity",
                $"{ScenesBasePath}/JunglePath.unity",
                "Assets/_Project/Scenes/Dev/CharacterTest.unity"
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
