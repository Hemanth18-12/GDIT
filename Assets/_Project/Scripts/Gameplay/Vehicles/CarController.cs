using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Vehicles
{
    /// <summary>
    /// WheelCollider driving: steering with speed falloff, throttle, handbrake, motor and coast braking, reverse,
    /// anti-roll bars. Ported from "Modular Car Controller" by Ilisescu Adrian Corneliu (MIT,
    /// github.com/ilisescu03/Unity---Car-Controller). Changes for this game: reads input only while the player
    /// is driving (or from the cinematic autopilot), four-wheel drive, signals on Z / X (E is get in / out),
    /// gamepad and on-screen controls, and the wheels are wired up at runtime by <see cref="Jeep"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        public enum GearMode { Park, Reverse, Neutral, Drive }

        [Header("Driving")]
        public float motorForce = 1150f;
        public float maxSteeringAngle = 32f;
        public float steeringFalloffSpeed = 18f;
        [Range(0f, 1f)] public float highSpeedSteeringFactor = 0.35f;
        public float steeringSpeed = 110f;
        public float brakeForce = 5200f;
        public float motorBrakeForce = 1800f;
        public float coastBrakeForce = 380f;
        public float coastBrakeStopSpeed = 1.5f;
        [Range(0f, 1f)] public float frontBrakeBias = 0.6f;
        public float motorBrakeSpeedThreshold = 0.3f;
        public float maxForwardSpeed = 16f;   // about 58 km/h: village roads
        public float maxReverseSpeed = 5f;
        public bool fourWheelDrive = true;

        [Header("Stability")]
        public Vector3 centerOfMass = new Vector3(0f, 0.42f, 0.1f);
        public float antiRollForce = 9000f;

        [Header("Wheels")]
        public WheelCollider frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel;
        public Transform frontLeftWheelTransform, frontRightWheelTransform, rearLeftWheelTransform, rearRightWheelTransform;
        public Transform steeringWheel;
        public CarLights lights;

        /// <summary>True while the player sits in the driver's seat.</summary>
        public bool Driven;
        /// <summary>Cinematic autopilot: when on, these values replace the player's input.</summary>
        public bool autopilot; public float autoThrottle, autoSteer; public bool autoBrake;

        private float horizontalInput, verticalInput, currentSteeringAngle, currentBrakeForce;
        private bool isBraking, isMotorBraking;
        private Rigidbody rb;
        private Quaternion flOff, frOff, rlOff, rrOff, steerRest;

        public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
        public float ForwardSpeed => rb != null ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f;
        public float Throttle => verticalInput;
        public bool Braking => isBraking || isMotorBraking;
        public GearMode EffectiveGear
        {
            get
            {
                if (isBraking && CurrentSpeed < 0.5f) return GearMode.Park;
                float fs = ForwardSpeed;
                if (Mathf.Approximately(verticalInput, 0f) && !isMotorBraking && Mathf.Abs(fs) < motorBrakeSpeedThreshold) return GearMode.Neutral;
                return fs < -0.1f || verticalInput < 0f ? GearMode.Reverse : GearMode.Drive;
            }
        }

        public void Init()
        {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass = centerOfMass;
            if (lights == null) lights = GetComponent<CarLights>();
            flOff = Quaternion.Inverse(frontLeftWheel.transform.rotation) * frontLeftWheelTransform.rotation;
            frOff = Quaternion.Inverse(frontRightWheel.transform.rotation) * frontRightWheelTransform.rotation;
            rlOff = Quaternion.Inverse(rearLeftWheel.transform.rotation) * rearLeftWheelTransform.rotation;
            rrOff = Quaternion.Inverse(rearRightWheel.transform.rotation) * rearRightWheelTransform.rotation;
            if (steeringWheel != null) steerRest = steeringWheel.localRotation;
        }

        private void Update()
        {
            if (rb == null || frontLeftWheel == null) return;
            GetInput();
            UpdateWheelPoses();
            if (steeringWheel != null) steeringWheel.localRotation = steerRest * Quaternion.Euler(0f, 0f, -currentSteeringAngle * 12f);
            if (lights != null) lights.SetDrivingState(isBraking, isMotorBraking, verticalInput < 0f);
        }

        private void FixedUpdate()
        {
            if (rb == null || frontLeftWheel == null) return;
            HandleMotor(); HandleSteering(); ApplyAntiRoll();
        }

        private void GetInput()
        {
            horizontalInput = 0f; verticalInput = 0f; isMotorBraking = false; isBraking = false;
            float fwdSpeed = ForwardSpeed;
            if (autopilot)
            {
                horizontalInput = Mathf.Clamp(autoSteer, -1f, 1f);
                if (autoThrottle > 0.01f) { if (fwdSpeed < -motorBrakeSpeedThreshold) isMotorBraking = true; else verticalInput = autoThrottle; }
                else if (autoThrottle < -0.01f) { if (fwdSpeed > motorBrakeSpeedThreshold) isMotorBraking = true; else verticalInput = autoThrottle; }
                isBraking = autoBrake;
                return;
            }
            if (!Driven || InputGate.GameplayBlocked || Time.timeScale == 0f) { isBraking = !Driven; return; }

            var kb = Keyboard.current; var gp = Gamepad.current;
            float steer = 0f, gas = 0f;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steer -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) gas += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) gas -= 1f;
                isBraking = kb.spaceKey.isPressed;
                if (kb.lKey.wasPressedThisFrame && lights != null) lights.ToggleHeadLights();
                if (kb.zKey.wasPressedThisFrame && lights != null) lights.ToggleSignalLeft();
                if (kb.xKey.wasPressedThisFrame && lights != null) lights.ToggleSignalRight();
            }
            if (gp != null)
            {
                steer += gp.leftStick.ReadValue().x;
                gas += gp.rightTrigger.ReadValue() - gp.leftTrigger.ReadValue();
                isBraking |= gp.buttonSouth.isPressed;
            }
            steer += VirtualInput.Move.x; gas += VirtualInput.Move.y;
            horizontalInput = Mathf.Clamp(steer, -1f, 1f);
            gas = Mathf.Clamp(gas, -1f, 1f);
            if (gas > 0.05f) { if (fwdSpeed < -motorBrakeSpeedThreshold) isMotorBraking = true; else verticalInput = gas; }
            else if (gas < -0.05f) { if (fwdSpeed > motorBrakeSpeedThreshold) isMotorBraking = true; else verticalInput = gas; }
        }

        private void HandleMotor()
        {
            float fwd = ForwardSpeed;
            float torque = verticalInput * motorForce;
            if (verticalInput > 0f && fwd >= maxForwardSpeed) torque = 0f;
            if (verticalInput < 0f && fwd <= -maxReverseSpeed) torque = 0f;
            if (isBraking || isMotorBraking) torque = 0f;
            float share = fourWheelDrive ? 0.5f : 1f;
            frontLeftWheel.motorTorque = frontRightWheel.motorTorque = torque * share;
            rearLeftWheel.motorTorque = rearRightWheel.motorTorque = fourWheelDrive ? torque * share : 0f;

            bool noThrottle = Mathf.Approximately(verticalInput, 0f);
            if (isBraking) currentBrakeForce = brakeForce;
            else if (isMotorBraking) currentBrakeForce = motorBrakeForce;
            else if (noThrottle && coastBrakeForce > 0f)
                currentBrakeForce = coastBrakeForce * (coastBrakeStopSpeed > 0f ? Mathf.Lerp(2f, 1f, Mathf.Clamp01(Mathf.Abs(fwd) / coastBrakeStopSpeed)) : 1f);
            else currentBrakeForce = 0f;
            // The front axle takes the larger share (frontBrakeBias), like a real car. It used to be the other way round
            // (rear 100 %, front 60 %), which locks the rear wheels first and can spin the car under hard braking.
            float bias = Mathf.Clamp(frontBrakeBias, 0.5f, 0.9f);
            frontLeftWheel.brakeTorque = frontRightWheel.brakeTorque = currentBrakeForce;
            rearLeftWheel.brakeTorque = rearRightWheel.brakeTorque = currentBrakeForce * (1f - bias) / bias;
        }

        private void HandleSteering()
        {
            float t = steeringFalloffSpeed > 0f ? Mathf.Clamp01(CurrentSpeed / steeringFalloffSpeed) : 0f;
            float target = maxSteeringAngle * Mathf.Lerp(1f, highSpeedSteeringFactor, t) * horizontalInput;
            currentSteeringAngle = steeringSpeed > 0f ? Mathf.MoveTowards(currentSteeringAngle, target, steeringSpeed * Time.fixedDeltaTime) : target;
            frontLeftWheel.steerAngle = frontRightWheel.steerAngle = currentSteeringAngle;
        }

        private static void Pose(WheelCollider c, Transform mesh, Quaternion off)
        {
            c.GetWorldPose(out Vector3 p, out Quaternion r); mesh.position = p; mesh.rotation = r * off;
        }
        private void UpdateWheelPoses()
        {
            Pose(frontLeftWheel, frontLeftWheelTransform, flOff); Pose(frontRightWheel, frontRightWheelTransform, frOff);
            Pose(rearLeftWheel, rearLeftWheelTransform, rlOff); Pose(rearRightWheel, rearRightWheelTransform, rrOff);
        }

        private void ApplyAntiRoll()
        {
            if (antiRollForce <= 0f) return;
            Stabilize(frontLeftWheel, frontRightWheel); Stabilize(rearLeftWheel, rearRightWheel);
        }
        private void Stabilize(WheelCollider l, WheelCollider r)
        {
            float tl = 1f, tr = 1f;
            bool gl = l.GetGroundHit(out WheelHit hl), gr = r.GetGroundHit(out WheelHit hr);
            if (gl) tl = (-l.transform.InverseTransformPoint(hl.point).y - l.radius) / l.suspensionDistance;
            if (gr) tr = (-r.transform.InverseTransformPoint(hr.point).y - r.radius) / r.suspensionDistance;
            float f = (tl - tr) * antiRollForce;
            if (gl) rb.AddForceAtPosition(l.transform.up * -f, l.transform.position);
            if (gr) rb.AddForceAtPosition(r.transform.up * f, r.transform.position);
        }
    }
}
