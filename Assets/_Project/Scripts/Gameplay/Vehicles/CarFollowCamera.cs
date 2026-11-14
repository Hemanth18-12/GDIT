using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Vehicles
{
    /// <summary>
    /// Orbit-and-follow camera for driving: mouse / right stick orbit, scroll zoom, auto-recentre behind the car
    /// when moving, sphere-cast collision so it never ends up inside a wall. Ported from "Modular Car Controller"
    /// CameraFollow (MIT, Ilisescu Adrian Corneliu). It is added to the main camera and switched on only while driving.
    /// </summary>
    public class CarFollowCamera : MonoBehaviour
    {
        public Transform target; public CarController car;
        public float distance = 7.5f, minDistance = 4f, maxDistance = 13f, lookAtHeight = 1.4f;
        public float defaultPitch = 14f, minPitch = -5f, maxPitch = 60f, sensitivity = 0.15f;
        public float recenterDelay = 1.2f, recenterYawSpeed = 110f, recenterPitchSpeed = 35f, recenterFullSpeed = 10f;
        public float positionLerp = 9f, rotationLerp = 12f, collisionRadius = 0.3f, collisionPadding = 0.2f;
        private float yaw, pitch, idle; private bool init;

        public void Snap()
        {
            if (target == null) return;
            yaw = target.eulerAngles.y; pitch = defaultPitch; init = true;
            Vector3 anchor = target.position + Vector3.up * lookAtHeight;
            transform.position = anchor + Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distance);
            transform.rotation = Quaternion.LookRotation(anchor - transform.position);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (!init) Snap();
            Vector2 d = Vector2.zero; float scroll = 0f;
            if (Mouse.current != null && !InputGate.GameplayBlocked) { d += Mouse.current.delta.ReadValue(); scroll = Mouse.current.scroll.ReadValue().y; }
            if (Gamepad.current != null) d += Gamepad.current.rightStick.ReadValue() * 8f;
            d += VirtualInput.Look * 4f;
            if (d.sqrMagnitude > 0.01f) { yaw += d.x * sensitivity; pitch = Mathf.Clamp(pitch - d.y * sensitivity, minPitch, maxPitch); idle = 0f; }
            else idle += Time.deltaTime;
            if (Mathf.Abs(scroll) > 0.01f) distance = Mathf.Clamp(distance - scroll * 0.01f, minDistance, maxDistance);

            float speed = car != null ? Mathf.Abs(car.ForwardSpeed) : 0f;
            if (idle > recenterDelay && speed > 0.5f)
            {
                float k = Mathf.Clamp01(speed / recenterFullSpeed);
                float goalYaw = target.eulerAngles.y + (car != null && car.ForwardSpeed < -0.5f ? 180f : 0f);
                yaw = Mathf.MoveTowardsAngle(yaw, goalYaw, recenterYawSpeed * k * Time.deltaTime);
                pitch = Mathf.MoveTowards(pitch, defaultPitch, recenterPitchSpeed * k * Time.deltaTime);
            }

            Vector3 anchor = target.position + Vector3.up * lookAtHeight;
            Vector3 want = anchor + Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distance);
            transform.position = Vector3.Lerp(transform.position, want, positionLerp * Time.deltaTime);
            // collision: pull in before anything that is not the car
            Vector3 dir = transform.position - anchor; float len = dir.magnitude;
            if (len > 0.01f)
            {
                foreach (var h in Physics.SphereCastAll(anchor, collisionRadius, dir / len, len, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider.transform.IsChildOf(target) || h.distance <= 0f) continue;
                    float dd = Mathf.Max(0.8f, h.distance - collisionPadding);
                    if (dd < len) { len = dd; transform.position = anchor + dir.normalized * len; }
                }
            }
            Vector3 look = anchor - transform.position;
            if (look.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look, Vector3.up), rotationLerp * Time.deltaTime);
            var e = transform.eulerAngles; transform.eulerAngles = new Vector3(e.x, e.y, 0f);
        }
    }
}
