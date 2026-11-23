using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Helper for level design: call Fire() from any UnityEvent (a button, an animation event, a Timeline signal) to
    /// ask the voice manager for a line of the chosen trigger (any trigger in Docs/VOICE_LINES.csv). The manager still
    /// decides (cooldown, chance, priority, conditions), so it never forces a line or makes two voices overlap.
    /// </summary>
    [AddComponentMenu("Legacy of the Temple/Voice On Event")]
    public sealed class VoiceOnEvent : MonoBehaviour
    {
        [Tooltip("Trigger name from Docs/VOICE_LINES.csv")]
        public string trigger = "notice";
        [Tooltip("Optional details for the line's conditions, for example enemy=kishore")]
        public string context;
        [Tooltip("Optional: who speaks (their head is used for 3D sound)")]
        public Transform speaker;
        [Tooltip("Also ask when this object is switched on")]
        public bool onEnable;

        private void OnEnable() { if (onEnable) Fire(); }

        /// <summary>Asks for a line of this component's trigger.</summary>
        public void Fire() => VoiceEvents.Fire(trigger, speaker, context);
        /// <summary>Asks for a line of another trigger (handy from a UnityEvent with a text argument).</summary>
        public void FireTrigger(string triggerName) => VoiceEvents.Fire(triggerName, speaker, context);
    }
}
