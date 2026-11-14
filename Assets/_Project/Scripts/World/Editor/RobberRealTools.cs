using System.IO;
using UnityEditor;
using UnityEngine;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>
    /// Puts the owner's two robber models from assets.blend (rigged copies made by Tools/blender/rig_robbers.py)
    /// into the Robber1 and Robber2 prefabs, replacing the old stand-ins. Humanoid rig, so the project's idle / walk / run clips play on them.
    /// </summary>
    public static class RobberRealTools
    {
        const string Dir = "Assets/_Project/Models/Characters/Robbers/";
        const string Prefabs = "Assets/_Project/Prefabs/Characters/";

        [MenuItem("Legacy of the Temple/Robbers/Use the assets.blend robbers")]
        public static void Build()
        {
            foreach (var n in new[] { "Robber1", "Robber2" }) Make(n);
            AssetDatabase.SaveAssets();
            Part2Tools.BuildNpc();   // the cinematic copies follow the prefabs
            Debug.Log("[Batch] real robbers in the prefabs");
        }

        static void Make(string n)
        {
            string fbx = Dir + n + "_Real.fbx", tex = Dir + n + "_Albedo.png";
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            if (imp == null) { Debug.LogError("[Robbers] missing " + fbx); return; }
            imp.animationType = ModelImporterAnimationType.Human; imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false; imp.materialImportMode = ModelImporterMaterialImportMode.None; imp.importCameras = false; imp.importLights = false;
            imp.SaveAndReimport();
            var ti = (TextureImporter)AssetImporter.GetAtPath(tex); if (ti != null && ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; ti.SaveAndReimport(); }
            string mp = Dir + n + "_Mat.mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, mp); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex)); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Metallic", 0.45f); m.SetFloat("_Smoothness", 0.45f); EditorUtility.SetDirty(m);

            Avatar avatar = null; foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx)) if (o is Avatar a) avatar = a;
            if (avatar == null || !avatar.isHuman) Debug.LogWarning($"[Robbers] {n}: avatar is {(avatar == null ? "missing" : "not humanoid")}");
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animations/CharacterAnimator.controller");

            var root = PrefabUtility.LoadPrefabContents(Prefabs + n + ".prefab");
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach (var an in root.GetComponents<Animator>()) Object.DestroyImmediate(an);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "VisualModel"; model.transform.SetParent(root.transform, false); model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = m;
            var anim = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            anim.avatar = avatar; anim.runtimeAnimatorController = ctrl; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var drv = root.GetComponent<NpcAnimDriver>() ?? root.AddComponent<NpcAnimDriver>(); drv.standIn = false;
            int tris = 0; foreach (var sm in model.GetComponentsInChildren<SkinnedMeshRenderer>()) tris += sm.sharedMesh.triangles.Length / 3;
            PrefabUtility.SaveAsPrefabAsset(root, Prefabs + n + ".prefab"); PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[Batch] {n}: real model from assets.blend, humanoid={(avatar != null && avatar.isHuman)}, {tris} triangles");
        }
    }
}
