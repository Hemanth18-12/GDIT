using UnityEditor;
using UnityEngine;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>
    /// The originals contain no human robber models (only an armoured king and a winged boss sculpt).
    /// Until the real Bhaskar/Kishore models are supplied, the robber prefabs use a rigged COPY of the
    /// detective model, tinted, with a red scarf + blade (Bhaskar) or black hood + staff (Kishore).
    /// Clearly flagged as stand-ins (NpcAnimDriver.standIn, Docs/BLOCKED.md). Originals are untouched.
    /// </summary>
    public static class RobberStandInTools
    {
        private const string Dir = "Assets/_Project/Prefabs/Characters/";
        private const string MatDir = "Assets/_Project/Materials/StandIn";

        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials")) AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/_Project/Materials", "StandIn");
            Make("Robber1", 1.75f, new Color(0.62f, 0.42f, 0.36f), true);
            Make("Robber2", 1.70f, new Color(0.30f, 0.30f, 0.34f), false);
            AssetDatabase.SaveAssets();
            Debug.Log("[StandIn] Robber stand-ins built");
        }

        private static Material Flat(string name, Color c)
        {
            string p = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void Make(string name, float height, Color tint, bool bhaskar)
        {
            var hero = PrefabUtility.LoadPrefabContents(Dir + "Investigator.prefab");
            var root = PrefabUtility.LoadPrefabContents(Dir + name + ".prefab");
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach (var a in root.GetComponents<Animator>()) Object.DestroyImmediate(a);
            var src = hero.transform.Find("VisualModel");
            var model = Object.Instantiate(src.gameObject, root.transform);
            model.name = "VisualModel";
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = src.localScale * (height / 1.78f);

            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name.ToLowerInvariant();
                if (n.Contains("hat") || n.Contains("glasses") || n.Contains("paper")) { r.gameObject.SetActive(false); continue; }
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string p = MatDir + "/" + name + "_" + mats[i].name + ".mat";
                    var copy = AssetDatabase.LoadAssetAtPath<Material>(p);
                    if (copy == null) { copy = new Material(mats[i]); AssetDatabase.CreateAsset(copy, p); }
                    bool skin = mats[i].name.ToLowerInvariant().Contains("head") || mats[i].name.ToLowerInvariant().Contains("eye");
                    if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", skin ? Color.white : tint);
                    EditorUtility.SetDirty(copy);
                    mats[i] = copy;
                }
                r.sharedMaterials = mats;
            }

            var anim = model.GetComponent<Animator>();
            anim.applyRootMotion = false; anim.Rebind();
            Transform head = anim.GetBoneTransform(HumanBodyBones.Head), neck = anim.GetBoneTransform(HumanBodyBones.Neck), hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (neck == null) neck = head;
            if (bhaskar)
            {
                if (neck != null) Prop(PrimitiveType.Sphere, "RedScarf", neck, new Vector3(0, 0.02f, 0), new Vector3(0.26f, 0.12f, 0.26f), Flat("RedScarf", new Color(0.64f, 0.10f, 0.08f)));
                if (hand != null)
                {
                    var blade = Prop(PrimitiveType.Cube, "CurvedBlade", hand, new Vector3(0, 0.05f, 0.32f), new Vector3(0.02f, 0.06f, 0.62f), Flat("Blade", new Color(0.75f, 0.75f, 0.78f)));
                    blade.GetComponent<Renderer>().sharedMaterial.SetFloat("_Metallic", 0.9f);
                    Prop(PrimitiveType.Cube, "BladeTip", blade.transform, new Vector3(0, 0.6f, 0.45f), new Vector3(1f, 1f, 0.25f), Flat("Blade", Color.white), Quaternion.Euler(-25, 0, 0), true);
                }
                var spine = anim.GetBoneTransform(HumanBodyBones.Spine);
                if (spine != null) Prop(PrimitiveType.Cube, "Satchel", spine, new Vector3(-0.22f, 0f, -0.12f), new Vector3(0.14f, 0.3f, 0.34f), Flat("Satchel", new Color(0.33f, 0.22f, 0.12f)));
            }
            else
            {
                if (head != null) Prop(PrimitiveType.Sphere, "BlackHood", head, new Vector3(0, 0.07f, -0.03f), new Vector3(0.27f, 0.28f, 0.29f), Flat("BlackHood", new Color(0.05f, 0.05f, 0.06f)));
                if (hand != null) Prop(PrimitiveType.Cylinder, "Staff", hand, new Vector3(0, 0.05f, 0.2f), new Vector3(0.035f, 0.7f, 0.035f), Flat("Staff", new Color(0.36f, 0.24f, 0.13f)), Quaternion.Euler(90, 0, 0));
            }
            var drv = root.GetComponent<NpcAnimDriver>(); if (drv == null) drv = root.AddComponent<NpcAnimDriver>();
            drv.standIn = true;
            PrefabUtility.SaveAsPrefabAsset(root, Dir + name + ".prefab");
            PrefabUtility.UnloadPrefabContents(root);
            PrefabUtility.UnloadPrefabContents(hero);
        }

        private static GameObject Prop(PrimitiveType t, string name, Transform bone, Vector3 worldOffset, Vector3 worldScale, Material m, Quaternion? rot = null, bool local = false)
        {
            var g = GameObject.CreatePrimitive(t); g.name = name;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(bone, false);
            if (local) { g.transform.localPosition = worldOffset; g.transform.localScale = worldScale; g.transform.localRotation = rot ?? Quaternion.identity; }
            else
            {
                g.transform.position = bone.position + worldOffset;
                g.transform.rotation = rot ?? Quaternion.identity;
                Vector3 ls = bone.lossyScale;
                g.transform.localScale = new Vector3(worldScale.x / ls.x, worldScale.y / ls.y, worldScale.z / ls.z);
            }
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }
    }
}
