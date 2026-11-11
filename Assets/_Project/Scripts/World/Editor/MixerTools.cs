using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>
    /// Builds Resources/Audio/LotMixer.mixer from code (groups, effects, exposed parameters, music snapshots).
    /// Unity has no public API to author a mixer, so this uses the editor's internal AudioMixerController by reflection.
    /// </summary>
    public static class MixerTools
    {
        private const string Path = "Assets/_Project/Audio/Resources/Audio/LotMixer.mixer";
        private static Type T(string n) => typeof(AssetDatabase).Assembly.GetType("UnityEditor.Audio." + n);
        private const BindingFlags BF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static object Call(object o, string m, params object[] a)
        {
            var type = o as Type ?? o.GetType();
            var mi = type.GetMethods(BF).FirstOrDefault(x => x.Name == m && x.GetParameters().Length == a.Length);
            if (mi == null) throw new Exception("No method " + type.Name + "." + m + "/" + a.Length + ". Has: " + string.Join(", ", type.GetMethods(BF).Select(x => x.Name + "/" + x.GetParameters().Length).Distinct().Take(200)));
            return mi.Invoke(o is Type ? null : o, a);
        }
        private static object Get(object o, string p) { var pi = o.GetType().GetProperty(p, BF); if (pi == null) throw new Exception("No property " + o.GetType().Name + "." + p); return pi.GetValue(o); }

        private static object ctrl, snap;

        private static object Group(string name, object parent)
        {
            var g = Call(ctrl, "CreateNewGroup", name, false);
            Call(ctrl, "AddChildToParent", g, parent);
            return g;
        }

        private static object Effect(object group, string effectName, params (string p, float v)[] values)
        {
            var e = Activator.CreateInstance(T("AudioMixerEffectController"), effectName);
            Call(e, "PreallocateGUIDs");
            AssetDatabase.AddObjectToAsset((UnityEngine.Object)e, (UnityEngine.Object)ctrl);
            int count = ((Array)Get(group, "effects")).Length;
            Call(group, "InsertEffect", e, count);
            foreach (var (p, v) in values) Call(e, "SetValueForParameter", ctrl, snap, p, v);
            return e;
        }

        private static void ExposeVolume(object group, string name)
        {
            var guid = Call(group, "GetGUIDForVolume");
            var path = Activator.CreateInstance(T("AudioGroupParameterPath"), group, guid);
            Call(ctrl, "AddExposedParameter", path);
            Rename(guid, name);
        }
        private static void ExposeEffect(object group, object effect, string param, string name)
        {
            var guid = Call(effect, "GetGUIDForParameter", param);
            var path = Activator.CreateInstance(T("AudioEffectParameterPath"), group, effect, guid);
            Call(ctrl, "AddExposedParameter", path);
            Rename(guid, name);
        }
        private static void Rename(object guid, string name)
        {
            var pi = ctrl.GetType().GetProperty("exposedParameters", BF);
            var arr = (Array)pi.GetValue(ctrl);
            for (int i = 0; i < arr.Length; i++)
            {
                object ep = arr.GetValue(i);
                var gf = ep.GetType().GetField("guid"); var nf = ep.GetType().GetField("name");
                if (gf.GetValue(ep).Equals(guid)) { nf.SetValue(ep, name); arr.SetValue(ep, i); }
            }
            pi.SetValue(ctrl, arr);
        }

        public static void Build()
        {
            if (System.IO.File.Exists(Path)) AssetDatabase.DeleteAsset(Path);
            ctrl = Call(T("AudioMixerController"), "CreateMixerControllerAtPath", Path);
            snap = Get(ctrl, "TargetSnapshot");
            ((UnityEngine.Object)snap).name = "Calm";
            object master = Get(ctrl, "masterGroup");

            object music = Group("Music", master);
            string[] stems = { "Calm", "Tension", "Stealth", "Combat", "Resolve" };
            var stemGroups = stems.Select(s => Group("Stem" + s, music)).ToArray();
            object amb = Group("Ambience", master), sfx = Group("SFX", master), voice = Group("Voice", master), ui = Group("UI", master);
            object aryan = Group("VoiceAryan", voice), npc = Group("VoiceNPC", voice);
            _ = ui;

            // Aryan: high-pass, low shelf (warmth), presence, light compression. NPCs: high-pass + gentle compression.
            Effect(aryan, "Highpass", ("Cutoff freq", 80f));
            var low = Effect(aryan, "ParamEQ", ("Center freq", 170f), ("Octave range", 1.6f), ("Frequency gain", 1.26f));
            var pres = Effect(aryan, "ParamEQ", ("Center freq", 2900f), ("Octave range", 1.2f), ("Frequency gain", 1.26f));
            var comp = Effect(aryan, "Compressor", ("Threshold", -20f), ("Attack", 15f), ("Release", 120f), ("Make up gain", 2f));
            Effect(npc, "Highpass", ("Cutoff freq", 100f));
            Effect(npc, "Compressor", ("Threshold", -18f), ("Attack", 15f), ("Release", 150f), ("Make up gain", 1f));
            // one reverb per bus; the audio director moves Room / Decay per map ("reverb zones") and per vocal tier
            var vrev = Effect(voice, "SFX Reverb", ("Dry Level", 0f), ("Room", -1800f), ("Decay Time", 1.4f), ("Reflections", -2000f), ("Reverb", -1500f));
            var srev = Effect(sfx, "SFX Reverb", ("Dry Level", 0f), ("Room", -2200f), ("Decay Time", 1.2f), ("Reflections", -2400f), ("Reverb", -1800f));
            var alp = Effect(amb, "Lowpass", ("Cutoff freq", 22000f));

            ExposeVolume(master, "MasterVol"); ExposeVolume(music, "MusicVol"); ExposeVolume(amb, "AmbienceVol"); ExposeVolume(sfx, "SfxVol"); ExposeVolume(voice, "VoiceVol");
            ExposeEffect(aryan, low, "Frequency gain", "AryanLowGain"); ExposeEffect(aryan, pres, "Frequency gain", "AryanPresenceGain");
            ExposeEffect(aryan, comp, "Threshold", "AryanCompThreshold"); ExposeEffect(aryan, comp, "Make up gain", "AryanMakeUp");
            ExposeEffect(voice, vrev, "Room", "VoiceRoom"); ExposeEffect(voice, vrev, "Decay Time", "VoiceDecay");
            ExposeEffect(sfx, srev, "Room", "SfxRoom"); ExposeEffect(sfx, srev, "Decay Time", "SfxDecay");
            ExposeEffect(amb, alp, "Cutoff freq", "AmbienceCutoff");

            // music snapshots: stem volumes in dB (Calm / Tension / Stealth / Combat / Resolve)
            float[][] vol = {
                new[] { 0f, -8f, -80f, -80f, -5f },    // Calm stem
                new[] { -80f, 0f, -5f, -6f, -80f },    // Tension stem
                new[] { -80f, -14f, 0f, -80f, -80f },  // Stealth stem
                new[] { -80f, -80f, -80f, 0f, -80f },  // Combat stem
                new[] { -80f, -80f, -80f, -80f, 0f } };// Resolve stem
            for (int s = 0; s < 5; s++)
            {
                if (s > 0)
                {
                    Call(ctrl, "CloneNewSnapshotFromTarget", false);
                    snap = Get(ctrl, "TargetSnapshot");
                    ((UnityEngine.Object)snap).name = stems[s];
                }
                for (int g = 0; g < 5; g++) Call(stemGroups[g], "SetValueForVolume", ctrl, snap, vol[g][s]);
            }
            EditorUtility.SetDirty((UnityEngine.Object)ctrl);
            AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset(Path);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Path);
            int groups = mixer.FindMatchingGroups("").Length; bool ok = mixer.FindSnapshot("Combat") != null && mixer.GetFloat("MusicVol", out _);
            Debug.Log("[Batch] mixer built: groups=" + groups + " snapshotsAndParamsOk=" + ok);
        }
    }
}
