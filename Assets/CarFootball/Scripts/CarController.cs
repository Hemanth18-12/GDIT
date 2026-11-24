using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// W/S = forward/back, A/D = steer, Space = jump (only when grounded).
/// Input is read in Update(), physics is applied in FixedUpdate().
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Driving")]
    [Tooltip("Top forward speed in m/s")] public float maxSpeed = 18f;
    [Tooltip("Top reverse speed in m/s")] public float maxReverseSpeed = 8f;
    [Tooltip("Acceleration in m/s^2 (mass independent)")] public float acceleration = 22f;
    [Tooltip("How strongly sideways sliding is cancelled")] public float lateralGrip = 6f;

    [Header("Boost (hold Left Shift)")]
    public float boostSpeedMultiplier = 1.7f;
    public float boostAccelerationMultiplier = 1.8f;

    [Header("Steering")]
    [Tooltip("Turn rate in degrees per second at speed")] public float steeringStrength = 120f;
    [Range(0f, 1f)] public float steeringAtStandstill = 0.5f;
    [Range(0f, 1f)] public float airSteering = 0.3f;

    [Header("Jump")]
    [Tooltip("Impulse applied upward (N*s)")] public float jumpForce = 1000f;
    public float groundCheckDistance = 0.25f;
    public LayerMask groundLayers = ~0;

    [Header("Stability")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.4f, 0f);

    Rigidbody rb;
    BoxCollider box;
    float throttleInput, steerInput;
    bool jumpRequested;
    bool boosting;
    bool isGrounded;
    float jumpCooldown;

    public bool IsGrounded => isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // Stop the car flipping: X and Z rotation frozen, Y (steering) stays free.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.centerOfMass += centerOfMassOffset;
    }

    void Update()
    {
        float t = 0f, s = 0f;
        bool jump = false;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) t += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) t -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) s += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) s -= 1f;
            jump = kb.spaceKey.wasPressedThisFrame;
            boosting = kb.leftShiftKey.isPressed;
        }
#else
        if (Input.GetKey(KeyCode.W)) t += 1f;
        if (Input.GetKey(KeyCode.S)) t -= 1f;
        if (Input.GetKey(KeyCode.D)) s += 1f;
        if (Input.GetKey(KeyCode.A)) s -= 1f;
        jump = Input.GetKeyDown(KeyCode.Space);
        boosting = Input.GetKey(KeyCode.LeftShift);
#endif
        throttleInput = t;
        steerInput = s;
        if (jump) jumpRequested = true;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (jumpCooldown > 0f) jumpCooldown -= dt;
        isGrounded = jumpCooldown <= 0f && CheckGround();

        Vector3 fwd = transform.forward;
        Vector3 right = transform.right;
        Vector3 vel = rb.linearVelocity;
        float forwardSpeed = Vector3.Dot(vel, fwd);

        if (isGrounded)
        {
            // Drive with AddForce (Acceleration mode = frame-rate and mass independent)
            bool boost = boosting && throttleInput > 0f;
            float topSpeed = boost ? maxSpeed * boostSpeedMultiplier : maxSpeed;
            float accel = boost ? acceleration * boostAccelerationMultiplier : acceleration;
            bool canAccel = (throttleInput > 0f && forwardSpeed < topSpeed) ||
                            (throttleInput < 0f && forwardSpeed > -maxReverseSpeed);
            if (canAccel)
                rb.AddForce(fwd * throttleInput * accel, ForceMode.Acceleration);

            // Tyre grip: cancel sideways sliding
            float sideways = Vector3.Dot(vel, right);
            rb.AddForce(-right * sideways * lateralGrip, ForceMode.Acceleration);
        }

        // Physics-aware steering with MoveRotation (reversed when driving backwards)
        if (Mathf.Abs(steerInput) > 0.01f)
        {
            float speedFactor = Mathf.Lerp(steeringAtStandstill, 1f, Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 6f));
            float dir = (forwardSpeed < -0.5f || (throttleInput < 0f && forwardSpeed < 0.5f)) ? -1f : 1f;
            float grip = isGrounded ? 1f : airSteering;
            float yaw = steerInput * steeringStrength * speedFactor * dir * grip * dt;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));
        }
        rb.angularVelocity = new Vector3(0f, rb.angularVelocity.y * 0.9f, 0f);

        // Jump: only when grounded, single impulse, no double jump
        if (jumpRequested && isGrounded)
        {
            rb.linearVelocity = new Vector3(vel.x, 0f, vel.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            jumpCooldown = 0.2f;
        }
        jumpRequested = false; // presses made in the air are discarded
    }

    bool CheckGround()
    {
        float halfHeight = box != null ? box.size.y * 0.5f * transform.lossyScale.y : 0.5f;
        Vector3 center = box != null ? transform.TransformPoint(box.center) : transform.position;
        float radius = 0.4f;
        float dist = halfHeight - radius + groundCheckDistance;
        var hits = Physics.SphereCastAll(center, radius, Vector3.down, dist, groundLayers, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.collider.attachedRigidbody == rb) continue;
            if (h.normal.y < 0.5f) continue; // ignore walls
            return true;
        }
        return false;
    }
}
