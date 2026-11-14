using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>
    /// Part 2 asset setup: the red jeep prefab (URP materials, damage meshes), the CC0 oak tree prefab
    /// (Medieval Village Kit) and the game copy of the user's temple. Run from the menu or in batch mode.
    /// </summary>
    public static class Part2Tools
    {
        const string JeepDir = "Assets/_Project/Models/Vehicles/Jeep";
        const string TreeDir = "Assets/_Project/Art/MedievalVillageKit/Tree";
        const string TempleDir = "Assets/_Project/Models/Temple";

        [MenuItem("Legacy of the Temple/Part 2/Build all")]
        public static void BuildAll() { BuildJeep(); BuildOak(); BuildNpc(); BuildTemple(); AssetDatabase.SaveAssets(); Debug.Log("[Batch] part 2 assets built"); }

        // ------------------------------------------------------------------ materials
        static Material Lit(string path, Color c, float metal, float smooth, Color? emission = null, float alpha = 1f)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            c.a = alpha; m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            if (alpha < 1f)
            {
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent;
            }
            m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
        }
        static Color G(float r, float g, float b) => new Color(r, g, b).gamma;   // Blender values are linear

        static void Remap(string fbx, System.Func<string, Material> pick)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx); if (imp == null) { Debug.LogWarning("[Part2] missing " + fbx); return; }
            imp.importCameras = false; imp.importLights = false; imp.importNormals = ModelImporterNormals.Import; imp.isReadable = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.SaveAndReimport();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
            {
                if (!(o is Material src)) continue;
                var m = pick(src.name); if (m != null) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), m);
            }
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------------ jeep
        public static void BuildJeep()
        {
            Directory.CreateDirectory(JeepDir + "/Materials"); AssetDatabase.Refresh();
            string M(string n) => JeepDir + "/Materials/" + n + ".mat";
            var mats = new Dictionary<string, Material>
            {
                { "MAT_Paint_Red", Lit(M("MAT_Paint_Red"), G(0.42f, 0.018f, 0.012f), 0.35f, 0.78f) },
                { "MAT_Hardtop_Black", Lit(M("MAT_Hardtop_Black"), G(0.03f, 0.03f, 0.03f), 0f, 0.35f) },
                { "MAT_BlackTrim", Lit(M("MAT_BlackTrim"), G(0.02f, 0.02f, 0.02f), 0.1f, 0.4f) },
                { "MAT_Rubber", Lit(M("MAT_Rubber"), G(0.018f, 0.018f, 0.018f), 0f, 0.12f) },
                { "MAT_Alloy", Lit(M("MAT_Alloy"), G(0.55f, 0.56f, 0.58f), 1f, 0.72f) },
                { "MAT_Chrome", Lit(M("MAT_Chrome"), G(0.8f, 0.8f, 0.8f), 1f, 0.9f) },
                { "MAT_Glass", Lit(M("MAT_Glass"), G(0.03f, 0.05f, 0.05f), 0f, 0.95f, null, 0.35f) },
                { "MAT_Light_Head", Lit(M("MAT_Light_Head"), G(1f, 0.96f, 0.85f), 0f, 0.9f, Color.black) },
                { "MAT_Light_Tail", Lit(M("MAT_Light_Tail"), G(0.5f, 0.02f, 0.02f), 0f, 0.8f, Color.black) },
                { "MAT_Light_Reverse", Lit(M("MAT_Light_Reverse"), G(0.9f, 0.9f, 0.9f), 0f, 0.8f, Color.black) },
                { "MAT_Light_Indicator", Lit(M("MAT_Light_Indicator"), G(1f, 0.5f, 0.05f), 0f, 0.8f, Color.black) },
                { "MAT_Gold_Emblem", Lit(M("MAT_Gold_Emblem"), G(0.83f, 0.62f, 0.22f), 1f, 0.7f) },
                { "MAT_Interior", Lit(M("MAT_Interior"), G(0.06f, 0.055f, 0.05f), 0f, 0.25f) },
                { "MAT_Seat_Leather", Lit(M("MAT_Seat_Leather"), G(0.11f, 0.075f, 0.05f), 0f, 0.4f) },
                { "MAT_Plate", Lit(M("MAT_Plate"), G(0.92f, 0.92f, 0.88f), 0f, 0.5f) },
                { "MAT_Undercarriage", Lit(M("MAT_Undercarriage"), G(0.035f, 0.035f, 0.04f), 0.4f, 0.3f) },
                { "Jeep_Screen", Lit(M("MAT_Screen"), G(0.05f, 0.18f, 0.3f), 0f, 0.8f, new Color(0.1f, 0.4f, 0.7f)) },
                { "Jeep_Paint_Scuffed", Lit(M("MAT_Paint_Red_Scuffed"), G(0.30f, 0.03f, 0.02f), 0.2f, 0.35f) },
                { "Jeep_Paint_Burnt", Lit(M("MAT_Paint_Burnt"), G(0.035f, 0.032f, 0.028f), 0.3f, 0.15f) },
            };
            Material Pick(string n) { foreach (var kv in mats) if (n == kv.Key || n.StartsWith(kv.Key)) return kv.Value; return mats["MAT_BlackTrim"]; }
            foreach (var f in new[] { "Car_Model", "Car_Model_LOD1", "Car_Model_LOD2", "Car_Damage_Damaged", "Car_Damage_Wrecked", "Car_Damage_Destroyed" })
                if (File.Exists(JeepDir + "/" + f + ".fbx")) Remap(JeepDir + "/" + f + ".fbx", Pick);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(JeepDir + "/Car_Model.fbx");
            if (model == null) { Debug.LogError("[Part2] Car_Model.fbx not imported"); return; }
            var root = new GameObject("Jeep");
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model); inst.name = "Model"; inst.transform.SetParent(root.transform, false);
            foreach (var r in inst.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = r.sharedMaterial != null && r.sharedMaterial.name.Contains("Glass") ? ShadowCastingMode.Off : ShadowCastingMode.On; }
            var jeep = root.AddComponent<Jeep>();
            Mesh Find(string fbx, string mesh)
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(JeepDir + "/" + fbx + ".fbx")) if (o is Mesh m && m.name.StartsWith(mesh)) return m;   // Blender adds .001 to the damaged copies
                return null;
            }
            string[] states = { null, "Car_Damage_Damaged", "Car_Damage_Wrecked", "Car_Damage_Destroyed" };
            for (int i = 1; i < 4; i++) { jeep.bodyStates[i] = Find(states[i], "SM_Jeep_Body"); jeep.hoodStates[i] = Find(states[i], "SM_Jeep_Hood"); }
            jeep.scuffedPaint = mats["Jeep_Paint_Scuffed"]; jeep.burntPaint = mats["Jeep_Paint_Burnt"]; jeep.interior = mats["MAT_Interior"];
            Directory.CreateDirectory("Assets/_Project/Resources/Vehicles");
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Resources/Vehicles/Jeep.prefab");
            Object.DestroyImmediate(root);
            int tris = 0; foreach (var mf in model.GetComponentsInChildren<MeshFilter>()) tris += mf.sharedMesh != null ? mf.sharedMesh.triangles.Length / 3 : 0;
            Debug.Log($"[Batch] jeep prefab built: {tris} triangles, damage meshes {(jeep.bodyStates[3] != null ? "found" : "MISSING")}");
        }

        // ------------------------------------------------------------------ background villagers (the project's own Villager model)
        public static void BuildNpc()
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Villager.prefab");
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animations/CharacterAnimator.controller");
            if (src == null || ctrl == null) { Debug.LogWarning("[Part2] villager prefab or CharacterAnimator missing"); return; }
            var g = (GameObject)PrefabUtility.InstantiatePrefab(src); PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            g.name = "Villager_NPC";
            foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);   // no dialogue, no name tag: a passer-by, not Meera
            foreach (var c in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            var an = g.GetComponentInChildren<Animator>(); if (an != null) { an.runtimeAnimatorController = ctrl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.CullUpdateTransforms; }
            g.AddComponent<LegacyOfTemples.Characters.NpcAnimDriver>();
            var cap = g.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0f, 0.85f, 0f); cap.height = 1.7f; cap.radius = 0.25f;
            Directory.CreateDirectory("Assets/_Project/Resources/NPC");
            PrefabUtility.SaveAsPrefabAsset(g, "Assets/_Project/Resources/NPC/Villager_NPC.prefab"); Object.DestroyImmediate(g);
            Debug.Log("[Batch] villager NPC prefab built");
            foreach (var rn in new[] { "Robber1", "Robber2" })
            {   // the project's two robbers, for the opening cinematic only (no dialogue or combat scripts)
                var rs = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/" + rn + ".prefab"); if (rs == null) continue;
                var r = (GameObject)PrefabUtility.InstantiatePrefab(rs); PrefabUtility.UnpackPrefabInstance(r, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var mb in r.GetComponentsInChildren<MonoBehaviour>(true)) if (!(mb is LegacyOfTemples.Characters.NpcAnimDriver)) Object.DestroyImmediate(mb);
                foreach (var c in r.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                if (r.GetComponent<LegacyOfTemples.Characters.NpcAnimDriver>() == null) r.AddComponent<LegacyOfTemples.Characters.NpcAnimDriver>();
                var ra = r.GetComponentInChildren<Animator>(); if (ra != null) ra.applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(r, "Assets/_Project/Resources/NPC/" + rn + "_Cine.prefab"); Object.DestroyImmediate(r);
            }
        }

        // ------------------------------------------------------------------ oak (Medieval Village Kit, CC0)
        public static void BuildOak()
        {
            string fbx = TreeDir + "/SM_Tree_Oak.fbx"; if (!File.Exists(fbx)) return;
            void Normal(string p) { var ti = (TextureImporter)AssetImporter.GetAtPath(p); if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); } }
            void Alpha(string p) { var ti = (TextureImporter)AssetImporter.GetAtPath(p); if (ti != null && !ti.alphaIsTransparency) { ti.alphaIsTransparency = true; ti.SaveAndReimport(); } }
            Normal(TreeDir + "/T_Bark_Normal.png"); Alpha(TreeDir + "/T_Leaves_Atlas.png"); Alpha(TreeDir + "/T_Tree_Impostor_0.png"); Alpha(TreeDir + "/T_Tree_Impostor_1.png");
            Material Tex(string name, string tex, string nrm, bool cutout, Color tint)
            {
                var m = Lit(TreeDir + "/" + name + ".mat", tint, 0f, 0.15f);
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TreeDir + "/" + tex));
                if (nrm != null) { m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TreeDir + "/" + nrm)); m.EnableKeyword("_NORMALMAP"); }
                if (cutout) { m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.45f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cull", 0f); m.renderQueue = 2450; m.doubleSidedGI = true; }
                EditorUtility.SetDirty(m); return m;
            }
            var bark = Tex("M_Tree_Bark", "T_Bark_BaseColor.png", "T_Bark_Normal.png", false, Color.white);
            var leaves = Tex("M_Tree_Leaves", "T_Leaves_Atlas.png", null, true, new Color(0.8f, 0.95f, 0.7f));
            var canopy = Tex("M_Tree_Canopy", "T_Leaves_Atlas.png", null, false, new Color(0.45f, 0.62f, 0.32f));
            var imp0 = Tex("M_Tree_Impostor_0", "T_Tree_Impostor_0.png", null, true, Color.white);
            var imp1 = Tex("M_Tree_Impostor_1", "T_Tree_Impostor_1.png", null, true, Color.white);
            Remap(fbx, n => n.Contains("Bark") ? bark : n.Contains("Leaves") ? leaves : n.Contains("Canopy") ? canopy : n.Contains("Impostor_1") ? imp1 : n.Contains("Impostor") ? imp0 : bark);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(model); PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "oak_tree";
            foreach (var t in root.GetComponentsInChildren<Transform>())
                if (t.name.Contains("Collider")) { var r = t.GetComponent<Renderer>(); if (r != null) r.enabled = false; var mf = t.GetComponent<MeshFilter>(); if (mf != null) t.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; }
            if (root.GetComponent<LODGroup>() == null)
            {
                var lods = new List<LOD>(); float[] h = { 0.5f, 0.25f, 0.1f, 0.02f };
                for (int i = 0; i < 4; i++) { var t = root.transform.Find("SM_Tree_Oak_LOD" + i); if (t != null) lods.Add(new LOD(h[i], t.GetComponentsInChildren<Renderer>())); }
                if (lods.Count > 0) root.AddComponent<LODGroup>().SetLODs(lods.ToArray());
            }
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Resources/Props/oak_tree.prefab"); Object.DestroyImmediate(root);
            Debug.Log("[Batch] oak tree prefab built");
        }

        // ------------------------------------------------------------------ temple (user's temple.fbx, optimised copy)
        public static void BuildTemple()
        {
            string fbx = TempleDir + "/Temple_Game.fbx";
            if (!File.Exists(fbx)) { Debug.LogWarning("[Part2] " + fbx + " not there yet (run Tools/blender/optimize_temple.py)"); return; }
            Directory.CreateDirectory(TempleDir + "/Materials"); AssetDatabase.Refresh();
            Material FromPh(string name, string src, Color tint, float tile)
            {
                var path = TempleDir + "/Materials/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                var s = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Mats/" + src + ".mat");
                if (m == null) { m = s != null ? new Material(s) : new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
                else if (s != null) m.CopyPropertiesFromMaterial(s);
                m.SetColor("_BaseColor", tint); m.SetTextureScale("_BaseMap", Vector2.one / tile); EditorUtility.SetDirty(m); return m;
            }
            var sandstone = FromPh("MAT_Temple_Sandstone", "sandstone_blocks_08", new Color(1f, 0.93f, 0.82f), 2.6f);
            var laterite = FromPh("MAT_Temple_Laterite", "red_sandstone_wall", new Color(0.78f, 0.6f, 0.5f), 2.2f);
            var carved = FromPh("MAT_Temple_Carved", "red_sandstone_wall", new Color(1f, 0.9f, 0.78f), 1.6f);
            var roof = FromPh("MAT_Temple_RoofStone", "sandstone_blocks_08", new Color(0.86f, 0.74f, 0.6f), 1.8f);
            var wood = FromPh("MAT_Temple_Wood", "medieval_wood", Color.white, 1.4f);
            var gold = Lit(TempleDir + "/Materials/MAT_Temple_Gold.mat", G(0.83f, 0.6f, 0.2f), 1f, 0.72f);
            var glass = Lit(TempleDir + "/Materials/MAT_Temple_Glass.mat", G(0.1f, 0.12f, 0.12f), 0f, 0.9f, null, 0.5f);
            var cloth = Lit(TempleDir + "/Materials/MAT_Temple_FlagRed.mat", G(0.6f, 0.03f, 0.02f), 0f, 0.2f);
            Remap(fbx, n =>
                n.StartsWith("Stone_01_01") ? sandstone : n.StartsWith("Stone_01_02") ? laterite : n.StartsWith("Stone_01_03") ? carved :
                n.StartsWith("Roof_01_01") ? roof : n.StartsWith("Roof_01_02") ? laterite : n.StartsWith("Roof_01_03") ? sandstone :
                n.StartsWith("Metal") ? gold : n.StartsWith("Glass") ? glass : n.StartsWith("Wood") ? wood : cloth);
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx); imp.indexFormat = ModelImporterIndexFormat.UInt32; imp.isReadable = true; imp.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(model); PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "Temple"; int tris = 0, cols = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                tris += mf.sharedMesh.triangles.Length / 3;
                var mr = mf.GetComponent<MeshRenderer>(); string mn = mr.sharedMaterial != null ? mr.sharedMaterial.name : "";
                mf.gameObject.isStatic = true;
                if (!mn.Contains("Glass") && !mn.Contains("Flag") && !mn.Contains("Gold")) { mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; cols++; }
            }
            Directory.CreateDirectory("Assets/_Project/Resources/Temple");
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Resources/Temple/Temple.prefab"); Object.DestroyImmediate(root);
            Debug.Log($"[Batch] temple prefab built: {tris} triangles, {cols} mesh colliders");
        }
    }
}
