using UnityEngine;

namespace LegacyOfTemples.Characters
{
    /// <summary>Measures actual posed vertices, including FBX unit and bone scales.</summary>
    public static class CharacterMeshBounds
    {
        public static bool TryMeasure(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            var scratch = new Mesh();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.BakeMesh(scratch, true);
                    mesh = scratch;
                }
                else if (renderer.TryGetComponent<MeshFilter>(out var filter)) mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                foreach (var vertex in mesh.vertices)
                {
                    Vector3 p = root.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                    if (!found) { bounds = new Bounds(p, Vector3.zero); found = true; }
                    else bounds.Encapsulate(p);
                }
                scratch.Clear();
            }
            if (Application.isPlaying) Object.Destroy(scratch);
            else Object.DestroyImmediate(scratch);
            return found;
        }
    }
}
