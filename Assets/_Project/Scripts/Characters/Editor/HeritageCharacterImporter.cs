using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.World;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Characters.Editor
{
    public static class HeritageCharacterImporter
    {
        private const string ModelsBasePath = "Assets/_Project/Models/Characters";
        private const string TexturesBasePath = "Assets/_Project/Models/Characters/Textures";
        private const string MaterialsBasePath = "Assets/_Project/Models/Characters/Materials";
        private const string PrefabBasePath = "Assets/_Project/Prefabs/Characters";
        private const string ConfigBasePath = "Assets/_Project/Models/Characters";
        private const string AnimationBasePath = "Assets/_Project/Animations";

        [MenuItem("Tools/Legacy/Import Heritage Characters")]
        [MenuItem("Tools/Legacy/Build Characters")]
        public static void ImportAllHeritageCharacters()
        {
            Debug.Log("[HeritageCharacterImporter] Starting Heritage Character Import & Rigging Pipeline...");

            EnsureDirectories();
            ConfigureTextureImporters();
            ConfigureModelImporters();
            CreateOrUpdatePBRMaterials();
            AnimatorController animController = CreateOrUpdateAnimatorController();
            AnimatorController priestController = CreateOrUpdatePriestController();
            AnimatorController robber1Controller = CreateOrUpdateRobber1Controller();
            AnimatorController robber2Controller = CreateOrUpdateRobber2Controller();
            AnimatorController villagerController = CreateOrUpdateVillagerController();

            BuildInvestigatorPrefab(animController);
            BuildVillagerPrefab(villagerController);
            BuildPriestPrefab(priestController);
            BuildRobber1Prefab(robber1Controller);
            BuildRobber2Prefab(robber2Controller);

            UpdateCharacterConfigs();
            CleanupOldPlaceholders();
            UpdateCourtyardAndDevScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateHeritageCharacters();
            Debug.Log("[HeritageCharacterImporter] Successfully completed Heritage Character Import & Rigging!");
        }

        public static void ImportAllHeritageCharactersBatch()
        {
            ImportAllHeritageCharacters();
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { ModelsBasePath, TexturesBasePath, MaterialsBasePath, PrefabBasePath, AnimationBasePath };
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            AssetDatabase.Refresh();
        }

        #region Texture Importers

        public static void ConfigureTextureImporters()
        {
            Debug.Log("[HeritageCharacterImporter] Configuring Textures...");
            if (!Directory.Exists(TexturesBasePath)) return;

            string[] textureFiles = Directory.GetFiles(TexturesBasePath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in textureFiles)
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;

                TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer == null) continue;

                string fileName = Path.GetFileName(file).ToLowerInvariant();
                bool isNormal = fileName.Contains("normal") || fileName.Contains("_n_") || fileName.EndsWith("_n.png") || fileName.EndsWith("_normal.jpeg");

                bool changed = false;
                if (isNormal && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    changed = true;
                }
                else if (!isNormal && importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    changed = true;
                }

                if (importer.maxTextureSize > 2048)
                {
                    importer.maxTextureSize = 2048;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
            AssetDatabase.Refresh();
        }

        #endregion

        #region Model Importers

        public static void ConfigureModelImporters()
        {
            Debug.Log("[HeritageCharacterImporter] Configuring FBX Model Importers...");

            // Villager (Mixamo humanoid skeleton)
            ConfigureModel($"{ModelsBasePath}/Villager.fbx", ModelImporterAnimationType.Human);

            // Investigator (Fortnite Gumshoe skeleton)
            ConfigureModel($"{ModelsBasePath}/Investigator.fbx", ModelImporterAnimationType.Human);

            // Priest, Robber 1, Robber 2 (Generic / Static Mesh)
            ConfigureModel($"{ModelsBasePath}/Priest.fbx", ModelImporterAnimationType.Generic);
            ConfigureModel($"{ModelsBasePath}/Robber1.fbx", ModelImporterAnimationType.Generic);
            ConfigureModel($"{ModelsBasePath}/Robber2.fbx", ModelImporterAnimationType.Generic);

            AssetDatabase.Refresh();
        }

        private static void ConfigureModel(string fbxPath, ModelImporterAnimationType animType)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[HeritageCharacterImporter] Model not found: {fbxPath}");
                return;
            }

            bool changed = false;
            if (importer.animationType != animType)
            {
                importer.animationType = animType;
                changed = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                changed = true;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                changed = true;
            }

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[HeritageCharacterImporter] Reimported {Path.GetFileName(fbxPath)} as {animType}.");
            }
        }

        #endregion

        #region PBR Material Setup

        public static void CreateOrUpdatePBRMaterials()
        {
            Debug.Log("[HeritageCharacterImporter] Creating/Updating PBR Materials...");

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Investigator Materials
            SetupMaterial("mat_Investigator_Body", urpShader, "t_m_med_commando_gumshoe_body_d_8.png", "t_m_med_commando_gumshoe_body_n_7.png", 0.35f, 0.1f);
            SetupMaterial("mat_Investigator_Head", urpShader, "T_M_MED_Commando_Gumshoe_head_D_1.png", "M_Med_Soldier_Head_01_N_0.png", 0.35f, 0.05f);
            SetupMaterial("mat_Investigator_Hat", urpShader, "t_m_med_commando_gumshoe_hat_d_14.png", "t_m_med_commando_gumshoe_hat_n_13.png", 0.25f, 0.05f);
            SetupMaterial("mat_Investigator_Hair", urpShader, "T_M_MED_Commando_Gumshoe_hair_D_5.png", "T_M_MED_Commando_Gumshoe_hair_N_4.png", 0.25f, 0.0f);
            SetupMaterial("mat_Investigator_Glasses", urpShader, null, "t_m_med_commando_gumshoe_glasses_n_10.png", 0.85f, 0.2f, new Color(0.12f, 0.12f, 0.14f, 0.9f));
            SetupMaterial("mat_Investigator_Eyes", urpShader, "fortnite_eye_blue_3.png", null, 0.8f, 0.0f);

            // Villager Materials (Indian Woman in Saree)
            SetupMaterial("mat_Villager_Outfit", urpShader, "saree_Aramazd_Color_1Ka.png", "saree_Aramazd_Normal_Tiles.jpeg", 0.30f, 0.05f);
            SetupMaterial("mat_Villager_Body", urpShader, "AvatarBodyFemale_Color_1K.jpeg", "AvatarBodyFemale_Normal1_1K.jpeg", 0.35f, 0.05f);
            SetupMaterial("mat_Villager_Head", urpShader, "AvatarHeadFemale_Color_1Ka_upscayl_2x_ultr.png", "AvatarHeadFemale_Normal1_1K.jpeg", 0.35f, 0.05f);
            SetupMaterial("mat_Villager_Hair", urpShader, "HairCard0_Color_1K.png", "HairCard0_Normal_1K.jpeg", 0.25f, 0.0f);
            SetupMaterial("mat_Villager_Eyes", urpShader, "AvatarEyes_Color_512.jpeg", "AvatarEyes_Normal_512.jpeg", 0.75f, 0.0f);
            SetupMaterial("mat_Villager_Teeth", urpShader, "AvatarTeeth_Color_1K.jpeg", "AvatarTeeth_Normal1_1K.jpeg", 0.50f, 0.0f);

            // Priest Material
            SetupMaterial("mat_Priest", urpShader,
                "elderly-hindu-priest-3d-character-model-gocssfkoamc_Material_01_BaseColor.png",
                "elderly-hindu-priest-3d-character-model-gocssfkoamc_Material_01_Normal.png",
                0.35f, 0.1f, Color.white,
                "elderly-hindu-priest-3d-character-model-gocssfkoamc_Material_01_Metallic.png");

            // Robber 1 Material (Corrupted King in Black Armor)
            SetupMaterial("mat_Robber1", urpShader,
                "Zyklus2_Zeile3_basecolor.jpeg",
                "Zyklus2_Zeile3_normal.jpeg",
                0.45f, 0.65f, Color.white,
                "Zyklus2_Zeile3_metallic.jpeg");

            // Robber 2 Material (Corrupted Archangel Boss)
            SetupMaterial("mat_Robber2", urpShader,
                "Zyklus7_Zeile4_basecolor.jpeg",
                "Zyklus7_Zeile4_normal.jpeg",
                0.45f, 0.55f, Color.white,
                "Zyklus7_Zeile4_metallic.jpeg");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Material SetupMaterial(string matName, Shader shader, string baseTexName, string normalTexName, float smoothness, float metallic, Color? baseColor = null, string metallicTexName = null)
        {
            string matPath = $"{MaterialsBasePath}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = matName };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            if (baseColor.HasValue)
            {
                mat.SetColor("_BaseColor", baseColor.Value);
            }
            else
            {
                mat.SetColor("_BaseColor", Color.white);
            }

            if (!string.IsNullOrEmpty(baseTexName))
            {
                Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesBasePath}/{baseTexName}");
                if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);
            }

            if (!string.IsNullOrEmpty(normalTexName))
            {
                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesBasePath}/{normalTexName}");
                if (normalTex != null)
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            if (!string.IsNullOrEmpty(metallicTexName))
            {
                Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesBasePath}/{metallicTexName}");
                if (metallicTex != null)
                {
                    mat.SetTexture("_MetallicGlossMap", metallicTex);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
            }

            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        #endregion

        #region Animator Controller

        public static AnimatorController CreateOrUpdateAnimatorController()
        {
            string controllerPath = $"{AnimationBasePath}/CharacterAnimator.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                Debug.Log($"[HeritageCharacterImporter] Created new AnimatorController at {controllerPath}.");
            }

            // Ensure parameters exist
            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsRunning", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsCrouching", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Crouch", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Jump", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Interact", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "MoveZ", AnimatorControllerParameterType.Float);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Clear old states
            rootStateMachine.states = new ChildAnimatorState[0];
            rootStateMachine.anyStateTransitions = new AnimatorStateTransition[0];

            AnimationClip idleClip = LoadBestInvestigatorClip("idle");
            AnimationClip walkClip = LoadBestInvestigatorClip("walk");
            AnimationClip runClip = LoadBestInvestigatorClip("run");
            AnimationClip sprintClip = LoadBestInvestigatorClip("sprint");
            AnimationClip crouchClip = LoadBestInvestigatorClip("crouch");
            AnimationClip jumpClip = LoadBestInvestigatorClip("jump");
            AnimationClip interactClip = LoadBestInvestigatorClip("interact");

            // 1. Locomotion BlendTree
            BlendTree blendTree;
            AnimatorState locomotionState = controller.CreateBlendTreeInController("Locomotion", out blendTree);
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = "Speed";
            if (idleClip != null) blendTree.AddChild(idleClip, 0f);
            if (walkClip != null) blendTree.AddChild(walkClip, 2.8f);
            if (runClip != null) blendTree.AddChild(runClip, 6.0f);
            if (sprintClip != null) blendTree.AddChild(sprintClip, 8.5f);

            rootStateMachine.defaultState = locomotionState;

            // 2. Jump State
            AnimatorState jumpState = rootStateMachine.AddState("Jump", new Vector3(250, -80, 0));
            if (jumpClip != null) jumpState.motion = jumpClip;

            var tLocoJump = locomotionState.AddTransition(jumpState);
            tLocoJump.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            tLocoJump.hasExitTime = false;
            tLocoJump.duration = 0.1f;

            var tJumpLoco = jumpState.AddTransition(locomotionState);
            tJumpLoco.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            tJumpLoco.hasExitTime = false;
            tJumpLoco.duration = 0.15f;

            // 3. Crouch State
            AnimatorState crouchState = rootStateMachine.AddState("Crouch", new Vector3(20, 70, 0));
            if (crouchClip != null) crouchState.motion = crouchClip;

            var tLocoCrouch = locomotionState.AddTransition(crouchState);
            tLocoCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");
            tLocoCrouch.hasExitTime = false;
            tLocoCrouch.duration = 0.15f;

            var tCrouchLoco = crouchState.AddTransition(locomotionState);
            tCrouchLoco.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrouching");
            tCrouchLoco.hasExitTime = false;
            tCrouchLoco.duration = 0.15f;

            // 4. Interact State
            AnimatorState interactState = rootStateMachine.AddState("Interact", new Vector3(480, 70, 0));
            if (interactClip != null) interactState.motion = interactClip;

            var tAnyInteract = rootStateMachine.AddAnyStateTransition(interactState);
            tAnyInteract.AddCondition(AnimatorConditionMode.If, 0, "Interact");
            tAnyInteract.hasExitTime = false;
            tAnyInteract.duration = 0.1f;

            var tInteractLoco = interactState.AddTransition(locomotionState);
            tInteractLoco.hasExitTime = true;
            tInteractLoco.exitTime = 0.9f;
            tInteractLoco.duration = 0.2f;

            EditorUtility.SetDirty(controller);
            Debug.Log($"[HeritageCharacterImporter] Built Locomotion BlendTree & Animator with Idle={idleClip != null}, Walk={walkClip != null}, Run={runClip != null}, Sprint={sprintClip != null}, Jump={jumpClip != null}, Crouch={crouchClip != null}");
            return controller;
        }

        private static AnimationClip LoadBestInvestigatorClip(string clipType)
        {
            string clipsDir = $"{AnimationBasePath}/Clips";
            string[] searchFiles = null;
            switch (clipType.ToLowerInvariant())
            {
                case "idle":
                    searchFiles = new[] { "Idle.fbx", "Breathing Idle.fbx", "Investigator_Idle.anim" };
                    break;
                case "walk":
                    searchFiles = new[] { "Walking.fbx", "Walk.fbx", "Investigator_Walk.anim" };
                    break;
                case "run":
                    searchFiles = new[] { "Running.fbx", "Run.fbx", "Investigator_Run.anim" };
                    break;
                case "sprint":
                    searchFiles = new[] { "Sprint.fbx", "Sprinting.fbx", "Investigator_Sprint.anim" };
                    break;
                case "crouch":
                    searchFiles = new[] { "Crouch_Idle.fbx", "Crouch.fbx", "Investigator_Crouch.anim" };
                    break;
                case "jump":
                    searchFiles = new[] { "Jump.fbx", "Jumping.fbx", "Investigator_Jump.anim" };
                    break;
                case "interact":
                    searchFiles = new[] { "Interact.fbx", "Investigator_Interact.anim" };
                    break;
            }

            if (searchFiles != null)
            {
                foreach (var fileName in searchFiles)
                {
                    string p = $"{clipsDir}/{fileName}";
                    if (File.Exists(p))
                    {
                        if (p.EndsWith(".anim"))
                        {
                            AnimationClip c = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                            if (c != null) return c;
                        }
                        else
                        {
                            AnimationClip c = ExtractOrLoadClip(p, "");
                            if (c != null) return c;
                        }
                    }
                }
            }

            return ExtractOrLoadClip($"{ModelsBasePath}/Investigator.fbx", $"Investigator_{clipType}")
                ?? ExtractOrLoadClip($"{ModelsBasePath}/Villager.fbx", "Layer0");
        }

        private static void EnsureParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == paramName) return;
            }
            controller.AddParameter(paramName, type);
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string stateName, Vector3 position)
        {
            foreach (var childState in sm.states)
            {
                if (childState.state.name == stateName) return childState.state;
            }
            return sm.AddState(stateName, position);
        }

        private static AnimationClip ExtractOrLoadClip(string fbxPath, string clipNameSubstring)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    if (string.IsNullOrEmpty(clipNameSubstring) || clip.name.IndexOf(clipNameSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return clip;
                    }
                }
            }
            return null;
        }

        public static AnimatorController CreateOrUpdatePriestController()
        {
            string path = $"{AnimationBasePath}/PriestAnimator.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            EnsureParameter(controller, "IsTalking", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Point", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = GetOrCreateState(rootStateMachine, "Idle", new Vector3(250, 0, 0));
            AnimatorState talkState = GetOrCreateState(rootStateMachine, "Talk", new Vector3(250, 70, 0));
            AnimatorState pointState = GetOrCreateState(rootStateMachine, "Point", new Vector3(250, 140, 0));

            rootStateMachine.defaultState = idleState;

            AnimationClip idleClip = ExtractOrLoadClip($"{ModelsBasePath}/Priest.fbx", "Priest_Idle");
            AnimationClip talkClip = ExtractOrLoadClip($"{ModelsBasePath}/Priest.fbx", "Priest_Talk");
            AnimationClip pointClip = ExtractOrLoadClip($"{ModelsBasePath}/Priest.fbx", "Priest_Point");

            if (idleClip != null) idleState.motion = idleClip;
            if (talkClip != null) talkState.motion = talkClip;
            if (pointClip != null) pointState.motion = pointClip;

            idleState.transitions = new AnimatorStateTransition[0];
            talkState.transitions = new AnimatorStateTransition[0];
            pointState.transitions = new AnimatorStateTransition[0];

            var tIdleTalk = idleState.AddTransition(talkState);
            tIdleTalk.AddCondition(AnimatorConditionMode.If, 0, "IsTalking");
            tIdleTalk.duration = 0.2f;

            var tTalkIdle = talkState.AddTransition(idleState);
            tTalkIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTalking");
            tTalkIdle.duration = 0.2f;

            var tPoint = idleState.AddTransition(pointState);
            tPoint.AddCondition(AnimatorConditionMode.If, 0, "Point");
            tPoint.duration = 0.2f;

            var tPointIdle = pointState.AddTransition(idleState);
            tPointIdle.hasExitTime = true;
            tPointIdle.exitTime = 0.9f;
            tPointIdle.duration = 0.2f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        public static AnimatorController CreateOrUpdateRobber1Controller()
        {
            string path = $"{AnimationBasePath}/Robber1Animator.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Threaten", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = GetOrCreateState(rootStateMachine, "Idle", new Vector3(250, 0, 0));
            AnimatorState walkState = GetOrCreateState(rootStateMachine, "Walk", new Vector3(250, 70, 0));
            AnimatorState threatState = GetOrCreateState(rootStateMachine, "Threaten", new Vector3(250, 140, 0));

            rootStateMachine.defaultState = idleState;

            AnimationClip idleClip = ExtractOrLoadClip($"{ModelsBasePath}/Robber1.fbx", "Robber1_Idle");
            AnimationClip walkClip = ExtractOrLoadClip($"{ModelsBasePath}/Robber1.fbx", "Robber1_Walk");
            AnimationClip threatClip = ExtractOrLoadClip($"{ModelsBasePath}/Robber1.fbx", "Robber1_Threaten");

            if (idleClip != null) idleState.motion = idleClip;
            if (walkClip != null) walkState.motion = walkClip;
            if (threatClip != null) threatState.motion = threatClip;

            idleState.transitions = new AnimatorStateTransition[0];
            walkState.transitions = new AnimatorStateTransition[0];

            var tIdleWalk = idleState.AddTransition(walkState);
            tIdleWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            tIdleWalk.duration = 0.2f;

            var tWalkIdle = walkState.AddTransition(idleState);
            tWalkIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            tWalkIdle.duration = 0.2f;

            var tThreat = idleState.AddTransition(threatState);
            tThreat.AddCondition(AnimatorConditionMode.If, 0, "Threaten");
            tThreat.duration = 0.2f;

            var tThreatIdle = threatState.AddTransition(idleState);
            tThreatIdle.hasExitTime = true;
            tThreatIdle.exitTime = 0.9f;
            tThreatIdle.duration = 0.2f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        public static AnimatorController CreateOrUpdateRobber2Controller()
        {
            string path = $"{AnimationBasePath}/Robber2Animator.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            EnsureParameter(controller, "IsStealth", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Cornered", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = GetOrCreateState(rootStateMachine, "Idle", new Vector3(250, 0, 0));
            AnimatorState stealthState = GetOrCreateState(rootStateMachine, "Stealth", new Vector3(250, 70, 0));
            AnimatorState cornerState = GetOrCreateState(rootStateMachine, "Cornered", new Vector3(250, 140, 0));

            rootStateMachine.defaultState = idleState;

            AnimationClip idleClip = ExtractOrLoadClip($"{ModelsBasePath}/Robber2.fbx", "Robber2_Idle");
            AnimationClip stealthClip = ExtractOrLoadClip($"{ModelsBasePath}/Robber2.fbx", "Robber2_Stealth");
            AnimationClip cornerClip = ExtractOrLoadClip($"{ModelsBasePath}/Robber2.fbx", "Robber2_Cornered");

            if (idleClip != null) idleState.motion = idleClip;
            if (stealthClip != null) stealthState.motion = stealthClip;
            if (cornerClip != null) cornerState.motion = cornerClip;

            idleState.transitions = new AnimatorStateTransition[0];
            stealthState.transitions = new AnimatorStateTransition[0];

            var tIdleStealth = idleState.AddTransition(stealthState);
            tIdleStealth.AddCondition(AnimatorConditionMode.If, 0, "IsStealth");
            tIdleStealth.duration = 0.2f;

            var tStealthIdle = stealthState.AddTransition(idleState);
            tStealthIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsStealth");
            tStealthIdle.duration = 0.2f;

            var tCorner = idleState.AddTransition(cornerState);
            tCorner.AddCondition(AnimatorConditionMode.If, 0, "Cornered");
            tCorner.duration = 0.2f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        public static AnimatorController CreateOrUpdateVillagerController()
        {
            string path = $"{AnimationBasePath}/VillagerAnimator.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            EnsureParameter(controller, "IsTalking", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Pray", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = GetOrCreateState(rootStateMachine, "Idle", new Vector3(250, 0, 0));
            AnimatorState talkState = GetOrCreateState(rootStateMachine, "Talk", new Vector3(250, 70, 0));
            AnimatorState prayState = GetOrCreateState(rootStateMachine, "Pray", new Vector3(250, 140, 0));

            rootStateMachine.defaultState = idleState;

            AnimationClip idleClip = ExtractOrLoadClip($"{ModelsBasePath}/Villager.fbx", "Layer0")
                                  ?? ExtractOrLoadClip($"{ModelsBasePath}/Villager.fbx", "Idle");
            AnimationClip talkClip = ExtractOrLoadClip($"{ModelsBasePath}/Priest.fbx", "Priest_Talk") ?? idleClip;
            AnimationClip prayClip = ExtractOrLoadClip($"{ModelsBasePath}/Priest.fbx", "Priest_Point") ?? idleClip;

            if (idleClip != null) idleState.motion = idleClip;
            if (talkClip != null) talkState.motion = talkClip;
            if (prayClip != null) prayState.motion = prayClip;

            idleState.transitions = new AnimatorStateTransition[0];
            talkState.transitions = new AnimatorStateTransition[0];
            prayState.transitions = new AnimatorStateTransition[0];

            var tIdleTalk = idleState.AddTransition(talkState);
            tIdleTalk.AddCondition(AnimatorConditionMode.If, 0, "IsTalking");
            tIdleTalk.duration = 0.2f;

            var tTalkIdle = talkState.AddTransition(idleState);
            tTalkIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTalking");
            tTalkIdle.duration = 0.2f;

            var tPray = idleState.AddTransition(prayState);
            tPray.AddCondition(AnimatorConditionMode.If, 0, "Pray");
            tPray.duration = 0.2f;

            var tPrayIdle = prayState.AddTransition(idleState);
            tPrayIdle.hasExitTime = true;
            tPrayIdle.exitTime = 0.9f;
            tPrayIdle.duration = 0.2f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        #endregion

        #region Prefab Builders

        private static void BuildInvestigatorPrefab(AnimatorController animController)
        {
            string prefabPath = $"{PrefabBasePath}/Investigator.prefab";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBasePath}/Investigator.fbx");
            if (modelPrefab == null)
            {
                Debug.LogError($"[HeritageCharacterImporter] Cannot find {ModelsBasePath}/Investigator.fbx");
                return;
            }

            GameObject root = new GameObject("Investigator");
            root.tag = "Player";
            root.layer = LayerMask.NameToLayer("Default");

            // Instantiate visual model as child
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            visual.name = "VisualModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Assign PBR Materials
            Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Investigator_Body.mat");
            Material headMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Investigator_Head.mat");
            Material hatMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Investigator_Hat.mat");
            Material hairMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Investigator_Hair.mat");
            Material glassesMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Investigator_Glasses.mat");
            Material eyesMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Investigator_Eyes.mat");

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                string rName = renderer.name.ToLowerInvariant();
                if (rName.Contains("head"))
                {
                    // Head mesh has 5 submesh slots: [eyebrows, head/face, right eye, left eye, hair]
                    // Setting all 5 slots ensures high-res face, blue eyes, and hair texture are crystal clear!
                    renderer.sharedMaterials = new Material[] { headMat, headMat, eyesMat, eyesMat, hairMat };
                }
                else if (rName.Contains("hat")) renderer.sharedMaterial = hatMat;
                else if (rName.Contains("hair")) renderer.sharedMaterial = hairMat;
                else if (rName.Contains("glasses")) renderer.sharedMaterial = glassesMat;
                else renderer.sharedMaterial = bodyMat;
            }

            // Character Controller
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.35f;
            cc.skinWidth = 0.08f;
            cc.minMoveDistance = 0.001f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.35f;
            cc.height = 1.8f;

            // Player Controller
            PlayerController pc = root.AddComponent<PlayerController>();

            // Animator
            Animator anim = root.GetComponentInChildren<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = animController;
            Avatar avatar = LoadAvatar($"{ModelsBasePath}/Investigator.fbx");
            if (avatar != null) anim.avatar = avatar;

            // Camera Target
            GameObject camTarget = new GameObject("CameraTarget");
            camTarget.transform.SetParent(root.transform, false);
            camTarget.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[HeritageCharacterImporter] Built Investigator prefab at {prefabPath}.");
        }

        private static void BuildVillagerPrefab(AnimatorController animController)
        {
            string prefabPath = $"{PrefabBasePath}/Villager.prefab";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBasePath}/Villager.fbx");
            if (modelPrefab == null)
            {
                Debug.LogError($"[HeritageCharacterImporter] Cannot find {ModelsBasePath}/Villager.fbx");
                return;
            }

            GameObject root = new GameObject("Villager");
            root.tag = "Untagged";

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            visual.name = "VisualModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Assign PBR Materials
            Material outfitMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Villager_Outfit.mat");
            Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Villager_Body.mat");
            Material headMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Villager_Head.mat");
            Material hairMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Villager_Hair.mat");
            Material eyesMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Villager_Eyes.mat");
            Material teethMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Villager_Teeth.mat");

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                string rName = renderer.name.ToLowerInvariant();
                if (rName.Contains("outfit")) renderer.sharedMaterial = outfitMat;
                else if (rName.Contains("body")) renderer.sharedMaterial = bodyMat;
                else if (rName.Contains("head")) renderer.sharedMaterial = headMat;
                else if (rName.Contains("hair")) renderer.sharedMaterial = hairMat;
                else if (rName.Contains("eye")) renderer.sharedMaterial = eyesMat;
                else if (rName.Contains("teeth")) renderer.sharedMaterial = teethMat;
                else renderer.sharedMaterial = bodyMat;
            }

            // Collider
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.35f;
            col.height = 1.8f;

            // NPC Behaviors
            NPCFacePlayer face = root.AddComponent<NPCFacePlayer>();
            face.Setup(6.0f, 5.0f);

            NPCDialogueTrigger dt = root.AddComponent<NPCDialogueTrigger>();
            dt.Setup(
                Constants.Villager,
                "Meera (Villager)",
                new string[]
                {
                    "Namaste, Detective! I was bringing fresh marigold garlands to the temple when the alarm bells began ringing frantically.",
                    "The temple storehouse was breached! I saw two dark cloaked figures escaping toward the outer shrine with heavy sacks!",
                    "Elderly Priest Ananda is praying at the courtyard steps. Please speak with him immediately!"
                },
                2.8f
            );

            NPCHeadIndicator ind = root.AddComponent<NPCHeadIndicator>();
            ind.Setup("Meera (Villager)", new Color(0.4f, 0.95f, 0.5f), 1.95f);

            // Animator
            Animator anim = root.GetComponentInChildren<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = animController;
            Avatar avatar = LoadAvatar($"{ModelsBasePath}/Villager.fbx");
            if (avatar != null) anim.avatar = avatar;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[HeritageCharacterImporter] Built Villager prefab at {prefabPath}.");
        }

        private static void BuildPriestPrefab(AnimatorController animController)
        {
            string prefabPath = $"{PrefabBasePath}/Priest.prefab";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBasePath}/Priest.fbx");
            if (modelPrefab == null)
            {
                Debug.LogError($"[HeritageCharacterImporter] Cannot find {ModelsBasePath}/Priest.fbx");
                return;
            }

            GameObject root = new GameObject("Priest");
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            visual.name = "VisualModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Material priestMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Priest.mat");
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = priestMat;
            }

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.86f, 0f);
            col.radius = 0.35f;
            col.height = 1.72f;

            NPCFacePlayer face = root.AddComponent<NPCFacePlayer>();
            face.Setup(6.0f, 5.0f);

            NPCDialogueTrigger dt = root.AddComponent<NPCDialogueTrigger>();
            dt.Setup(
                Constants.Priest,
                "Priest Ananda",
                new string[]
                {
                    "Namaskaram, Detective Aryan... An unspeakable violation has befallen our ancient sanctuary!",
                    "Three priceless heritage relics have been stolen from the locked storage chamber: the ancient ceremonial Pradip vessel, the carved stone rosette, and the sacred Tala Patra palm-leaf manuscript!",
                    "The thieves shattered the brass lock and fled through the outer shrine into the moonlit jungle. You must track their trail and recover our heritage!"
                },
                2.8f
            );

            NPCHeadIndicator ind = root.AddComponent<NPCHeadIndicator>();
            ind.Setup("Priest Ananda", new Color(1.0f, 0.78f, 0.25f), 1.95f);

            Animator anim = root.GetComponentInChildren<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = animController;
            Avatar avatar = LoadAvatar($"{ModelsBasePath}/Priest.fbx");
            if (avatar != null) anim.avatar = avatar;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[HeritageCharacterImporter] Built Priest prefab at {prefabPath}.");
        }

        private static void BuildRobber1Prefab(AnimatorController animController)
        {
            string prefabPath = $"{PrefabBasePath}/Robber1.prefab";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBasePath}/Robber1.fbx");
            if (modelPrefab == null)
            {
                Debug.LogError($"[HeritageCharacterImporter] Cannot find {ModelsBasePath}/Robber1.fbx");
                return;
            }

            GameObject root = new GameObject("Robber1");
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            visual.name = "VisualModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Material r1Mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Robber1.mat");
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = r1Mat;
            }

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.92f, 0f);
            col.radius = 0.40f;
            col.height = 1.85f;

            NPCFacePlayer face = root.AddComponent<NPCFacePlayer>();
            face.Setup(7.0f, 5.0f);

            NPCDialogueTrigger dt = root.AddComponent<NPCDialogueTrigger>();
            dt.Setup(
                Constants.Robber1,
                "Shadow Robber (Leader)",
                new string[]
                {
                    "You're too late, detective! The ancient palm-leaf manuscript and the sacred brass artifacts belong to us now!",
                    "No mortal investigator can stop what has already begun in the shadows of the jungle!"
                },
                2.8f
            );

            NPCHeadIndicator ind = root.AddComponent<NPCHeadIndicator>();
            ind.Setup("Kala (Thief Leader)", new Color(0.95f, 0.35f, 0.35f), 1.95f);

            Animator anim = root.GetComponentInChildren<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = animController;
            Avatar avatar = LoadAvatar($"{ModelsBasePath}/Robber1.fbx");
            if (avatar != null) anim.avatar = avatar;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[HeritageCharacterImporter] Built Robber1 prefab at {prefabPath}.");
        }

        private static void BuildRobber2Prefab(AnimatorController animController)
        {
            string prefabPath = $"{PrefabBasePath}/Robber2.prefab";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsBasePath}/Robber2.fbx");
            if (modelPrefab == null)
            {
                Debug.LogError($"[HeritageCharacterImporter] Cannot find {ModelsBasePath}/Robber2.fbx");
                return;
            }

            GameObject root = new GameObject("Robber2");
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            visual.name = "VisualModel";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Material r2Mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsBasePath}/mat_Robber2.mat");
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = r2Mat;
            }

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.92f, 0f);
            col.radius = 0.40f;
            col.height = 1.85f;

            NPCFacePlayer face = root.AddComponent<NPCFacePlayer>();
            face.Setup(7.0f, 5.0f);

            NPCDialogueTrigger dt = root.AddComponent<NPCDialogueTrigger>();
            dt.Setup(
                Constants.Robber2,
                "Shadow Robber (Accomplice)",
                new string[]
                {
                    "Kala told me it was just an old brass vessel and some ancient scrolls... I didn't sign up for an imperial investigation!",
                    "Back off, detective! We only took the antiquities from the storeroom!"
                },
                2.8f
            );

            NPCHeadIndicator ind = root.AddComponent<NPCHeadIndicator>();
            ind.Setup("Rudra (Accomplice)", new Color(0.95f, 0.45f, 0.35f), 1.95f);

            Animator anim = root.GetComponentInChildren<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = animController;
            Avatar avatar = LoadAvatar($"{ModelsBasePath}/Robber2.fbx");
            if (avatar != null) anim.avatar = avatar;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[HeritageCharacterImporter] Built Robber2 prefab at {prefabPath}.");
        }

        private static Avatar LoadAvatar(string fbxPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is Avatar avatar) return avatar;
            }
            return null;
        }

        #endregion

        #region ScriptableObject Configs

        public static void UpdateCharacterConfigs()
        {
            Debug.Log("[HeritageCharacterImporter] Updating CharacterConfig ScriptableObjects...");

            UpdateConfig("InvestigatorConfig", Constants.Investigator, "Investigator (Detective)", true, $"{PrefabBasePath}/Investigator.prefab");
            UpdateConfig("VillagerConfig", Constants.Villager, "Meera (Villager)", false, $"{PrefabBasePath}/Villager.prefab");
            UpdateConfig("PriestConfig", Constants.Priest, "Priest Ananda", false, $"{PrefabBasePath}/Priest.prefab");
            UpdateConfig("Robber1Config", Constants.Robber1, "Shadow Robber (Leader)", false, $"{PrefabBasePath}/Robber1.prefab");
            UpdateConfig("Robber2Config", Constants.Robber2, "Shadow Robber (Accomplice)", false, $"{PrefabBasePath}/Robber2.prefab");

            AssetDatabase.SaveAssets();
        }

        private static void UpdateConfig(string configAssetName, string id, string displayName, bool isPlayer, string prefabPath)
        {
            string path = $"{ConfigBasePath}/{configAssetName}.asset";
            CharacterConfig config = AssetDatabase.LoadAssetAtPath<CharacterConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CharacterConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            config.SetConfig(id, displayName, isPlayer, prefab);
            EditorUtility.SetDirty(config);
            Debug.Log($"[HeritageCharacterImporter] Updated config {configAssetName} -> {displayName}.");
        }

        #endregion

        #region Cleanup Old Placeholders

        public static void CleanupOldPlaceholders()
        {
            Debug.Log("[HeritageCharacterImporter] Cleaning up old primitive placeholder assets...");

            // Delete obsolete primitive materials
            string[] obsoleteMaterials = new string[]
            {
                "mat_Char_TrenchCoat.mat",
                "mat_Char_DhotiKhaki.mat",
                "mat_Char_LeatherBrown.mat",
                "mat_Char_SkinTone.mat",
                "mat_Char_SaffronAccent.mat",
                "mat_Char_GoldBrass.mat",
                "mat_Char_DarkHair.mat",
                "mat_Char_PriestSilk.mat",
                "mat_Char_PriestAngavastram.mat",
                "mat_Char_RobberDark.mat",
                "mat_Char_RobberRedMask.mat",
                "mat_Char_VillagerKurta.mat"
            };

            foreach (var matName in obsoleteMaterials)
            {
                string p = $"{MaterialsBasePath}/{matName}";
                if (File.Exists(p))
                {
                    AssetDatabase.DeleteAsset(p);
                    Debug.Log($"[HeritageCharacterImporter] Deleted obsolete material: {matName}");
                }
            }

            AssetDatabase.Refresh();
        }

        #endregion

        #region Scene Updates & Spawning

        public static void UpdateCourtyardAndDevScenes()
        {
            Debug.Log("[HeritageCharacterImporter] Updating Courtyard and CharacterTest scenes with real characters...");

            // Update Courtyard
            string courtyardPath = "Assets/_Project/Scenes/Environments/Courtyard.unity";
            if (File.Exists(courtyardPath))
            {
                var scene = EditorSceneManager.OpenScene(courtyardPath, OpenSceneMode.Single);

                // Remove existing characters in scene hierarchy if any
                foreach (var go in scene.GetRootGameObjects())
                {
                    if (go.name == "Investigator" || go.name == "Priest" || go.name == "Villager" || go.name == "Robber1" || go.name == "Robber2" || go.name == "Characters")
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                    }
                }

                // Create Characters parent
                GameObject charRoot = new GameObject("Characters");

                // Spawn Investigator at player_start
                GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBasePath}/Investigator.prefab");
                if (invPrefab != null)
                {
                    Vector3 spawnPos = new Vector3(0f, 0.05f, -12f);
                    SpawnPoint sp = UnityEngine.Object.FindAnyObjectByType<SpawnPoint>();
                    if (sp != null && sp.SpawnId == "player_start")
                    {
                        spawnPos = sp.transform.position;
                    }

                    GameObject playerInst = (GameObject)PrefabUtility.InstantiatePrefab(invPrefab);
                    playerInst.transform.position = spawnPos;
                    playerInst.transform.rotation = Quaternion.identity;
                    playerInst.transform.SetParent(charRoot.transform);

                    // Connect OrbitCamera if present
                    OrbitCamera cam = UnityEngine.Object.FindAnyObjectByType<OrbitCamera>();
                    if (cam != null)
                    {
                        cam.SetTarget(playerInst.transform);
                    }
                }

                // Spawn Priest at priest_spawn
                GameObject priestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBasePath}/Priest.prefab");
                if (priestPrefab != null)
                {
                    GameObject priestInst = (GameObject)PrefabUtility.InstantiatePrefab(priestPrefab);
                    priestInst.transform.position = new Vector3(1.8f, 0.05f, 11.5f);
                    priestInst.transform.rotation = Quaternion.Euler(0f, 195f, 0f);
                    priestInst.transform.SetParent(charRoot.transform);
                }

                // Spawn Villager at villager_spawn
                GameObject villagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBasePath}/Villager.prefab");
                if (villagerPrefab != null)
                {
                    GameObject villagerInst = (GameObject)PrefabUtility.InstantiatePrefab(villagerPrefab);
                    villagerInst.transform.position = new Vector3(-1.8f, 0.05f, 11.5f);
                    villagerInst.transform.rotation = Quaternion.Euler(0f, 165f, 0f);
                    villagerInst.transform.SetParent(charRoot.transform);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[HeritageCharacterImporter] Courtyard scene updated with real characters.");
            }

            // Update CharacterTest Scene
            string testScenePath = "Assets/_Project/Scenes/Dev/CharacterTest.unity";
            if (File.Exists(testScenePath))
            {
                var scene = EditorSceneManager.OpenScene(testScenePath, OpenSceneMode.Single);
                foreach (var go in scene.GetRootGameObjects())
                {
                    if (go.name == "Investigator" || go.name == "Priest" || go.name == "Villager" || go.name == "Robber1" || go.name == "Robber2" || go.name == "Characters")
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                    }
                }

                GameObject testRoot = new GameObject("Characters");
                string[] pNames = { "Investigator", "Villager", "Priest", "Robber1", "Robber2" };
                float spacing = 2.5f;
                float startX = -((pNames.Length - 1) * spacing) * 0.5f;

                for (int i = 0; i < pNames.Length; i++)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBasePath}/{pNames[i]}.prefab");
                    if (prefab != null)
                    {
                        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        inst.transform.position = new Vector3(startX + i * spacing, 0f, 0f);
                        inst.transform.rotation = Quaternion.identity;
                        inst.transform.SetParent(testRoot.transform);
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[HeritageCharacterImporter] CharacterTest scene updated with all 5 real characters.");
            }
        }

        #endregion

        #region Validation

        public static bool ValidateHeritageCharacters()
        {
            Debug.Log("[HeritageCharacterImporter] Validating Heritage Characters...");
            string[] characterNames = { "Investigator", "Villager", "Priest", "Robber1", "Robber2" };
            bool allValid = true;

            foreach (var name in characterNames)
            {
                string pPath = $"{PrefabBasePath}/{name}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                if (prefab == null)
                {
                    Debug.LogError($"[Validation FAILED] Missing prefab at {pPath}");
                    allValid = false;
                    continue;
                }

                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    Debug.LogError($"[Validation FAILED] Prefab {name} has no Renderers!");
                    allValid = false;
                }

                var anim = prefab.GetComponentInChildren<Animator>();
                if (anim == null)
                {
                    Debug.LogWarning($"[Validation Warning] Prefab {name} has no Animator.");
                }

                if (name == "Investigator")
                {
                    if (prefab.GetComponent<PlayerController>() == null)
                    {
                        Debug.LogError($"[Validation FAILED] Investigator is missing PlayerController!");
                        allValid = false;
                    }
                    if (prefab.GetComponent<CharacterController>() == null)
                    {
                        Debug.LogError($"[Validation FAILED] Investigator is missing CharacterController!");
                        allValid = false;
                    }
                }
                else
                {
                    if (prefab.GetComponent<NPCDialogueTrigger>() == null)
                    {
                        Debug.LogError($"[Validation FAILED] NPC {name} is missing NPCDialogueTrigger!");
                        allValid = false;
                    }
                    if (prefab.GetComponent<Collider>() == null)
                    {
                        Debug.LogError($"[Validation FAILED] NPC {name} is missing Collider!");
                        allValid = false;
                    }
                }

                Debug.Log($"[Validation PASSED] {name}: {renderers.Length} renderers, Collider OK, Animator OK.");
            }

            return allValid;
        }

        #endregion
    }
}
