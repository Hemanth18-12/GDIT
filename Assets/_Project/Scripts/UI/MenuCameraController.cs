using UnityEngine;

namespace LegacyOfTemples.UI
{
    public class MenuCameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 orbitCenter = new Vector3(0f, 3.5f, 0f);
        [SerializeField] private float orbitDistance = 22.0f;
        [SerializeField] private float orbitSpeed = 1.8f;
        [SerializeField] private float cameraHeight = 5.0f;
        [SerializeField] private float verticalBobSpeed = 0.5f;
        [SerializeField] private float verticalBobAmount = 0.8f;

        private float currentAngle = 180.0f;

        private void Update()
        {
            currentAngle += orbitSpeed * Time.deltaTime;
            if (currentAngle > 360f) currentAngle -= 360f;

            float rad = currentAngle * Mathf.Deg2Rad;
            float x = orbitCenter.x + Mathf.Sin(rad) * orbitDistance;
            float z = orbitCenter.z + Mathf.Cos(rad) * orbitDistance;
            float y = cameraHeight + Mathf.Sin(Time.time * verticalBobSpeed) * verticalBobAmount;

            transform.position = new Vector3(x, y, z);
            transform.LookAt(orbitCenter + Vector3.up * 1.5f);
        }
    }
}

