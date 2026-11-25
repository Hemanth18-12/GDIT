using UnityEngine;
using System.Collections.Generic;

namespace LegacyOfTemples.Characters
{
    /// <summary>Drives a humanoid NPC's locomotion parameters from its real movement (real mocap clips, no faked motion).</summary>
    public sealed class NpcAnimDriver : MonoBehaviour
    {
        public bool crouch;
        public bool standIn; // true = temporary stand-in model (see Docs/BLOCKED.md)
        private Animator anim; private Vector3 last; private float speed;

        readonly HashSet<string> parameters = new HashSet<string>();
        private void Start()
        {
            anim = GetComponentInChildren<Animator>(); last = transform.position;
            if (anim != null) foreach (var parameter in anim.parameters) parameters.Add(parameter.name);
        }
        void Float(string key, float value) { if (parameters.Contains(key)) anim.SetFloat(key, value); }
        void Flag(string key, bool value) { if (parameters.Contains(key)) anim.SetBool(key, value); }

        private void LateUpdate()
        {
            if (anim == null || Time.deltaTime <= 0f) return;
            Vector3 d = transform.position - last; d.y = 0f; last = transform.position;
            speed = Mathf.Lerp(speed, d.magnitude / Time.deltaTime, 10f * Time.deltaTime);
            bool moving = speed > 0.25f;
            Float("Speed", moving ? speed : 0f);
            Float("MoveX", 0f); Float("MoveZ", moving ? 1f : 0f);
            Flag("IsMoving", moving); Flag("IsRunning", speed > 3f);
            Flag("IsCrouching", crouch); Flag("Crouch", crouch); Flag("Grounded", true);
        }
    }
}
