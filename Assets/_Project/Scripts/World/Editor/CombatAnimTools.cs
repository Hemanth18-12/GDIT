using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>
    /// Fight animations from Open Kit 3D (CC0, Humanoid muscle clips: one-hand sword idle / walk / run / slash / thrust / hit /
    /// block / fall and three sword combo swings). Builds RobberCombat.controller for the two robbers and adds strike, block and
    /// hit states to the investigator's CharacterAnimator.
    /// </summary>
    public static class CombatAnimTools
    {
        const string Clips = "Assets/_Project/Animations/Clips/OpenKit3D/";
        static AnimationClip C(string n) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips + n + ".anim");

        static void Loop(string n, bool on)
        {
            var c = C(n); if (c == null) return;
            var s = AnimationUtility.GetAnimationClipSettings(c); if (s.loopTime == on) return; s.loopTime = on; AnimationUtility.SetAnimationClipSettings(c, s); EditorUtility.SetDirty(c);
        }

        [MenuItem("Legacy of the Temple/Combat/Build fight animators")]
        public static void Build()
        {
            foreach (var n in new[] { "OHS_Idle", "OHS_Walk", "OHS_Run", "OHS_BlockHold" }) Loop(n, true);
            foreach (var n in new[] { "OHS_Slash", "OHS_Thrust", "OHS_Hit", "OHS_Death", "Sword_Regular_A", "Sword_Regular_B", "Sword_Regular_C" }) Loop(n, false);

            // ---- robbers
            const string path = "Assets/_Project/Animations/RobberCombat.controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (var (p, t) in new[] { ("Speed", AnimatorControllerParameterType.Float), ("MoveX", AnimatorControllerParameterType.Float), ("MoveZ", AnimatorControllerParameterType.Float),
                ("IsMoving", AnimatorControllerParameterType.Bool), ("IsRunning", AnimatorControllerParameterType.Bool), ("IsCrouching", AnimatorControllerParameterType.Bool), ("Crouch", AnimatorControllerParameterType.Bool),
                ("Grounded", AnimatorControllerParameterType.Bool), ("Block", AnimatorControllerParameterType.Bool), ("Down", AnimatorControllerParameterType.Bool),
                ("Slash", AnimatorControllerParameterType.Trigger), ("Thrust", AnimatorControllerParameterType.Trigger), ("Combo", AnimatorControllerParameterType.Trigger), ("Hit", AnimatorControllerParameterType.Trigger) })
                ac.AddParameter(p, t);
            var sm = ac.layers[0].stateMachine;
            var idle = sm.AddState("Idle"); idle.motion = C("OHS_Idle"); sm.defaultState = idle;
            var walk = sm.AddState("Walk"); walk.motion = C("OHS_Walk");
            var run = sm.AddState("Run"); run.motion = C("OHS_Run");
            void T(AnimatorState a, AnimatorState b, string p, bool v) { var tr = a.AddTransition(b); tr.hasExitTime = false; tr.duration = 0.15f; tr.AddCondition(v ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, p); }
            T(idle, walk, "IsMoving", true); T(walk, idle, "IsMoving", false); T(walk, run, "IsRunning", true); T(run, walk, "IsRunning", false); T(run, idle, "IsMoving", false);
            AnimatorState Shot(string name, string clip, string trigger)
            {
                var s = sm.AddState(name); s.motion = C(clip);
                var a = sm.AddAnyStateTransition(s); a.hasExitTime = false; a.duration = 0.08f; a.canTransitionToSelf = false; a.AddCondition(AnimatorConditionMode.If, 0, trigger);
                var back = s.AddTransition(idle); back.hasExitTime = true; back.exitTime = 0.92f; back.duration = 0.15f;
                return s;
            }
            Shot("Slash", "OHS_Slash", "Slash"); Shot("Thrust", "OHS_Thrust", "Thrust"); Shot("Combo", "Sword_Regular_C", "Combo"); Shot("Hit", "OHS_Hit", "Hit");
            var block = sm.AddState("Block"); block.motion = C("OHS_BlockHold");
            var tb = sm.AddAnyStateTransition(block); tb.hasExitTime = false; tb.duration = 0.1f; tb.canTransitionToSelf = false; tb.AddCondition(AnimatorConditionMode.If, 0, "Block");
            var tbx = block.AddTransition(idle); tbx.hasExitTime = false; tbx.duration = 0.15f; tbx.AddCondition(AnimatorConditionMode.IfNot, 0, "Block");
            var down = sm.AddState("Down"); down.motion = C("OHS_Death");
            var td = sm.AddAnyStateTransition(down); td.hasExitTime = false; td.duration = 0.1f; td.canTransitionToSelf = false; td.AddCondition(AnimatorConditionMode.If, 0, "Down");
            var tdx = down.AddTransition(idle); tdx.hasExitTime = false; tdx.duration = 0.3f; tdx.AddCondition(AnimatorConditionMode.IfNot, 0, "Down");
            EditorUtility.SetDirty(ac);
            foreach (var n in new[] { "Robber1", "Robber2" })
            {
                string pp = "Assets/_Project/Prefabs/Characters/" + n + ".prefab"; var root = PrefabUtility.LoadPrefabContents(pp);
                foreach (var an in root.GetComponentsInChildren<Animator>()) an.runtimeAnimatorController = ac;
                PrefabUtility.SaveAsPrefabAsset(root, pp); PrefabUtility.UnloadPrefabContents(root);
            }

            // ---- investigator: strikes (three sword-combo swings, used as unarmed strikes), heavy, block, hit
            var hero = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Project/Animations/CharacterAnimator.controller");
            if (hero != null)
            {
                foreach (var (p, t) in new[] { ("Attack1", AnimatorControllerParameterType.Trigger), ("Attack2", AnimatorControllerParameterType.Trigger), ("Attack3", AnimatorControllerParameterType.Trigger),
                    ("Heavy", AnimatorControllerParameterType.Trigger), ("Hit", AnimatorControllerParameterType.Trigger), ("Block", AnimatorControllerParameterType.Bool) })
                { bool has = false; foreach (var q in hero.parameters) if (q.name == p) has = true; if (!has) hero.AddParameter(p, t); }
                var hsm = hero.layers[0].stateMachine; var def = hsm.defaultState;
                foreach (var cs in hsm.states) if (cs.state.name.StartsWith("Fight_")) hsm.RemoveState(cs.state);
                foreach (var (name, clip, trig) in new[] { ("Fight_Attack1", "Sword_Regular_A", "Attack1"), ("Fight_Attack2", "Sword_Regular_B", "Attack2"), ("Fight_Attack3", "Sword_Regular_C", "Attack3"), ("Fight_Heavy", "OHS_Thrust", "Heavy"), ("Fight_Hit", "OHS_Hit", "Hit") })
                {
                    var s = hsm.AddState(name); s.motion = C(clip); s.speed = name == "Fight_Heavy" ? 1.1f : 1.35f;
                    var a = hsm.AddAnyStateTransition(s); a.hasExitTime = false; a.duration = 0.06f; a.canTransitionToSelf = true; a.AddCondition(AnimatorConditionMode.If, 0, trig);
                    var back = s.AddTransition(def); back.hasExitTime = true; back.exitTime = 0.85f; back.duration = 0.15f;
                }
                var hb = hsm.AddState("Fight_Block"); hb.motion = C("OHS_BlockHold");
                var hbi = hsm.AddAnyStateTransition(hb); hbi.hasExitTime = false; hbi.duration = 0.1f; hbi.canTransitionToSelf = false; hbi.AddCondition(AnimatorConditionMode.If, 0, "Block");
                var hbo = hb.AddTransition(def); hbo.hasExitTime = false; hbo.duration = 0.15f; hbo.AddCondition(AnimatorConditionMode.IfNot, 0, "Block");
                EditorUtility.SetDirty(hero);
            }
            AssetDatabase.SaveAssets();
            Part2Tools.BuildNpc();
            Debug.Log("[Batch] fight animators built (robbers: RobberCombat, investigator: 5 strike/hit states + block)");
        }
    }
}
