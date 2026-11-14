using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>Turns the downloaded Poly Haven (CC0) models and texture sets into URP materials and prefabs:
    /// Resources/Props/&lt;name&gt;.prefab and Resources/Mats/&lt;name&gt;.mat. Re-run after Tools/assets/fetch_polyhaven.py.</summary>
    public static class PolyHavenTools
    {
        private const string Root = "Assets/_Project/Art/PolyHaven", PropDir = "Assets/_Project/Resources/Props", MatDir = "Assets/_Project/Resources/Mats";

        private static void Folder(string p) { if (!AssetDatabase.IsValidFolder(p)) { Folder(Path.GetDirectoryName(p).Replace('\\', '/')); AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\', '/'), Path.GetFileName(p)); } }

        private static Texture2D Tex(string path, bool normal, bool linear)
        {
            if (!File.Exists(path)) return null;
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return null;
            bool change = false;
            if (normal && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; change = true; }
            if (!normal && imp.sRGBTexture == linear) { imp.sRGBTexture = !linear; change = true; }
            if (imp.maxTextureSize != 2048) { imp.maxTextureSize = 2048; change = true; }
            if (change) imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Poly Haven "arm" = AO (R), roughness (G), metal (B). URP Lit wants metal (R), AO (G), smoothness (A).</summary>
        private static Texture2D Mask(string armPath)
        {
            if (!File.Exists(armPath)) return null;
            string outPath = armPath.Replace("_arm", "_mask").Replace(".jpg", ".png");
            if (!File.Exists(outPath))
            {
                var src = new Texture2D(2, 2, TextureFormat.RGBA32, false, true); src.LoadImage(File.ReadAllBytes(armPath));
                var px = src.GetPixels32();
                for (int i = 0; i < px.Length; i++) { var c = px[i]; px[i] = new Color32(c.b, c.r, 0, (byte)(255 - c.g)); }
                var dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, true); dst.SetPixels32(px);
                File.WriteAllBytes(outPath, dst.EncodeToPNG());
                Object.DestroyImmediate(src); Object.DestroyImmediate(dst);
                AssetDatabase.ImportAsset(outPath);
            }
            return Tex(outPath, false, true);
        }

        private static Material Make(string matPath, string diff, string nor, string arm, float tiling)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, matPath); }
            var d = Tex(diff, false, false); var n = Tex(nor, true, true); var k = Mask(arm);
            m.SetColor("_BaseColor", Color.white);
            if (d != null) m.SetTexture("_BaseMap", d);
            if (n != null) { m.SetTexture("_BumpMap", n); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
            if (k != null) { m.SetTexture("_MetallicGlossMap", k); m.SetTexture("_OcclusionMap", k); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.EnableKeyword("_OCCLUSIONMAP"); m.SetFloat("_Smoothness", 1f); m.SetFloat("_Metallic", 1f); m.SetFloat("_OcclusionStrength", 1f); }
            m.SetTextureScale("_BaseMap", Vector2.one * tiling);
            bool alpha = diff.EndsWith(".png") && d != null && ((TextureImporter)AssetImporter.GetAtPath(diff)).DoesSourceTextureHaveAlpha();
            if (alpha) { m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.4f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cull", 0f); m.renderQueue = 2450; m.doubleSidedGI = true; }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Warm emissive material for the village's lit windows.</summary>
        public static void BuildWindowGlow()
        {
            const string path = "Assets/_Project/Resources/Mats/window_glow.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", new Color(1f, 0.72f, 0.38f));
            m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetColor("_EmissionColor", new Color(1f, 0.62f, 0.28f) * 3.2f);
            m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m); AssetDatabase.SaveAssets();
            Debug.Log("[Batch] window_glow material written");
        }

        /// <summary>Painted leaf-mass texture and material for tree crowns and bushes (own work).</summary>
        public static void BuildFoliage()
        {
            const int N = 512; var px = new Color[N * N]; var rng = new System.Random(77);
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0.16f, 0.24f, 0.11f);
            for (int k = 0; k < 2600; k++)
            {   // overlapping leaves, darker first then lighter on top
                float t = k / 2600f; int cx = rng.Next(N), cy = rng.Next(N); float ang = (float)rng.NextDouble() * Mathf.PI, len = 9f + (float)rng.NextDouble() * 9f, wid = len * 0.42f;
                float shade = 0.55f + 0.6f * t * (0.6f + 0.4f * (float)rng.NextDouble()); var col = new Color(0.30f * shade, 0.52f * shade, 0.20f * shade);
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang); int rad = Mathf.CeilToInt(len);
                for (int y = -rad; y <= rad; y++) for (int x = -rad; x <= rad; x++)
                {
                    float u = (x * ca + y * sa) / len, v = (-x * sa + y * ca) / wid; float d = u * u + v * v; if (d > 1f) continue;
                    int ix = ((cx + x) % N + N) % N, iy = ((cy + y) % N + N) % N;
                    float vein = Mathf.Abs(v) < 0.08f ? 0.82f : 1f, edge = d > 0.8f ? 0.8f : 1f;
                    px[iy * N + ix] = new Color(col.r * vein * edge, col.g * vein * edge, col.b * vein * edge);
                }
            }
            var tex = new Texture2D(N, N, TextureFormat.RGB24, false); tex.SetPixels(px); tex.Apply();
            const string tp = "Assets/_Project/Art/FX/foliage.png", mp = "Assets/_Project/Resources/Mats/foliage.mat";
            System.IO.File.WriteAllBytes(tp, tex.EncodeToPNG()); AssetDatabase.ImportAsset(tp);
            var imp = (TextureImporter)AssetImporter.GetAtPath(tp); imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = true; imp.SaveAndReimport();
            var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, mp); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp)); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.12f); m.enableInstancing = true;
            EditorUtility.SetDirty(m); AssetDatabase.SaveAssets();
            Debug.Log("[Batch] foliage texture and material written");
        }

        [MenuItem("Legacy of the Temple/Rebuild Poly Haven props and materials")]
        public static void Build()
        {
            Folder(PropDir); Folder(MatDir);
            int mats = 0, props = 0; var report = new System.Text.StringBuilder();
            foreach (string dir in Directory.GetDirectories(Root + "/Textures"))
            {
                string n = Path.GetFileName(dir), b = dir.Replace('\\', '/') + "/" + n;
                Make(MatDir + "/" + n + ".mat", b + "_diff.jpg", b + "_nor_gl.jpg", b + "_arm.jpg", 1f); mats++;
            }
            foreach (string dir0 in Directory.GetDirectories(Root + "/Models"))
            {
                string dir = dir0.Replace('\\', '/'), n = Path.GetFileName(dir), fbx = dir + "/" + n + ".fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                if (model == null) { report.Append("no fbx " + n + "; "); continue; }
                // texture sets in this folder: "<prefix>_diff_1k.jpg|png"
                var sets = Directory.GetFiles(dir).Select(f => f.Replace('\\', '/')).Where(f => (f.EndsWith(".jpg") || f.EndsWith(".png")) && f.Contains("_diff_"))
                    .ToDictionary(f => Path.GetFileName(f).Substring(0, Path.GetFileName(f).IndexOf("_diff_")), f => f);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                var root = new GameObject(n); inst.transform.SetParent(root.transform, false);
                var made = new Dictionary<string, Material>();
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                {
                    var ms = r.sharedMaterials;
                    for (int i = 0; i < ms.Length; i++)
                    {
                        string mn = ms[i] != null ? ms[i].name : n;
                        string key = sets.Keys.OrderByDescending(k => k.Length).FirstOrDefault(k => mn.Contains(k) || k.Contains(mn)) ?? (sets.Count > 0 ? sets.Keys.OrderBy(k => k.Length).First() : null);
                        if (key == null) continue;
                        if (!made.TryGetValue(key, out var mat))
                        {
                            string diff = sets[key], ext = Path.GetExtension(diff), res = diff.Substring(diff.LastIndexOf("_diff_") + 6).Replace(ext, "");
                            string nor = dir + "/" + key + "_nor_gl_" + res + ".jpg", arm = dir + "/" + key + "_arm_" + res + ".jpg";
                            mat = Make(dir + "/" + key + ".mat", diff, nor, arm, 1f); made[key] = mat;
                        }
                        ms[i] = mat;
                    }
                    r.sharedMaterials = ms;
                }
                var bounds = new Bounds(); bool any = false; int tris = 0;
                foreach (var r in inst.GetComponentsInChildren<Renderer>()) { if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds); }
                foreach (var mf in inst.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
                // pivot: centre on the ground
                inst.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                PrefabUtility.SaveAsPrefabAsset(root, PropDir + "/" + n + ".prefab");
                Object.DestroyImmediate(root);
                report.Append($"{n}: {bounds.size.x:0.00}x{bounds.size.y:0.00}x{bounds.size.z:0.00} m, {tris} tris, {made.Count} mat; ");
                props++;
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("../Docs/QA/polyhaven_import.txt", report.ToString().Replace("; ", "\n"));
            Debug.Log("[Batch] Poly Haven: " + mats + " tiling materials, " + props + " prop prefabs");
        }
    }
}
