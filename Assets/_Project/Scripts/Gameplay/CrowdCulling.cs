using UnityEngine;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Villagers far from the camera are not drawn and not animated (each villager woman is Meera's 89,000-triangle model,
    /// so a full crowd cost more than the whole temple). Near ones are untouched.
    /// </summary>
    public sealed class CrowdCulling : MonoBehaviour
    {
        public const float DrawDistance = 55f;
        public static int Visible { get; private set; }
        float next;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { var g = new GameObject("CrowdCulling"); DontDestroyOnLoad(g); g.AddComponent<CrowdCulling>(); }

        void Update()
        {
            if (Time.unscaledTime < next) return; next = Time.unscaledTime + 0.25f;
            var vl = VillageLife.Instance; var cam = Camera.main; if (vl == null || cam == null) return;
            int vis = 0; Vector3 c = cam.transform.position;
            foreach (var t in vl.Npcs)
            {
                if (t == null) continue;
                bool on = (t.position - c).sqrMagnitude < DrawDistance * DrawDistance;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true)) if (r.enabled != on) r.enabled = on;
                var a = t.GetComponentInChildren<Animator>(); if (a != null) a.cullingMode = AnimatorCullingMode.CullCompletely;
                if (on) vis++;
            }
            Visible = vis;
        }
    }
}
