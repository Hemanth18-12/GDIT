using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

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

        [MenuItem("Tools/Legacy/Characters/Build All Character Prefabs")]
        public static void BuildAllCharacterPrefabs()
        {
            BuildInvestigatorPrefab();
            Debug.Log("[CharacterBuilder] Build All Characters completed.");
        }

        public static void BuildInvestigatorBatch()
        {
            BuildInvestigatorPrefab();
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
