using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>Writes Docs/QA/scene_dump.txt: which meshes and materials each level is built from.</summary>
    public static class SceneDumpTools
    {
        public static void Dump()
        {
            var sb = new StringBuilder();
            foreach (string s in new[] { "Courtyard", "Sanctum", "ShrineCrime", "PuzzleWall", "JunglePath" })
            {
                var sc = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Environments/" + s + ".unity");
                var groups = new Dictionary<string, List<MeshRenderer>>();
                Bounds all = new Bounds(); bool any = false;
                foreach (var root in sc.GetRootGameObjects())
                    foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                        string key = (r.sharedMaterial != null ? r.sharedMaterial.name : "null") + " | mesh " + (mf.sharedMesh.name.Length > 18 ? mf.sharedMesh.name.Substring(0, 18) : mf.sharedMesh.name);
                        if (!groups.TryGetValue(key, out var l)) groups[key] = l = new List<MeshRenderer>();
                        l.Add(r);
                        if (!any) { all = r.bounds; any = true; } else all.Encapsulate(r.bounds);
                    }
                sb.Append($"\n== {s}: bounds centre {all.center} size {all.size}, roots: {string.Join(", ", sc.GetRootGameObjects().Select(g => g.name).Take(14))}\n");
                foreach (var kv in groups.OrderByDescending(k => k.Value.Count).Take(28))
                {
                    var r = kv.Value[0]; var m = r.sharedMaterial;
                    string tex = m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "-";
                    sb.Append($"  {kv.Value.Count,4} x {kv.Key} | tex {tex} | e.g. '{r.name}' size {r.bounds.size.x:0.0}x{r.bounds.size.y:0.0}x{r.bounds.size.z:0.0} tris {r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3}\n");
                }
            }
            File.WriteAllText("../Docs/QA/scene_dump.txt", sb.ToString());
            Debug.Log("[Batch] scene dump written");
        }
    }
}
