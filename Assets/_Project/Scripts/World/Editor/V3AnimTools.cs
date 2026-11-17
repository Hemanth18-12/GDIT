using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>Adds the dodge roll (the project's own Roll clip) to the investigator's animator, played by the trigger "Roll".</summary>
    public static class V3AnimTools
    {
        /// <summary>Sit and Talk loops (the project's Sitting_Idle_Loop and Idle_Talking_Loop) on the shared character animator, driven by bools.</summary>
        [MenuItem("Legacy of the Temple/V3/Add villager routine states")]
        public static void AddRoutineStates()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Project/Animations/CharacterAnimator.controller");
            if (ctrl == null) { Debug.LogError("[Batch] V3 anim: controller missing"); return; }
            var sm = ctrl.layers[0].stateMachine; int added = 0;
            foreach (var (state, param, clipName) in new[] { ("Routine_Sit", "Sitting", "Sitting_Idle_Loop"), ("Routine_Talk", "Talking", "Idle_Talking_Loop") })
            {
                bool exists = false; foreach (var st in sm.states) if (st.state.name == state) exists = true;
                if (exists) continue;
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Clips/Hero/" + clipName + ".anim"); if (clip == null) { Debug.LogWarning("[Batch] missing clip " + clipName); continue; }
                bool hasParam = false; foreach (var p in ctrl.parameters) if (p.name == param) hasParam = true;
                if (!hasParam) ctrl.AddParameter(param, AnimatorControllerParameterType.Bool);
                var s2 = sm.AddState(state); s2.motion = clip;
                var tin = sm.AddAnyStateTransition(s2); tin.AddCondition(AnimatorConditionMode.If, 0f, param); tin.duration = 0.35f; tin.hasExitTime = false; tin.canTransitionToSelf = false;
                var tout = s2.AddTransition(sm.defaultState); tout.AddCondition(AnimatorConditionMode.IfNot, 0f, param); tout.duration = 0.35f; tout.hasExitTime = false;
                added++;
            }
            EditorUtility.SetDirty(ctrl); AssetDatabase.SaveAssets();
            Debug.Log($"[Batch] V3 anim: {added} villager routine state(s) added (bools Sitting, Talking)");
        }

        [MenuItem("Legacy of the Temple/V3/Add fight states")]
        public static void AddFightStates()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Project/Animations/CharacterAnimator.controller");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Clips/Hero/Roll.anim");
            if (ctrl == null || clip == null) { Debug.LogError("[Batch] V3 anim: controller or Roll clip missing"); return; }
            var sm = ctrl.layers[0].stateMachine;
            foreach (var s in sm.states) if (s.state.name == "Fight_Roll") { Debug.Log("[Batch] V3 anim: Fight_Roll already present"); return; }
            bool has = false; foreach (var p in ctrl.parameters) if (p.name == "Roll") has = true;
            if (!has) ctrl.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
            var st = sm.AddState("Fight_Roll"); st.motion = clip; st.speed = 1.6f;
            var t = sm.AddAnyStateTransition(st); t.AddCondition(AnimatorConditionMode.If, 0f, "Roll"); t.duration = 0.06f; t.hasExitTime = false; t.canTransitionToSelf = false;
            var back = st.AddTransition(sm.defaultState); back.hasExitTime = true; back.exitTime = 0.85f; back.duration = 0.12f;
            EditorUtility.SetDirty(ctrl); AssetDatabase.SaveAssets();
            Debug.Log("[Batch] V3 anim: Fight_Roll added to CharacterAnimator (trigger Roll)");
        }
    }
}
