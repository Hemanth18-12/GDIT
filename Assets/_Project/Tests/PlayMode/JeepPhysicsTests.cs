using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.Tests
{
    /// <summary>The jeep must rest on flat ground without hopping or tipping when its physics switches on.</summary>
    public class JeepPhysicsTests
    {
        [UnityTest]
        public IEnumerator Jeep_RestsOnFlatGround_NoHop()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.position = new Vector3(500f, -0.5f, 500f); ground.transform.localScale = new Vector3(60f, 1f, 60f);
            Physics.SyncTransforms();
            var jeep = Jeep.Spawn(new Vector3(500f, 0.6f, 500f), Quaternion.identity);
            Assert.IsNotNull(jeep, "Resources/Vehicles/Jeep prefab missing");
            yield return null;
            var sb = new StringBuilder("[JeepTest] colliders: ");
            foreach (var c in jeep.GetComponentsInChildren<Collider>())
            {
                if (c is WheelCollider w) sb.Append($"{c.name} wheel r={w.radius:0.00} at {jeep.transform.InverseTransformPoint(c.transform.position):0.00}; ");
                else if (c is BoxCollider b) sb.Append($"{c.name} box centre={b.center:0.00} size={b.size:0.00}; ");
                else sb.Append($"{c.name} {c.GetType().Name}; ");
            }
            Debug.Log(sb.ToString());
            var rb = jeep.GetComponent<Rigidbody>();
            Debug.Log($"[JeepTest] mass={rb.mass} com={rb.centerOfMass:0.00} inertia={rb.inertiaTensor:0} model local pos={jeep.Model.localPosition:0.000} rot={jeep.Model.localEulerAngles:0}");
            float min = float.MaxValue, max = float.MinValue, tilt = 0f; var hs = new StringBuilder();
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForSeconds(0.2f);
                float y = jeep.transform.position.y; min = Mathf.Min(min, y); max = Mathf.Max(max, y); tilt = Mathf.Max(tilt, Vector3.Angle(jeep.transform.up, Vector3.up)); hs.Append(y.ToString("0.000")).Append(' ');
            }
            Debug.Log($"[JeepTest] heights: {hs} max tilt={tilt:0.0}");
            Object.Destroy(jeep.gameObject); Object.Destroy(ground);
            Assert.Less(max - min, 0.08f, "the jeep hopped: " + hs);
            Assert.Less(tilt, 3f, "the jeep tipped");
        }
    }
}
