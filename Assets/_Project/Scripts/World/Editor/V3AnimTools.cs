using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>Adds the dodge roll (the project's own Roll clip) to the investigator's animator, played by the trigger "Roll".</summary>
    public static class V3AnimTools
    {
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
