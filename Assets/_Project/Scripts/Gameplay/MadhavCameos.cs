using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// Madhav, the key-keeper (the existing male villager model), is in every place before the reveal, doing something
    /// ordinary that fits the evidence in hindsight: serving tea and wiping his hands, standing by the storeroom with his
    /// keys, holding a lantern at the vaults, watching from the stepwell rim and the Sun Wall plaza. He never fights and
    /// never gives an answer; at three places he says one line.
    /// </summary>
    public sealed class MadhavCameos : MonoBehaviour
    {
        public static MadhavCameos Instance { get; private set; }
        public Transform Madhav { get; private set; }
        public string Where { get; private set; } = "";
        string spokenAt = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded += (s, m) => { if (s.name == Constants.Courtyard) Spawn(); }; if (SceneManager.GetActiveScene().name == Constants.Courtyard) Spawn(); }
        static void Spawn() { if (Instance == null) new GameObject("V3_Madhav").AddComponent<MadhavCameos>(); }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Make()
        {
            var pf = Resources.Load<GameObject>("NPC/Villager_Male_NPC"); if (pf == null) return;
            var g = Instantiate(pf, transform); g.name = "Madhav_Cameo";
            foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;   // the animator keeps him breathing in his idle
            foreach (var c in g.GetComponentsInChildren<Collider>()) Destroy(c);
            // the only male villager model shares the priest's body, so Madhav is set apart clearly: an indigo kurta and a red turban
            var mpb = new MaterialPropertyBlock(); foreach (var r in g.GetComponentsInChildren<Renderer>()) { r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", new Color(0.42f, 0.48f, 0.78f)); r.SetPropertyBlock(mpb); }
            Transform head = null; foreach (var t in g.GetComponentsInChildren<Transform>()) if (t.name == "Head" || t.name.EndsWith(":Head")) { head = t; break; }
            var turbanParent = head != null ? head : g.transform;
            var turban = ZoneKit.Round(turbanParent, "Turban", Vector3.zero, Vector3.one, ZoneKit.Tinted("clay_plaster", new Color(0.72f, 0.12f, 0.08f), 0.15f));
            turban.transform.position = (head != null ? head.position + Vector3.up * 0.13f : g.transform.position + Vector3.up * 1.78f); turban.transform.rotation = g.transform.rotation;
            turban.transform.localScale = Vector3.one; turban.transform.localScale = new Vector3(0.24f / turban.transform.lossyScale.x, 0.07f / turban.transform.lossyScale.y, 0.24f / turban.transform.lossyScale.z);
            var shawl = ZoneKit.Part(g.transform, "Shawl", new Vector3(0f, 1.38f, 0f), new Vector3(0.5f, 0.06f, 0.32f), ZoneKit.Tinted("clay_plaster", new Color(0.85f, 0.75f, 0.45f), 0.1f), false);
            shawl.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var keys = ZoneKit.Round(g.transform, "KeyRing", new Vector3(0.22f, 0.98f, 0.05f), new Vector3(0.14f, 0.01f, 0.14f), ZoneKit.Tinted("clay_plaster", new Color(0.75f, 0.6f, 0.25f), 0.85f));
            keys.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Madhav = g.transform;
        }

        /// <summary>Where he stands for the current story step (null = not on stage).</summary>
        (Vector3 pos, Vector3 look, string line)? Spot(string obj)
        {
            switch (obj)
            {
                case "obj_v3_bazaar":
                    var vl = VillageLife.Instance; if (vl == null) return null;
                    return (vl.TeaStallStand + new Vector3(-1.8f, 0f, 1.2f), vl.TeaStallStand + Vector3.right * 4f, "v3:madhav_tea");
                case "obj_v3_storeroom":
                    var z2 = BreakInPuzzle.Instance; if (z2 == null || !z2.Built) return null;
                    return (z2.transform.TransformPoint(new Vector3(5.5f, 0f, 5f)), z2.transform.TransformPoint(new Vector3(0f, 1f, 0f)), "v3:madhav_lock");
                case "obj_v3_vaults":
                    var z3 = VaultPuzzle.Instance; if (z3 == null || !z3.Built) return null;
                    return (z3.transform.TransformPoint(new Vector3(-14.5f, 0f, 2.2f)), z3.transform.TransformPoint(new Vector3(-12.3f, 1f, 0f)), "v3:madhav_lantern");
                case "obj_v3_stepwell":
                    var z4 = StepwellPuzzle.Instance; if (z4 == null || !z4.Built) return null;
                    return (z4.transform.TransformPoint(new Vector3(-9.5f, 0f, -10.5f)), z4.transform.TransformPoint(Vector3.zero), null);
                case "obj_v3_sunwall":
                    var z5 = SunWallPuzzle.Instance; if (z5 == null || !z5.Built) return null;
                    return (z5.transform.TransformPoint(new Vector3(11.5f, 0f, -15f)), z5.transform.TransformPoint(new Vector3(0f, 1f, 4f)), null);
            }
            return null;
        }

        void Update()
        {
            var st = StoryDialogueSystem.Instance; string obj = st != null && st.Current != null ? st.Current.id : "";
            var spot = Spot(obj);
            if (spot == null) { if (Madhav != null && Madhav.gameObject.activeSelf) Madhav.gameObject.SetActive(false); Where = ""; return; }
            if (Madhav == null) Make(); if (Madhav == null) return;
            if (Where != obj)
            {   // he is somewhere new: put him on the ground there, looking at the place
                Where = obj; Vector3 p = spot.Value.pos;
                if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out RaycastHit h, 8f, ~0, QueryTriggerInteraction.Ignore)) p.y = h.point.y;
                Vector3 look = spot.Value.look - p; look.y = 0f;
                Madhav.SetPositionAndRotation(p, Quaternion.LookRotation(look.sqrMagnitude > 0.01f ? look : Vector3.forward));
                Madhav.gameObject.SetActive(true);
                Debug.Log($"[Madhav] on stage for {obj} at {p:0.0}");
            }
            var pc = ZoneKit.Player;
            if (spot.Value.line != null && spokenAt != obj && pc != null && Vector3.Distance(pc.transform.position, Madhav.position) < 7f)
            {   // one short line, once per place, as the player passes
                spokenAt = obj; st?.Say(spot.Value.line);
                Vector3 to = pc.transform.position - Madhav.position; to.y = 0f; if (to.sqrMagnitude > 0.01f) Madhav.rotation = Quaternion.LookRotation(to);
            }
        }
    }
}
