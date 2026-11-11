using UnityEngine;

namespace LegacyOfTemples.Characters
{
    /// <summary>Drives a humanoid NPC's locomotion parameters from its real movement (real mocap clips, no faked motion).</summary>
    public sealed class NpcAnimDriver : MonoBehaviour
    {
        public bool crouch;
        public bool standIn; // true = temporary stand-in model (see Docs/BLOCKED.md)
        private Animator anim; private Vector3 last; private float speed;

        private void Start() { anim = GetComponentInChildren<Animator>(); last = transform.position; }

        private void LateUpdate()
        {
            if (anim == null || Time.deltaTime <= 0f) return;
            Vector3 d = transform.position - last; d.y = 0f; last = transform.position;
            speed = Mathf.Lerp(speed, d.magnitude / Time.deltaTime, 10f * Time.deltaTime);
            bool moving = speed > 0.25f;
            anim.SetFloat("Speed", moving ? speed : 0f);
            anim.SetFloat("MoveX", 0f); anim.SetFloat("MoveZ", moving ? 1f : 0f);
            anim.SetBool("IsMoving", moving); anim.SetBool("IsRunning", speed > 3f);
            anim.SetBool("IsCrouching", crouch); anim.SetBool("Crouch", crouch); anim.SetBool("Grounded", true);
        }
    }
}
