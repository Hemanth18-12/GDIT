using System.Collections.Generic;
using UnityEngine;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Art pass for the V3 zones: no bare boxes. Every long wall gets a carved coping on top and a moulded plinth at its foot,
    /// and each zone gets hero props modelled in Blender (Tools/blender/build_v3_props.py): carved pillars, brass lamp stands
    /// with real light, stone arches.
    /// </summary>
    public static class ZoneDress
    {
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static Material stone, brass, darkStone;
        public static int Placed { get; private set; }

        static Mesh M(string name)
        {
            if (meshes.TryGetValue(name, out var m) && m != null) return m;
            var g = Resources.Load<GameObject>("Props/V3/" + name); m = null;
            if (g != null) { var mf = g.GetComponentInChildren<MeshFilter>(); if (mf != null) m = mf.sharedMesh; }
            if (m == null) Debug.LogWarning("[Dress] missing prop mesh " + name);
            meshes[name] = m; return m;
        }
        static Material Stone => stone != null ? stone : stone = ZoneKit.Tinted("sandstone_blocks_08", new Color(0.92f, 0.82f, 0.68f), 0.22f);
        static Material DarkStone => darkStone != null ? darkStone : darkStone = ZoneKit.Tinted("mossy_sandstone", new Color(0.75f, 0.72f, 0.66f), 0.2f);
        static Material Brass { get { if (brass == null) { brass = ZoneKit.Tinted("clay_plaster", new Color(0.78f, 0.58f, 0.24f), 0.82f); brass.SetFloat("_Metallic", 1f); } return brass; } }

        public static GameObject Prop(Transform parent, string mesh, Vector3 localPos, float yaw = 0f, Vector3? scale = null, Material mat = null, bool collider = false)
        {
            var m = M(mesh); if (m == null) return null;
            var g = new GameObject(mesh); g.transform.SetParent(parent, false); g.transform.localPosition = localPos; g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            g.transform.localScale = scale ?? Vector3.one;
            g.AddComponent<MeshFilter>().sharedMesh = m; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat ?? (mesh.Contains("Lamp") ? Brass : Stone);
            if (collider) g.AddComponent<MeshCollider>().sharedMesh = m;
            Placed++; return g;
        }

        /// <summary>A brass lamp stand with a warm flickering flame on top.</summary>
        public static void Lamp(Transform parent, Vector3 localPos, float range = 8f, float intensity = 5f)
        {
            if (Prop(parent, "SM_V3_LampStand", localPos) == null) return;
            var lg = new GameObject("LampFlame"); lg.transform.SetParent(parent, false); lg.transform.localPosition = localPos + Vector3.up * 1.62f;
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.66f, 0.3f); l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
            lg.AddComponent<LightFlicker>().Configure(intensity * 0.75f, intensity * 1.15f, 11f);
        }

        /// <summary>Coping on top and plinth at the foot of every tall wall under the root whose name starts with one of the prefixes.</summary>
        public static int Trim(Transform root, params string[] prefixes)
        {
            int n = 0; var walls = new List<Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                bool match = false; foreach (var p in prefixes) if (t.name.StartsWith(p)) match = true;
                if (!match || t.GetComponent<MeshRenderer>() == null) continue;
                Vector3 s = t.localScale; float len = Mathf.Max(s.x, s.z), thick = Mathf.Min(s.x, s.z);
                if (s.y < 2.2f || len < 1.4f || thick > 1.8f) continue;
                walls.Add(t);
            }
            foreach (var t in walls)
            {
                Vector3 s = t.localScale; bool alongX = s.x >= s.z; float len = alongX ? s.x : s.z, thick = alongX ? s.z : s.x;
                Quaternion rot = t.localRotation * (alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f));
                Vector3 topLocal = t.localPosition + t.localRotation * new Vector3(0f, s.y * 0.5f, 0f), footLocal = t.localPosition + t.localRotation * new Vector3(0f, -s.y * 0.5f, 0f);
                var c = Prop(t.parent, "SM_V3_Coping", topLocal, 0f, new Vector3(len + 0.12f, 1.2f, thick)); if (c != null) { c.transform.localRotation = rot; n++; }
                var p = Prop(t.parent, "SM_V3_Plinth", footLocal, 0f, new Vector3(len + 0.04f, 1.1f, thick), DarkStone); if (p != null) { p.transform.localRotation = rot; n++; }
            }
            return n;
        }

        /// <summary>Hide a plain box and stand a carved pillar in its place (the box keeps its collider).</summary>
        public static void PillarOver(Transform box)
        {
            if (box == null) return; var r = box.GetComponent<MeshRenderer>(); if (r != null) r.enabled = false;
            float h = box.localScale.y; var foot = box.localPosition + Vector3.down * h * 0.5f;
            Prop(box.parent, "SM_V3_Pillar", foot, 0f, new Vector3(box.localScale.x / 0.64f, h / 2.82f, box.localScale.z / 0.64f));
        }
    }
}
