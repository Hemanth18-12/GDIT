using UnityEngine;

namespace LegacyOfTemples.World
{
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [Header("Flicker Settings")]
        [SerializeField] private float minIntensity = 1.2f;
        [SerializeField] private float maxIntensity = 2.4f;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float subtlePositionShift = 0.02f;

        private Light targetLight;
        private Vector3 initialPosition;
        private float noiseOffset;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            initialPosition = transform.localPosition;
            noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (targetLight == null) return;

            float time = Time.time * speed + noiseOffset;
            float noise = Mathf.PerlinNoise(time, time * 0.35f);
            targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

            if (subtlePositionShift > 0f)
            {
                float offsetX = (Mathf.PerlinNoise(time * 0.5f, 0f) - 0.5f) * subtlePositionShift;
                float offsetZ = (Mathf.PerlinNoise(0f, time * 0.5f) - 0.5f) * subtlePositionShift;
                transform.localPosition = initialPosition + new Vector3(offsetX, 0f, offsetZ);
            }
        }
    }
}
