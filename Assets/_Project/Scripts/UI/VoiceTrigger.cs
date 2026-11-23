using UnityEngine;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Helper for level design: put it on an object with a trigger collider. When the player walks in, it asks the
    /// voice manager for a line of the chosen trigger (any trigger in Docs/VOICE_LINES.csv). The manager still decides
    /// (cooldown, chance, priority, conditions), so a VoiceTrigger never forces a line or makes two voices overlap.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Legacy of the Temple/Voice Trigger (area)")]
    public sealed class VoiceTrigger : MonoBehaviour
    {
        [Tooltip("Trigger name from Docs/VOICE_LINES.csv, for example notice, area_enter or danger_drop")]
        public string trigger = "notice";
        [Tooltip("Optional details for the line's conditions, for example zone=Vaults or speaker=pujari")]
        public string context;
        [Tooltip("Optional: who speaks (their head is used for 3D sound). Empty = found by the speaker name")]
        public Transform speaker;
        [Tooltip("Ask only the first time the player walks in")]
        public bool onlyOnce = true;

        private bool done;

        private void Reset() { var c = GetComponent<Collider>(); if (c != null) c.isTrigger = true; }

        private void OnTriggerEnter(Collider other)
        {
            if (done && onlyOnce) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            done = true;
            VoiceEvents.Fire(trigger, speaker, context);
        }
    }
}
