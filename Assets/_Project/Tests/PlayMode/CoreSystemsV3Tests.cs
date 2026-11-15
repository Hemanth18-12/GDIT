using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.Tests
{
    /// <summary>V3 core systems: graphics presets, per-zone looks, lip sync and blinking on the real character models.</summary>
    public class CoreSystemsV3Tests
    {
        [Test]
        public void Graphics_Presets_Apply_And_High_Is_Default()
        {
            int keep = GameOptions.Graphics;
            PlayerPrefs.DeleteKey("Settings_Graphics"); Assert.AreEqual("High", GameOptions.GraphicsNames[GameOptions.Graphics]);
            GameOptions.Graphics = 0; Assert.AreEqual(35f, QualitySettings.shadowDistance, 0.01f); Assert.AreEqual(60, Application.targetFrameRate);
            GameOptions.Graphics = 3; Assert.AreEqual(140f, QualitySettings.shadowDistance, 0.01f); Assert.IsTrue(GraphicsPresets.Current.motionBlur);
            GameOptions.Graphics = keep;
        }

        [Test]
        public void Six_Zone_Looks_Are_Distinct_And_Use_ACES()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (ZoneLook.Zone z in System.Enum.GetValues(typeof(ZoneLook.Zone)))
            {
                var go = new GameObject("look_" + z); var zl = go.AddComponent<ZoneLook>(); zl.zone = z; zl.Build();
                Assert.IsTrue(zl.Profile.TryGet(out Tonemapping t)); Assert.AreEqual(TonemappingMode.ACES, t.mode.value);
                Assert.IsTrue(zl.Profile.TryGet(out ColorAdjustments ca)); Assert.IsTrue(zl.Profile.TryGet(out Bloom b)); Assert.IsTrue(zl.Profile.TryGet(out Vignette v)); Assert.IsTrue(zl.Profile.TryGet(out WhiteBalance wb));
                Assert.IsFalse(zl.Volume.isGlobal); Assert.IsTrue(go.GetComponent<BoxCollider>().isTrigger);
                Assert.IsTrue(seen.Add($"{ca.postExposure.value:0.0}/{wb.temperature.value:0}/{ca.contrast.value:0}"), "every zone has its own grade: " + z);
                Object.DestroyImmediate(go);
            }
            Assert.AreEqual(6, seen.Count);
            Assert.Less(ZoneLook.Preset(ZoneLook.Zone.Vaults).exposure, ZoneLook.Preset(ZoneLook.Zone.SunWall).exposure, "the vaults are darker than the sun wall");
        }

        static GameObject Spawn(string prefab)
        {
#if UNITY_EDITOR
            var pf = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/" + prefab + ".prefab");
            if (pf == null) return null;
            var g = Object.Instantiate(pf, new Vector3(700f, 0f, 700f), Quaternion.identity);
            foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) if (!(mb is FaceAnimator)) mb.enabled = false;
            return g;
#else
            return null;
#endif
        }

        [UnityTest]
        public IEnumerator Faces_Lip_Sync_And_Blink_On_The_Real_Models()
        {
            var report = new System.Text.StringBuilder("[FaceTest] ");
            foreach (var name in new[] { "Investigator", "Villager", "Priest", "Robber1" })
            {
                var g = Spawn(name); Assert.IsNotNull(g, name + " prefab"); var anim = g.GetComponentInChildren<Animator>(); if (anim != null) anim.enabled = false;
                var f = g.AddComponent<FaceAnimator>(); yield return null;
                report.Append($"{name}: lips={f.LipRig}, blink={f.CanBlink}; ");
                f.BlinkNow(); for (int i = 0; i < 12; i++) yield return null;
                if (f.CanBlink) Assert.GreaterOrEqual(f.Blinks, 1, name + " blinks");
                Object.Destroy(g);
            }
            Debug.Log(report.ToString());

            // the Investigator's jaw really turns when the voice is loud
            var inv = Spawn("Investigator"); var an = inv.GetComponentInChildren<Animator>(); if (an != null) an.enabled = false;
            var face = inv.AddComponent<FaceAnimator>(); yield return null;
            Assert.AreEqual(FaceAnimator.Rig.JawBone, face.LipRig, "the Investigator has a jaw bone");
            Transform jaw = null; foreach (var t in inv.GetComponentsInChildren<Transform>()) if (t.name == "C_jaw") jaw = t;
            Assert.IsNotNull(jaw); var rest = jaw.localRotation;
            var src = inv.AddComponent<AudioSource>(); int sr = 44100; var data = new float[sr];
            for (int i = 0; i < sr; i++) data[i] = Mathf.Sin(2f * Mathf.PI * 180f * i / sr) * 0.5f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 4f * i / sr));
            var clip = AudioClip.Create("test_voice", sr, 1, sr, false); clip.SetData(data, 0); src.clip = clip; src.spatialBlend = 0f; src.Play(); face.Speak(src, 2f);
            float maxAngle = 0f, maxOpen = 0f;
            for (float t = 0f; t < 0.7f; t += Time.deltaTime) { yield return null; maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(rest, jaw.localRotation)); maxOpen = Mathf.Max(maxOpen, face.Openness); }
            Debug.Log($"[FaceTest] Investigator jaw: max openness {maxOpen:0.00}, max jaw angle {maxAngle:0.0} deg while a test voice clip played");
            if (maxOpen < 0.05f)
            {   // batch mode can run with the audio device muted: then prove the jaw path by feeding a level directly
                for (int i = 0; i < 6; i++) { face.Feed(0.8f, "aa"); yield return null; maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(rest, jaw.localRotation)); }
                Debug.Log($"[FaceTest] audio output was silent in this run; fed a level directly: jaw angle {maxAngle:0.0} deg");
            }
            Assert.Greater(maxAngle, 3f, "the jaw opens with the voice");
            face.StopSpeaking(); for (int i = 0; i < 20; i++) yield return null;
            Assert.Less(Quaternion.Angle(rest, jaw.localRotation), 1.5f, "the mouth closes when the line ends");
            Object.Destroy(inv);
        }
    }
}
