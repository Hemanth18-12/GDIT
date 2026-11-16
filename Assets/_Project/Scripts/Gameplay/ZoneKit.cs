using System.Collections.Generic;
using UnityEngine;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Shared helpers for the V3 zones (textured parts, reach triggers, clear-ground search, New Game reset, zone sky and sun).</summary>
    public static class ZoneKit
    {
        public static Material Tinted(string source, Color c, float smooth, Color? emission = null)
        {
            var src = ArtUpgrade.Mat(source); var m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth);
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            return m;
        }
        public static Material Glass(Color c, float smooth)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Surface", 1f); m.SetOverrideTag("RenderType", "Transparent"); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; return m;
        }
        /// <summary>A trigger sphere so E can reach a part without it blocking the player.</summary>
        public static void Reach(GameObject g, float worldRadius)
        {
            var s = g.AddComponent<SphereCollider>(); s.isTrigger = true;
            var ls = g.transform.lossyScale; s.radius = worldRadius / Mathf.Max(0.001f, Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z)));
        }
        public static GameObject Part(Transform parent, string name, Vector3 localPos, Vector3 size, Material m, bool collider = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = localPos; g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = m; if (!collider) Object.Destroy(g.GetComponent<Collider>()); return g;
        }
        public static GameObject Round(Transform parent, string name, Vector3 localPos, Vector3 size, Material m, bool collider = false)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = localPos; g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = m; if (!collider) Object.Destroy(g.GetComponent<Collider>()); return g;
        }
        /// <summary>First candidate spot whose box is free of buildings, trees and the temple. Returns false if none is.</summary>
        public static bool ClearGround(IEnumerable<(Vector3 at, Quaternion face)> candidates, Vector3 halfSize, out Vector3 at, out Quaternion face)
        {
            at = Vector3.zero; face = Quaternion.identity;
            foreach (var c in candidates)
            {
                at = c.at; face = c.face;
                if (!Physics.CheckBox(at + Vector3.up * (halfSize.y + 0.5f), halfSize, face, ~0, QueryTriggerInteraction.Ignore)) return true;
            }
            return false;
        }
        public static PlayerController Player => Object.FindAnyObjectByType<PlayerController>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { StoryDialogueSystem.OnNewGame -= NewGame; StoryDialogueSystem.OnNewGame += NewGame; }
        /// <summary>New Game: every V3 puzzle, score and checkpoint starts again (the high-score table stays).</summary>
        public static void NewGame() { PuzzleSave.Clear(); ScoreSystem.ResetRun(); Checkpoints.Clear(); Debug.Log("[V3] new game: puzzles, run score and checkpoint cleared"); }
    }

    /// <summary>
    /// Each zone has its own time of day. While the player stands inside a zone's look volume, the sun and fog blend to that zone's
    /// values (midday at the Sun Wall, night at the stepwell, dark in the vaults); outside every zone they blend back to the scene's own.
    /// </summary>
    public sealed class ZoneAtmosphere : MonoBehaviour
    {
        public static ZoneAtmosphere Instance { get; private set; }
        public ZoneLook Current { get; private set; }
        readonly List<ZoneLook> looks = new List<ZoneLook>(); readonly Dictionary<ZoneLook, Quaternion> sunAim = new Dictionary<ZoneLook, Quaternion>();
        Light sun; float baseIntensity, baseFog; Color baseColor, baseFogColor; Quaternion baseRot; bool baseFogOn, got;

        public static void Register(ZoneLook look, Quaternion sunRotation)
        {
            if (Instance == null) Instance = new GameObject("ZoneAtmosphere").AddComponent<ZoneAtmosphere>();
            Instance.looks.Add(look); Instance.sunAim[look] = sunRotation;
        }
        void Grab()
        {
            sun = RenderSettings.sun; if (sun == null) foreach (var l in FindObjectsByType<Light>()) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null) return;
            got = true; baseIntensity = sun.intensity; baseColor = sun.color; baseRot = sun.transform.rotation; baseFogOn = RenderSettings.fog; baseFog = RenderSettings.fogDensity; baseFogColor = RenderSettings.fogColor;
        }
        void Update()
        {
            if (!got) { Grab(); if (!got) return; }
            var cam = Camera.main; if (cam == null) return;
            ZoneLook inside = null;
            foreach (var l in looks) if (l != null && Mathf.Abs(l.transform.InverseTransformPoint(cam.transform.position).x) < l.size.x * 0.5f && Mathf.Abs(l.transform.InverseTransformPoint(cam.transform.position).y) < l.size.y * 0.5f && Mathf.Abs(l.transform.InverseTransformPoint(cam.transform.position).z) < l.size.z * 0.5f) { inside = l; break; }
            Current = inside; float k = 1f - Mathf.Exp(-Time.deltaTime * 1.6f);
            float ti = baseIntensity, tf = baseFogOn ? baseFog : 0f; Color tc = baseColor, tfc = baseFogColor; Quaternion tr = baseRot;
            if (inside != null) { var p = ZoneLook.Preset(inside.zone); ti = p.sunIntensity; tc = p.sunIntensity > 0f ? p.sun : baseColor; tf = p.fogDensity; tfc = p.fog; tr = sunAim[inside]; }
            sun.intensity = Mathf.Lerp(sun.intensity, ti, k); sun.color = Color.Lerp(sun.color, tc, k); sun.transform.rotation = Quaternion.Slerp(sun.transform.rotation, tr, k);
            RenderSettings.fog = true; RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, tf, k); RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, tfc, k);
        }
    }
}
