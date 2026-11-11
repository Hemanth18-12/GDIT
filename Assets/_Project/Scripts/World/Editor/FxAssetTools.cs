using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>Generates the small particle textures and materials used by weather, light shafts and combat FX
    /// (own work, no downloads). Materials live in Resources/FX so their shaders are included in builds.</summary>
    public static class FxAssetTools
    {
        private const string TexDir = "Assets/_Project/Art/FX", MatDir = "Assets/_Project/Resources/FX";

        [MenuItem("Legacy of the Temple/Rebuild FX materials")]
        public static void Build()
        {
            Directory.CreateDirectory(TexDir); Directory.CreateDirectory(MatDir);
            AssetDatabase.Refresh();
            var streak = Tex("fx_streak", 16, 64, (u, v) => Mathf.Pow(1f - Mathf.Abs(u - 0.5f) * 2f, 2f) * Mathf.Sin(v * Mathf.PI));
            var dot = Tex("fx_soft", 64, 64, (u, v) => Mathf.Pow(Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), Vector2.one * 0.5f) * 2f), 2f));
            var ring = Tex("fx_ring", 64, 64, (u, v) => { float d = Vector2.Distance(new Vector2(u, v), Vector2.one * 0.5f) * 2f; return Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) * 9f); });
            var shaft = Tex("fx_shaft", 64, 64, (u, v) => Mathf.Pow(1f - Mathf.Abs(u - 0.5f) * 2f, 1.5f) * Mathf.Pow(v, 0.7f) * (1f - Mathf.Pow(v, 6f)));
            Particle("Rain", streak, new Color(0.78f, 0.84f, 0.95f, 0.5f), false);
            Particle("Mist", dot, new Color(0.75f, 0.8f, 0.88f, 0.08f), false);
            Particle("Splash", dot, new Color(0.85f, 0.9f, 1f, 0.55f), false);
            Particle("Ripple", ring, new Color(0.85f, 0.9f, 1f, 0.45f), false);
            Particle("Drip", streak, new Color(0.8f, 0.86f, 0.95f, 0.7f), false);
            Particle("Shaft", shaft, new Color(1f, 0.85f, 0.6f, 0.16f), true);
            Particle("Glow", dot, new Color(1f, 0.7f, 0.3f, 0.5f), true);
            Particle("Ember", dot, new Color(1f, 0.55f, 0.15f, 0.9f), true);
            Particle("Spark", dot, new Color(1f, 0.95f, 0.8f, 1f), true);
            Particle("Dust", dot, new Color(0.7f, 0.62f, 0.5f, 0.3f), false);
            Particle("Thread", dot, new Color(0.9f, 0.9f, 1f, 0.9f), true);
            var puddle = Mat("Puddle", "Universal Render Pipeline/Lit");
            Transparent(puddle, false);
            puddle.SetColor("_BaseColor", new Color(0.05f, 0.06f, 0.08f, 0.5f)); puddle.SetFloat("_Smoothness", 0.96f); puddle.SetFloat("_Metallic", 0.1f);
            var grass = Mat("Grass", "Universal Render Pipeline/Lit");
            grass.SetColor("_BaseColor", new Color(0.16f, 0.32f, 0.14f, 1f)); grass.SetFloat("_Smoothness", 0.1f); grass.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(puddle); EditorUtility.SetDirty(grass);
            AssetDatabase.SaveAssets();
            Debug.Log("[Batch] FX materials built");
        }

        private static Texture2D Tex(string name, int w, int h, System.Func<float, float, float> alpha)
        {
            string p = TexDir + "/" + name + ".png";
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(alpha((x + 0.5f) / w, (y + 0.5f) / h))));
            File.WriteAllBytes(p, t.EncodeToPNG());
            AssetDatabase.ImportAsset(p);
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        private static Material Mat(string name, string shader)
        {
            string p = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, p); }
            return m;
        }

        private static void Transparent(Material m, bool additive)
        {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void Particle(string name, Texture2D tex, Color c, bool additive)
        {
            var m = Mat(name, "Universal Render Pipeline/Particles/Unlit");
            Transparent(m, additive);
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", c);
            m.SetFloat("_SoftParticlesEnabled", 0f);
            EditorUtility.SetDirty(m);
        }
    }
}
