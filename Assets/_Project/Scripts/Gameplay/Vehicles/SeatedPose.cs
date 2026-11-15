using UnityEngine;

namespace LegacyOfTemples.Gameplay.Vehicles
{
    /// <summary>
    /// Holds a humanoid in a driving pose (hips and knees bent, hands forward on the wheel, head looking ahead) with its hips
    /// on the driver's seat. A static pose set through Unity's HumanPoseHandler (no generated animation clip); removed on exit.
    /// </summary>
    public sealed class SeatedPose : MonoBehaviour
    {
        public Transform car; public Vector3 seatLocal;   // seat point in the car's space
        private Animator anim; private HumanPoseHandler handler; private HumanPose pose; private bool ok;

        private void Start()
        {
            anim = GetComponentInChildren<Animator>();
            if (anim == null || !anim.isHuman || anim.avatar == null) { Debug.LogWarning($"[SeatedPose] {name}: no humanoid animator (anim={(anim != null)}, human={(anim != null && anim.isHuman)})"); enabled = false; return; }
            anim.enabled = false;
            handler = new HumanPoseHandler(anim.avatar, anim.transform);
            handler.GetHumanPose(ref pose);
            for (int i = 0; i < pose.muscles.Length && i < HumanTrait.MuscleCount; i++)
            {
                string m = HumanTrait.MuscleName[i]; float v = 0f;
                if (m.Contains("Upper Leg Front-Back")) v = 0.9f;          // thighs forward along the seat
                else if (m.Contains("Lower Leg Stretch")) v = -0.75f;      // knees bent
                else if (m.Contains("Foot Up-Down")) v = 0.15f;
                else if (m.Contains("Arm Front-Back")) v = 0.55f;          // arms reach forward
                else if (m.Contains("Arm Down-Up")) v = -0.45f;          // upper arms down from the shoulder, not spread
                else if (m.Contains("Forearm Stretch")) v = 0.1f;         // elbows slightly bent, hands at the wheel
                else if (m.Contains("Shoulder Front-Back")) v = 0.2f;
                else if (m.Contains("Finger") || m.Contains("Thumb")) v = pose.muscles[i];   // keep the hands as they were
                pose.muscles[i] = v;
            }
            ok = true; logT = 1f;
        }
        private float logT;

        private void LateUpdate()
        {
            if (!ok || car == null) return;
            transform.rotation = car.rotation;
            handler.SetHumanPose(ref pose);
            var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null) transform.position += car.TransformPoint(seatLocal) - hips.position;   // hips exactly on the seat
            if (logT > 0f && (logT -= Time.deltaTime) <= 0f) Debug.Log($"[SeatedPose] seat={car.TransformPoint(seatLocal)} hips={(hips != null ? hips.position : Vector3.zero)} root={transform.position} head={anim.GetBoneTransform(HumanBodyBones.Head)?.position}");
        }

        private void OnDestroy()
        {
            if (anim != null) anim.enabled = true;
            handler?.Dispose();
        }
    }
}
