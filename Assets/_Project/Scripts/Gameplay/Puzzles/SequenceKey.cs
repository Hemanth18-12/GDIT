using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>One thing to strike in a sound lock (a bell, a chime, a stone). It only makes its sound and tells the lock.</summary>
    public class SequenceKey : MonoBehaviour, IInteractable
    {
        public SequenceLock owner;
        public int index;
        public float pitch = 1f;
        public string verb = "Strike";
        public float radius = 2f;
        public AudioSource sound;

        public string Id => "key." + name;
        public string Prompt => owner != null && owner.locked ? "" : verb;
        public Transform Anchor => transform;
        public float Radius => owner != null && owner.locked ? 0f : radius;
        public void Interact()
        {
            if (sound != null) { sound.pitch = pitch; sound.Play(); }
            if (owner != null) owner.Press(index);
        }
    }
}
