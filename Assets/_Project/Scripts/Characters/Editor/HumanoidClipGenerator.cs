using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace LegacyOfTemples.Characters.Editor
{
    public static class HumanoidClipGenerator
    {
        private const string ClipsDir = "Assets/_Project/Animations/Clips";

        [MenuItem("Tools/Legacy/Generate Humanoid Locomotion Clips")]
        public static void GenerateClips()
        {
            if (!Directory.Exists(ClipsDir))
            {
                Directory.CreateDirectory(ClipsDir);
            }

            CreateIdleClip($"{ClipsDir}/Investigator_Idle.anim");
            CreateWalkClip($"{ClipsDir}/Investigator_Walk.anim", 1.1f, 0.28f, 0.35f, 0.22f);
            CreateWalkClip($"{ClipsDir}/Investigator_Run.anim", 0.75f, 0.45f, 0.55f, 0.40f);
            CreateWalkClip($"{ClipsDir}/Investigator_Sprint.anim", 0.60f, 0.58f, 0.70f, 0.52f);
            CreateCrouchClip($"{ClipsDir}/Investigator_Crouch.anim");
            CreateJumpClip($"{ClipsDir}/Investigator_Jump.anim");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HumanoidClipGenerator] Successfully generated all 6 Humanoid locomotion clips!");
        }

        private static void CreateIdleClip(string path)
        {
            AnimationClip clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path) };
            clip.frameRate = 30;

            float duration = 2.4f;
            int frames = 72;

            Keyframe[] spineKeys = new Keyframe[frames + 1];
            Keyframe[] lArmKeys = new Keyframe[frames + 1];
            Keyframe[] rArmKeys = new Keyframe[frames + 1];

            for (int i = 0; i <= frames; i++)
            {
                float t = i * (duration / frames);
                float phase = (t / duration) * Mathf.PI * 2f;
                float breath = Mathf.Sin(phase) * 0.04f;

                spineKeys[i] = new Keyframe(t, breath);
                lArmKeys[i] = new Keyframe(t, 0.05f + breath * 0.5f);
                rArmKeys[i] = new Keyframe(t, 0.05f + breath * 0.5f);
            }

            clip.SetCurve("", typeof(Animator), "Spine Front-Back", new AnimationCurve(spineKeys));
            clip.SetCurve("", typeof(Animator), "Left Arm Down-Up", new AnimationCurve(lArmKeys));
            clip.SetCurve("", typeof(Animator), "Right Arm Down-Up", new AnimationCurve(rArmKeys));

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, path);
        }

        private static void CreateWalkClip(string path, float duration, float stride, float kneeBend, float armSwing)
        {
            AnimationClip clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path) };
            clip.frameRate = 30;

            int frames = Mathf.RoundToInt(duration * 30);

            Keyframe[] lLegKeys = new Keyframe[frames + 1];
            Keyframe[] rLegKeys = new Keyframe[frames + 1];
            Keyframe[] lKneeKeys = new Keyframe[frames + 1];
            Keyframe[] rKneeKeys = new Keyframe[frames + 1];
            Keyframe[] lArmKeys = new Keyframe[frames + 1];
            Keyframe[] rArmKeys = new Keyframe[frames + 1];
            Keyframe[] spineKeys = new Keyframe[frames + 1];

            for (int i = 0; i <= frames; i++)
            {
                float t = i * (duration / frames);
                float phase = (t / duration) * Mathf.PI * 2f;

                // Leg swing: Left and Right opposite phases
                float lSwing = Mathf.Sin(phase) * stride;
                float rSwing = -Mathf.Sin(phase) * stride;

                // Knee flex during recovery phase
                float lKnee = Mathf.Max(0f, Mathf.Cos(phase)) * kneeBend;
                float rKnee = Mathf.Max(0f, Mathf.Cos(phase + Mathf.PI)) * kneeBend;

                // Arm swing opposite to leg
                float lArm = -Mathf.Sin(phase) * armSwing;
                float rArm = Mathf.Sin(phase) * armSwing;

                // Subtle spine sway
                float spine = Mathf.Sin(phase * 2f) * 0.03f;

                lLegKeys[i] = new Keyframe(t, lSwing);
                rLegKeys[i] = new Keyframe(t, rSwing);
                lKneeKeys[i] = new Keyframe(t, lKnee);
                rKneeKeys[i] = new Keyframe(t, rKnee);
                lArmKeys[i] = new Keyframe(t, lArm);
                rArmKeys[i] = new Keyframe(t, rArm);
                spineKeys[i] = new Keyframe(t, spine);
            }

            clip.SetCurve("", typeof(Animator), "Left Upper Leg Front-Back", new AnimationCurve(lLegKeys));
            clip.SetCurve("", typeof(Animator), "Right Upper Leg Front-Back", new AnimationCurve(rLegKeys));
            clip.SetCurve("", typeof(Animator), "Left Lower Leg Stretch", new AnimationCurve(lKneeKeys));
            clip.SetCurve("", typeof(Animator), "Right Lower Leg Stretch", new AnimationCurve(rKneeKeys));
            clip.SetCurve("", typeof(Animator), "Left Arm Front-Back", new AnimationCurve(lArmKeys));
            clip.SetCurve("", typeof(Animator), "Right Arm Front-Back", new AnimationCurve(rArmKeys));
            clip.SetCurve("", typeof(Animator), "Spine Front-Back", new AnimationCurve(spineKeys));

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, path);
        }

        private static void CreateCrouchClip(string path)
        {
            AnimationClip clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path) };
            clip.frameRate = 30;

            float duration = 1.0f;
            int frames = 30;

            Keyframe[] lLegKeys = new Keyframe[frames + 1];
            Keyframe[] rLegKeys = new Keyframe[frames + 1];
            Keyframe[] lKneeKeys = new Keyframe[frames + 1];
            Keyframe[] rKneeKeys = new Keyframe[frames + 1];
            Keyframe[] spineKeys = new Keyframe[frames + 1];

            for (int i = 0; i <= frames; i++)
            {
                float t = i * (duration / frames);
                lLegKeys[i] = new Keyframe(t, 0.35f);
                rLegKeys[i] = new Keyframe(t, 0.35f);
                lKneeKeys[i] = new Keyframe(t, 0.65f);
                rKneeKeys[i] = new Keyframe(t, 0.65f);
                spineKeys[i] = new Keyframe(t, 0.25f);
            }

            clip.SetCurve("", typeof(Animator), "Left Upper Leg Front-Back", new AnimationCurve(lLegKeys));
            clip.SetCurve("", typeof(Animator), "Right Upper Leg Front-Back", new AnimationCurve(rLegKeys));
            clip.SetCurve("", typeof(Animator), "Left Lower Leg Stretch", new AnimationCurve(lKneeKeys));
            clip.SetCurve("", typeof(Animator), "Right Lower Leg Stretch", new AnimationCurve(rKneeKeys));
            clip.SetCurve("", typeof(Animator), "Spine Front-Back", new AnimationCurve(spineKeys));

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, path);
        }

        private static void CreateJumpClip(string path)
        {
            AnimationClip clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path) };
            clip.frameRate = 30;

            float duration = 0.8f;
            int frames = 24;

            Keyframe[] lLegKeys = new Keyframe[frames + 1];
            Keyframe[] rLegKeys = new Keyframe[frames + 1];
            Keyframe[] lKneeKeys = new Keyframe[frames + 1];
            Keyframe[] rKneeKeys = new Keyframe[frames + 1];
            Keyframe[] lArmKeys = new Keyframe[frames + 1];
            Keyframe[] rArmKeys = new Keyframe[frames + 1];

            for (int i = 0; i <= frames; i++)
            {
                float t = i * (duration / frames);
                float arc = Mathf.Sin((t / duration) * Mathf.PI);

                lLegKeys[i] = new Keyframe(t, arc * 0.20f);
                rLegKeys[i] = new Keyframe(t, arc * 0.20f);
                lKneeKeys[i] = new Keyframe(t, arc * 0.45f);
                rKneeKeys[i] = new Keyframe(t, arc * 0.45f);
                lArmKeys[i] = new Keyframe(t, -arc * 0.35f);
                rArmKeys[i] = new Keyframe(t, -arc * 0.35f);
            }

            clip.SetCurve("", typeof(Animator), "Left Upper Leg Front-Back", new AnimationCurve(lLegKeys));
            clip.SetCurve("", typeof(Animator), "Right Upper Leg Front-Back", new AnimationCurve(rLegKeys));
            clip.SetCurve("", typeof(Animator), "Left Lower Leg Stretch", new AnimationCurve(lKneeKeys));
            clip.SetCurve("", typeof(Animator), "Right Lower Leg Stretch", new AnimationCurve(rKneeKeys));
            clip.SetCurve("", typeof(Animator), "Left Arm Front-Back", new AnimationCurve(lArmKeys));
            clip.SetCurve("", typeof(Animator), "Right Arm Front-Back", new AnimationCurve(rArmKeys));

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, path);
        }
    }
}
