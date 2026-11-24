using UnityEngine;

/// <summary>Smooth third-person camera that follows the car from behind.</summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    [Tooltip("Distance behind the car")] public float distance = 10f;
    [Tooltip("Height above the car")] public float height = 4.5f;
    [Tooltip("Point above the car to look at")] public float lookHeight = 1f;
    [Tooltip("Position smoothness (higher = snappier)")] public float smoothness = 6f;
    [Tooltip("Rotation smoothness (higher = snappier)")] public float rotationSmoothness = 4f;

    Quaternion currentYaw;

    void Start()
    {
        if (target == null) return;
        currentYaw = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        transform.position = DesiredPosition();
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }

    void LateUpdate()
    {
        if (target == null) return;
        float dt = Time.deltaTime;
        // Only follow the car's yaw -> no shaking from bumps or jumps
        Quaternion targetYaw = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        currentYaw = Quaternion.Slerp(currentYaw, targetYaw, 1f - Mathf.Exp(-rotationSmoothness * dt));
        transform.position = Vector3.Lerp(transform.position, DesiredPosition(), 1f - Mathf.Exp(-smoothness * dt));
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }

    Vector3 DesiredPosition()
    {
        return target.position - currentYaw * Vector3.forward * distance + Vector3.up * height;
    }
}
