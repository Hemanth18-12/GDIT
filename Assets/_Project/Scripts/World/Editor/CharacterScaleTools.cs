using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>Measures (and optionally normalizes) character heights. Batch: -executeMethod ...Measure / ...Fix</summary>
    public static class CharacterScaleTools
    {
        public const string Dir = "Assets/_Project/Prefabs/Characters/";
        public static readonly (string prefab, string label, float target)[] Cast =
        {
            ("Investigator", "Aryan (Investigator)", 1.78f), ("Priest", "Pujari Ananda", 1.65f), ("Villager", "Meera", 1.60f),
            ("Robber1", "Bhaskar (Robber 1)", 1.75f), ("Robber2", "Kishore (Robber 2)", 1.70f),
        };

        public static Bounds MeshBounds(GameObject go)
        {
            // Real vertex extent: bake skinned meshes in their current pose (renderer.bounds is only an estimate).
            Bounds b = new Bounds(go.transform.position, Vector3.zero); bool any = false;
            var m = new Mesh();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                Vector3[] v; Matrix4x4 w;
                if (r is SkinnedMeshRenderer sk) { sk.BakeMesh(m, true); v = m.vertices; w = sk.transform.localToWorldMatrix; }
                else if (r is MeshRenderer && r.TryGetComponent(out MeshFilter f) && f.sharedMesh != null) { v = f.sharedMesh.vertices; w = r.transform.localToWorldMatrix; }
                else continue;
                foreach (var p in v) { var q = w.MultiplyPoint3x4(p); if (!any) { b = new Bounds(q, Vector3.zero); any = true; } else b.Encapsulate(q); }
            }
            Object.DestroyImmediate(m);
            return b;
        }

        public static string Describe(GameObject go)
        {
            var sb = new StringBuilder();
            go.transform.position = Vector3.zero;
            var anim = go.GetComponentInChildren<Animator>();
            if (anim != null) { anim.Rebind(); anim.Update(0f); }
            var b = MeshBounds(go);
            sb.Append($"height={b.size.y:0.000} feetOffsetFromPivot={b.min.y:0.000} topY={b.max.y:0.000}\n");
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (t.localScale != Vector3.one || t == go.transform) sb.Append($"   scale {Path(t, go.transform)} = {t.localScale.x:0.###},{t.localScale.y:0.###},{t.localScale.z:0.###}\n");
            var cc = go.GetComponent<CharacterController>();
            if (cc != null) sb.Append($"   CC h={cc.height:0.00} c={cc.center} r={cc.radius:0.00} skin={cc.skinWidth:0.00} step={cc.stepOffset:0.00} slope={cc.slopeLimit:0}\n");
            if (anim != null) sb.Append($"   Animator rootMotion={anim.applyRootMotion} avatar={(anim.avatar != null ? anim.avatar.name : "none")} on '{anim.name}'\n");
            return sb.ToString();
        }

        private static string Path(Transform t, Transform root)
        {
            string p = t.name;
            while (t != root && t.parent != null) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }

        /// <summary>Scale the MODEL child (never the CharacterController root) to the target height and put the feet on the pivot.</summary>
        public static void Fix()
        {
            var sb = new StringBuilder("# Character scale fix (before -> after)\n");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            foreach (var c in Cast)
            {
                string path = Dir + c.prefab + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                root.transform.position = Vector3.zero;
                Transform model = null; int best = -1;
                foreach (Transform ch in root.transform)
                {
                    int n = ch.GetComponentsInChildren<Renderer>(true).Length;
                    if (n > best) { best = n; model = ch; }
                }
                foreach (var a in root.GetComponentsInChildren<Animator>(true)) { a.applyRootMotion = false; a.Rebind(); a.Update(0f); }
                var before = MeshBounds(root);
                float k = c.target / before.size.y;
                model.localScale *= k;
                var mid = MeshBounds(root);
                model.localPosition += Vector3.up * (0f - mid.min.y);
                var after = MeshBounds(root);
                var cc = root.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.height = after.size.y; cc.center = new Vector3(0f, after.size.y * 0.5f + 0.03f, 0f);
                    cc.radius = 0.3f; cc.skinWidth = 0.03f; cc.stepOffset = 0.3f; cc.slopeLimit = 40f;
                }
                string modelName = model.name;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                sb.Append($"{c.label}: {before.size.y:0.00} m -> {after.size.y:0.00} m (target {c.target}), model '{modelName}' x{k:0.000}, feet {before.min.y:0.00} -> {after.min.y:0.00}\n");
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("../Docs/QA/scale_fix.txt", sb.ToString());
            Debug.Log(sb.ToString());
            LineupShot();
        }

        /// <summary>All five standing in a row on a plane, rendered to Docs/QA/scale_check.png.</summary>
        public static void LineupShot()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.localScale = Vector3.one * 3f;
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.3f; sun.transform.rotation = Quaternion.Euler(40, 160, 0);
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);
            for (int i = 0; i < Cast.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir + Cast[i].prefab + ".prefab"));
                go.transform.SetPositionAndRotation(new Vector3((i - 2) * 1.0f, 0f, 0f), Quaternion.Euler(0, 180, 0));
                foreach (var a in go.GetComponentsInChildren<Animator>()) { a.Rebind(); a.Update(0f); }
            }
            // 1.8 m reference pole on the right
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube); pole.transform.position = new Vector3(3f, 0.9f, 0f); pole.transform.localScale = new Vector3(0.05f, 1.8f, 0.05f);
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.position = new Vector3(0.5f, 1.0f, -5.5f); cam.transform.LookAt(new Vector3(0.5f, 0.95f, 0f));
            cam.fieldOfView = 40f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.65f, 0.75f);
            var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply(); RenderTexture.active = null;
            File.WriteAllBytes("../Docs/QA/scale_check.png", tex.EncodeToPNG());
        }

        public static void Measure()
        {
            var sb = new StringBuilder("# Character scale measure\n");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            foreach (var c in Cast)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + c.prefab + ".prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf);
                sb.Append($"PREFAB {c.label} target {c.target}: ").Append(Describe(go));
            }
            // Instances placed in the Courtyard scene (scene overrides).
            var sc = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Environments/Courtyard.unity");
            foreach (var root in sc.GetRootGameObjects())
                foreach (var a in root.GetComponentsInChildren<Animator>(true))
                {
                    var r = a.GetComponent<CharacterController>() != null || a.transform.parent == null ? a.transform : a.transform.parent;
                    var bb = MeshBounds(r.gameObject);
                    sb.Append($"SCENE Courtyard '{r.name}' scale={r.localScale} lossy={a.transform.lossyScale} height={bb.size.y:0.000} feet={bb.min.y - r.position.y:0.00}\n");
                }
            Directory.CreateDirectory("../Docs/QA");
            File.WriteAllText("../Docs/QA/scale_measure.txt", sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
