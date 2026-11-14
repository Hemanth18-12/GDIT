using UnityEngine;

namespace LegacyOfTemples.Gameplay.Vehicles
{
    /// <summary>
    /// Engine loop whose pitch follows a simulated rpm with gear shifts, an indicator tick, and impact sounds by
    /// strength. Ported from "Modular Car Controller" (MIT, Ilisescu Adrian Corneliu); clips Engine/Signal/Hit
    /// from the same project (Resources/Audio/Car). Volumes are kept low so the jeep never drowns the village.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class CarAudio : MonoBehaviour
    {
        public AudioSource engine, signal, hit;
        public float minPitch = 0.75f, maxPitch = 1.45f, rpmRise = 0.55f, rpmDecay = 0.9f, postShift = 0.5f;
        public int gears = 5;
        public float hitThreshold = 2f, hitReference = 16f;
        /// <summary>0..1: the engine fades out when the jeep parks (cinematic) and back in when started.</summary>
        public float engineLevel = 1f;
        private CarController car; private CarLights lights; private float rpm; private int gear = 1;

        public void Init()
        {
            car = GetComponent<CarController>(); lights = GetComponent<CarLights>();
            engine = Source("Engine", true, 0.32f); signal = Source("Signal", true, 0.18f); hit = Source("Hit", false, 0.6f);
            if (engine.clip != null) engine.Play();
        }

        private AudioSource Source(string clip, bool loop, float vol)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = Resources.Load<AudioClip>("Audio/Car/" + clip); s.loop = loop; s.volume = vol; s.playOnAwake = false;
            s.spatialBlend = 1f; s.minDistance = 4f; s.maxDistance = 45f; s.rolloffMode = AudioRolloffMode.Linear; s.dopplerLevel = 0f;
            LegacyOfTemples.Core.AudioManager.Route(s, "SFX"); // through the game mixer's SFX group
            return s;
        }

        private void Update()
        {
            if (car == null || engine == null) return;
            float throttle = Mathf.Abs(car.Throttle);
            if (throttle > 0.01f)
            {
                rpm += rpmRise * throttle * Time.deltaTime;
                if (rpm >= 1f) { if (gear < gears) { rpm = postShift; gear++; } else rpm = 1f; }
            }
            else
            {
                rpm = Mathf.Max(0f, rpm - rpmDecay * Time.deltaTime);
                if (rpm < 0.3f && gear > 1) { gear--; rpm = 0.7f; }
            }
            float speedPart = Mathf.Clamp01(car.CurrentSpeed / 16f) * 0.25f;
            engine.pitch = Mathf.Lerp(minPitch, maxPitch, Mathf.Clamp01(rpm * 0.8f + speedPart));
            engine.volume = 0.32f * engineLevel * Mathf.Lerp(0.75f, 1f, rpm);
            bool tick = lights != null && (lights.SignalLeftOn || lights.SignalRightOn);
            if (tick && !signal.isPlaying && signal.clip != null) signal.Play(); else if (!tick && signal.isPlaying) signal.Stop();
        }

        /// <summary>Called by the damage system with the impact speed change (m/s).</summary>
        public void Impact(float deltaV)
        {
            if (hit == null || hit.clip == null || deltaV < hitThreshold) return;
            float k = Mathf.Clamp01((deltaV - hitThreshold) / Mathf.Max(0.01f, hitReference - hitThreshold));
            hit.pitch = Mathf.Lerp(0.85f, 1.2f, k) + Random.Range(-0.08f, 0.08f);
            hit.PlayOneShot(hit.clip, Mathf.Clamp01(Mathf.Lerp(0.35f, 1f, k) + Random.Range(-0.1f, 0.1f)));
        }
    }
}
