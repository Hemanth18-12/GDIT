using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LegacyOfTemples.World
{
    /// <summary>
    /// Performance: the world builders tint and tile thousands of static blocks with per-object property blocks. A property block
    /// stops URP's SRP Batcher from batching that object, so every block became an expensive draw on the CPU render thread
    /// (17 to 40 ms a frame in the village). After a scene is built, this bakes each static object's colour and tiling into a
    /// shared, cached material and clears the block. Looks the same; draws far cheaper. Moving and glowing things are skipped.
    /// </summary>
    public sealed class BatchFriendly : MonoBehaviour
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        public static int Baked { get; private set; }
        public static int Materials => cache.Count;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), BaseST = Shader.PropertyToID("_BaseMap_ST"), Emission = Shader.PropertyToID("_EmissionColor");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var g = new GameObject("BatchFriendly"); DontDestroyOnLoad(g); var b = g.AddComponent<BatchFriendly>();
            SceneManager.sceneLoaded += (s, m) => b.StartCoroutine(b.Passes());
            b.StartCoroutine(b.Passes());
        }

        IEnumerator Passes()
        {   // the village, temple and zones finish building over the first seconds of a scene
            foreach (float t in new[] { 1.5f, 4f, 10f, 20f }) { yield return new WaitForSecondsRealtime(t); BakeAll(); }
        }

        static bool Skip(Renderer r)
        {
            if (!(r is MeshRenderer) || !r.HasPropertyBlock()) return true;
            for (var t = r.transform; t != null; t = t.parent)
            {
                string n = t.name;
                if (n == "Jeep" || n.StartsWith("Brawl") || n.StartsWith("Courier") || n.StartsWith("Bhaskar") || n.StartsWith("Madhav") || n.StartsWith("Villager")) return true;
                if (t.GetComponent<Rigidbody>() != null || t.GetComponent<Animator>() != null) return true;
            }
            return false;
        }

        public static int BakeAll()
        {
            int n = 0; var mpb = new MaterialPropertyBlock();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (Skip(r)) continue;
                var mats = r.sharedMaterials; if (mats.Length != 1 || mats[0] == null) continue;
                r.GetPropertyBlock(mpb);
                if (mpb.HasColor(Emission)) continue;   // lamps and sockets animate their glow
                var m = mats[0];
                Color c = mpb.HasColor(BaseColor) ? mpb.GetColor(BaseColor) : (m.HasProperty(BaseColor) ? m.GetColor(BaseColor) : Color.white);
                Vector4 st = mpb.HasVector(BaseST) ? mpb.GetVector(BaseST) : (m.HasProperty(BaseST) ? m.GetVector(BaseST) : new Vector4(1, 1, 0, 0));
                // quantise so near-identical blocks share one material
                c = new Color(Mathf.Round(c.r * 64f) / 64f, Mathf.Round(c.g * 64f) / 64f, Mathf.Round(c.b * 64f) / 64f, Mathf.Round(c.a * 32f) / 32f);
                st = new Vector4(Mathf.Round(st.x * 8f) / 8f, Mathf.Round(st.y * 8f) / 8f, Mathf.Round(st.z * 8f) / 8f, Mathf.Round(st.w * 8f) / 8f);
                string key = m.GetEntityId() + "|" + c + "|" + st;
                if (!cache.TryGetValue(key, out var baked) || baked == null)
                {
                    baked = new Material(m) { name = m.name + "_baked" };
                    if (baked.HasProperty(BaseColor)) baked.SetColor(BaseColor, c);
                    if (baked.HasProperty(BaseST)) { baked.SetVector(BaseST, st); baked.mainTextureScale = new Vector2(st.x, st.y); baked.mainTextureOffset = new Vector2(st.z, st.w); }
                    cache[key] = baked;
                }
                r.sharedMaterial = baked; r.SetPropertyBlock(null); n++;
            }
            Baked += n;
            if (n > 0) Debug.Log($"[BatchFriendly] baked {n} renderers into {cache.Count} shared materials (total {Baked})");
            return n;
        }
    }
}
