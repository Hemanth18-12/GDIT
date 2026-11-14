using UnityEngine;

namespace LegacyOfTemples.Gameplay.Vehicles
{
    /// <summary>
    /// Brake, head, reverse and indicator lights: real Light components plus glowing lamp meshes.
    /// Ported from "Modular Car Controller" (MIT, Ilisescu Adrian Corneliu). Lamp glow uses a per-renderer
    /// MaterialPropertyBlock instead of material instances, so the shared materials stay untouched.
    /// </summary>
    public class CarLights : MonoBehaviour
    {
        public Light[] head = new Light[0], stop = new Light[0], reverse = new Light[0], turnLeft = new Light[0], turnRight = new Light[0];
        public Renderer[] headMesh = new Renderer[0], stopMesh = new Renderer[0], reverseMesh = new Renderer[0], turnLeftMesh = new Renderer[0], turnRightMesh = new Renderer[0];
        public Color stopColor = new Color(1f, 0.05f, 0.03f), headColor = new Color(1f, 0.93f, 0.78f), reverseColor = Color.white, turnColor = new Color(1f, 0.55f, 0f);
        public float stopBrake = 6f, stopRunning = 1.6f, headIntensity = 6f, reverseIntensity = 3f, turnIntensity = 3f, blink = 0.4f;
        /// <summary>Set false when a lamp is broken: it stays dark.</summary>
        public bool[] broken = new bool[5];

        private bool braking, reversing, headOn, left, right, blinkOn; private float blinkT;
        private MaterialPropertyBlock mpb;
        public bool HeadLightsOn => headOn; public bool SignalLeftOn => left; public bool SignalRightOn => right;

        public void SetDrivingState(bool brake, bool motorBrake, bool reverse) { braking = brake || motorBrake; reversing = reverse; }
        public void SetHeadLights(bool on) { headOn = on; }
        public void ToggleHeadLights() { headOn = !headOn; }
        public void ToggleSignalLeft() { left = !left; if (left) right = false; blinkT = 0f; blinkOn = true; }
        public void ToggleSignalRight() { right = !right; if (right) left = false; blinkT = 0f; blinkOn = true; }
        public void SignalsOff() { left = right = false; }

        private void Update()
        {
            if (left || right) { blinkT += Time.deltaTime; if (blinkT >= blink) { blinkT = 0f; blinkOn = !blinkOn; } } else blinkOn = false;
            float s = braking ? stopBrake : headOn ? stopRunning : 0f;
            Set(stop, stopMesh, s > 0f && !broken[1], s, stopColor);
            Set(head, headMesh, headOn && !broken[0], headIntensity, headColor);
            Set(reverse, reverseMesh, reversing && !broken[2], reverseIntensity, reverseColor);
            Set(turnLeft, turnLeftMesh, left && blinkOn && !broken[3], turnIntensity, turnColor);
            Set(turnRight, turnRightMesh, right && blinkOn && !broken[4], turnIntensity, turnColor);
        }

        private void Set(Light[] ls, Renderer[] rs, bool on, float intensity, Color c)
        {
            foreach (var l in ls) if (l != null) { l.enabled = on; if (on) l.intensity = intensity; }
            if (mpb == null) mpb = new MaterialPropertyBlock();
            foreach (var r in rs)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", on ? c * Mathf.Max(1f, intensity * 0.6f) : c * 0.08f); r.SetPropertyBlock(mpb);
            }
        }
    }
}
