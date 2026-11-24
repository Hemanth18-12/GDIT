using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Vehicles
{
    /// <summary>
    /// The investigator's jeep (own Blender model, Art_Source/Jeep). Wires the model up at runtime: wheel colliders,
    /// body colliders, rigidbody, lights, engine audio, the ported car controller, doors and hood that open, getting
    /// in and out with E, the driving camera and the speed panel, and damage by impact strength and location.
    /// </summary>
    public class Jeep : MonoBehaviour, IInteractable
    {
        public static Jeep Instance { get; private set; }

        [Header("Damage meshes (filled by the editor tool)")]
        public Mesh[] bodyStates = new Mesh[4];   // 0 = intact (taken from the model), 1 damaged, 2 wrecked, 3 destroyed
        public Mesh[] hoodStates = new Mesh[4];
        public Material scuffedPaint, burntPaint, interior;

        public CarController Car { get; private set; }
        public CarLights Lights { get; private set; }
        public CarAudio Audio { get; private set; }
        public bool Occupied { get; private set; }
        public bool Transitioning { get; private set; }
        public int DamageState { get; private set; }
        public float TotalDamage { get; private set; }
        public readonly Dictionary<string, float> ZoneDamage = new Dictionary<string, float> { { "front", 0f }, { "rear", 0f }, { "left", 0f }, { "right", 0f }, { "top", 0f } };
        public int PartsBroken { get; private set; }
        public float LastImpact { get; private set; }

        public Transform Model { get; private set; }
        private Rigidbody rb;
        private readonly Dictionary<string, Transform> named = new Dictionary<string, Transform>();
        private Transform driverDoor, hood, steeringCol; private float driverSide = 1f;
        private Quaternion driverDoorClosed, hoodClosed; private float driverDoorSign = 1f, hoodSign = 1f;
        private PlayerController player; private OrbitCamera orbit; private CarFollowCamera follow;
        private int enteredFrame = -10;
        private ParticleSystem smoke;

        // ------------------------------------------------------------------ spawning
        public static Jeep Spawn(Vector3 position, Quaternion rotation)
        {
            var pf = Resources.Load<GameObject>("Vehicles/Jeep");
            if (pf == null) { Debug.LogWarning("[Jeep] Resources/Vehicles/Jeep prefab is missing (run VehicleTools.Build)."); return null; }
            // stand it exactly on the ground (wheels touch y = 0 of the root after AlignModel), never drop it
            if (Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out RaycastHit h, 10f, ~0, QueryTriggerInteraction.Ignore)) position.y = h.point.y + 0.02f;
            var g = Instantiate(pf, position, rotation); g.name = "Jeep";
            var j = g.GetComponent<Jeep>(); j.StartCoroutine(j.Settle());
            return j;
        }

        private void Awake()
        {
            Instance = this;
            Model = transform.childCount > 0 ? transform.GetChild(0) : transform;
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (!named.ContainsKey(t.name)) named[t.name] = t;
            AlignModel();
            BuildPhysics();
            BuildLights();
            Audio = gameObject.AddComponent<CarAudio>(); Audio.Init();
            Car.Init();
            CacheMovingParts();
            if (bodyStates[0] == null && T("SM_Jeep_Body") != null) bodyStates[0] = T("SM_Jeep_Body").GetComponent<MeshFilter>().sharedMesh;
            if (hoodStates[0] == null && T("SM_Jeep_Hood") != null) hoodStates[0] = T("SM_Jeep_Hood").GetComponent<MeshFilter>().sharedMesh;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Physics stays asleep for the first moments, so the jeep never hops when a level starts.</summary>
        public IEnumerator Settle()
        {
            rb.isKinematic = true;
            yield return new WaitForSeconds(0.6f);
            if (!Scripted) { rb.isKinematic = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; StartCoroutine(LogHeights()); }
        }
        private IEnumerator LogHeights()
        {
            SpawnHeights.Clear(); float t0 = Time.time;
            while (Time.time - t0 < 10f) { SpawnHeights.Add(transform.position.y); yield return new WaitForSeconds(0.5f); }
        }
        /// <summary>Height of the jeep every half second for 10 s after spawning (QA proof that it does not hop).</summary>
        public static readonly List<float> SpawnHeights = new List<float>();

        // ------------------------------------------------------------------ paint variants
        public enum Paint { White, Olive, Red, Blue }
        public void SetPaint(Paint p)
        {
            Color c = p == Paint.Olive ? new Color(0.30f, 0.35f, 0.23f) : p == Paint.Red ? new Color(0.62f, 0.06f, 0.04f) : p == Paint.Blue ? new Color(0.12f, 0.2f, 0.42f) : new Color(0.9f, 0.9f, 0.88f);
            var mpb = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) if (mats[i] != null && mats[i].name.Contains("Paint_Body")) { r.GetPropertyBlock(mpb, i); mpb.SetColor("_BaseColor", c); r.SetPropertyBlock(mpb, i); }
            }
        }

        // ------------------------------------------------------------------ scripted (cutscene) driving: kinematic, no physics jitter
        public bool Scripted { get; private set; }
        private Quaternion[] wheelRest; private Transform[] wheelT; private float spin, bobT; private Vector3 modelRest, lastPos; private Quaternion lastRot;
        public void BeginScripted()
        {
            Scripted = true; rb.isKinematic = true; Car.enabled = false;
            wheelT = new[] { Car.frontLeftWheelTransform, Car.frontRightWheelTransform, Car.rearLeftWheelTransform, Car.rearRightWheelTransform };
            wheelRest = new Quaternion[4]; for (int i = 0; i < 4; i++) wheelRest[i] = Quaternion.Inverse(transform.rotation) * wheelT[i].rotation;
            modelRest = Model.localPosition; lastPos = transform.position; lastRot = transform.rotation;
        }
        /// <summary>Move along the cutscene path: wheels spin with distance, the front wheels steer with the turn, the body bobs a little on its springs.</summary>
        public void ScriptedMove(Vector3 pos, Quaternion rot)
        {
            if (!Scripted) return;
            float dist = Vector3.Distance(Flat(pos), Flat(lastPos)), dt = Mathf.Max(Time.deltaTime, 1e-4f), speed = dist / dt;
            float yawRate = Mathf.DeltaAngle(lastRot.eulerAngles.y, rot.eulerAngles.y) / dt;
            transform.SetPositionAndRotation(pos, rot); lastPos = pos; lastRot = rot;
            spin += dist / 0.4f * Mathf.Rad2Deg;
            float steer = Mathf.Clamp(yawRate * 0.9f, -28f, 28f);
            for (int i = 0; i < 4; i++) if (wheelT[i] != null) wheelT[i].rotation = transform.rotation * Quaternion.Euler(0f, i < 2 ? steer : 0f, 0f) * Quaternion.Euler(spin, 0f, 0f) * wheelRest[i];
            bobT += dt * (6f + speed);
            Model.localPosition = modelRest + Vector3.up * (Mathf.Sin(bobT) * 0.008f * Mathf.Clamp01(speed / 6f)) ;
            Audio.engineLevel = 0.6f + 0.4f * Mathf.Clamp01(speed / 8f);
        }
        public void EndScripted(bool physics)
        {
            if (!Scripted) return;
            Scripted = false; Model.localPosition = modelRest; Car.enabled = true;
            rb.isKinematic = !physics; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            if (physics) StartCoroutine(LogHeights());
        }
        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        private Transform T(string n) => named.TryGetValue(n, out var t) ? t : null;
        private Vector3 L(Transform t) => transform.InverseTransformPoint(t.position);
        private Bounds LocalBounds(Transform t)
        {
            var r = t.GetComponent<Renderer>(); if (r == null) return new Bounds(L(t), Vector3.zero);
            var b = r.bounds; var lb = new Bounds(transform.InverseTransformPoint(b.center), Vector3.zero);
            foreach (var c in new[] { b.min, b.max, new Vector3(b.min.x, b.min.y, b.max.z), new Vector3(b.max.x, b.max.y, b.min.z) }) lb.Encapsulate(transform.InverseTransformPoint(c));
            return lb;
        }

        /// <summary>Turn the model so its front wheels point along +Z and its wheels stand on y = 0.</summary>
        private void AlignModel()
        {
            Transform fl = T("SM_Jeep_Wheel_FL"), fr = T("SM_Jeep_Wheel_FR"), rl = T("SM_Jeep_Wheel_RL"), rr = T("SM_Jeep_Wheel_RR");
            if (fl == null || rl == null) { Debug.LogWarning("[Jeep] wheel objects not found in the model"); return; }
            Vector3 f = (L(fl) + L(fr)) * 0.5f - (L(rl) + L(rr)) * 0.5f; f.y = 0f;
            if (f.sqrMagnitude > 0.01f) Model.localRotation = Quaternion.FromToRotation(f.normalized, Vector3.forward) * Model.localRotation;
            float low = float.MaxValue;
            foreach (var w in new[] { fl, fr, rl, rr }) { var b = LocalBounds(w); low = Mathf.Min(low, b.min.y); }
            Model.localPosition += Vector3.up * -low;
        }

        // ------------------------------------------------------------------ physics
        private WheelCollider Wheel(Transform mesh, string n)
        {
            var b = LocalBounds(mesh);
            var g = new GameObject(n); g.transform.SetParent(transform, false);
            g.transform.localPosition = b.center + Vector3.up * 0.12f;
            var wc = g.AddComponent<WheelCollider>();
            wc.radius = b.extents.y; wc.mass = 30f; wc.wheelDampingRate = 0.6f; wc.suspensionDistance = 0.24f; wc.forceAppPointDistance = 0.12f;
            var sp = wc.suspensionSpring; sp.spring = 38000f; sp.damper = 4200f; sp.targetPosition = 0.45f; wc.suspensionSpring = sp;
            var fw = wc.forwardFriction; fw.stiffness = 1.5f; wc.forwardFriction = fw;
            var sw = wc.sidewaysFriction; sw.stiffness = 2.1f; wc.sidewaysFriction = sw;   // grippy: the jeep must not slide on the road
            return wc;
        }

        private struct PartSnapshot
        {
            public Transform part, parent;
            public Vector3 localPos, localScale;
            public Quaternion localRot;
            public Mesh mesh;
            public Material[] mats;
        }
        private readonly List<PartSnapshot> partSnapshots = new List<PartSnapshot>();
        private float baseMotorForce = 1400f;
        private float repairToastUntil = -10f;

        private void BuildPhysics()
        {
            rb = gameObject.GetComponent<Rigidbody>(); if (rb == null) rb = gameObject.AddComponent<Rigidbody>();   // not "??": Unity's null check is needed here
            rb.mass = 1500f; rb.linearDamping = 0.05f; rb.angularDamping = 0.6f; rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var body = T("SM_Jeep_Body"); var top = T("SM_Jeep_Hardtop");
            if (body != null)
            {
                var b = LocalBounds(body); var c = gameObject.AddComponent<BoxCollider>();
                float bottom = 0.5f; c.center = new Vector3(b.center.x, (bottom + Mathf.Min(b.max.y, 1.15f)) * 0.5f, b.center.z); c.size = new Vector3(b.size.x, Mathf.Min(b.max.y, 1.15f) - bottom, b.size.z + 0.25f);
                if (top != null) { var tb = LocalBounds(top); var cc = gameObject.AddComponent<BoxCollider>(); cc.center = new Vector3(tb.center.x, (1.15f + tb.max.y) * 0.5f, tb.center.z); cc.size = new Vector3(tb.size.x, tb.max.y - 1.15f, tb.size.z); }
            }
            Car = gameObject.AddComponent<CarController>();
            baseMotorForce = Car.motorForce;
            Transform fl = T("SM_Jeep_Wheel_FL"), fr = T("SM_Jeep_Wheel_FR"), rl = T("SM_Jeep_Wheel_RL"), rr = T("SM_Jeep_Wheel_RR");
            // name the sides from where the wheels really are after alignment (left = -X in Unity)
            if (L(fl).x > L(fr).x) { var t = fl; fl = fr; fr = t; }
            if (L(rl).x > L(rr).x) { var t = rl; rl = rr; rr = t; }
            Car.frontLeftWheel = Wheel(fl, "WC_FrontLeft"); Car.frontRightWheel = Wheel(fr, "WC_FrontRight");
            Car.rearLeftWheel = Wheel(rl, "WC_RearLeft"); Car.rearRightWheel = Wheel(rr, "WC_RearRight");
            Car.frontLeftWheelTransform = fl; Car.frontRightWheelTransform = fr; Car.rearLeftWheelTransform = rl; Car.rearRightWheelTransform = rr;
            Car.steeringWheel = T("SM_Jeep_SteeringWheel");
        }

        // ------------------------------------------------------------------ lights
        private Light Lamp(Transform at, LightType type, Color c, float range, float angle, Vector3 dirLocal)
        {
            var g = new GameObject("LGT_" + at.name); g.transform.SetParent(at, false); g.transform.position = at.GetComponent<Renderer>() != null ? at.GetComponent<Renderer>().bounds.center : at.position;
            g.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(dirLocal));
            var l = g.AddComponent<Light>(); l.type = type; l.color = c; l.range = range; l.shadows = LightShadows.None; l.enabled = false;
            if (type == LightType.Spot) { l.spotAngle = angle; l.innerSpotAngle = angle * 0.6f; }
            return l;
        }

        private void BuildLights()
        {
            Lights = gameObject.AddComponent<CarLights>();
            var head = new List<Light>(); var stop = new List<Light>(); var rev = new List<Light>(); var tl = new List<Light>(); var tr = new List<Light>();
            var headR = new List<Renderer>(); var stopR = new List<Renderer>(); var revR = new List<Renderer>(); var tlR = new List<Renderer>(); var trR = new List<Renderer>();
            foreach (var kv in named)
            {
                var t = kv.Value; string n = kv.Key; var r = t.GetComponent<Renderer>(); if (r == null) continue;
                bool left = L(t).x < 0f, front = L(t).z > 0f;
                if (n.StartsWith("SM_Jeep_Headlight")) { head.Add(Lamp(t, LightType.Spot, new Color(1f, 0.93f, 0.8f), 32f, 62f, new Vector3(0f, -0.08f, 1f))); headR.Add(r); }
                else if (n.StartsWith("SM_Jeep_SpotLight") || n.StartsWith("SM_Jeep_FogLight")) headR.Add(r);
                else if (n.StartsWith("SM_Jeep_TailLight") && !n.Contains("Bezel")) { stop.Add(Lamp(t, LightType.Point, new Color(1f, 0.05f, 0.03f), 3.5f, 0f, Vector3.back)); stopR.Add(r); }
                else if (n.StartsWith("SM_Jeep_ReverseLight")) { rev.Add(Lamp(t, LightType.Spot, Color.white, 9f, 80f, new Vector3(0f, -0.2f, -1f))); revR.Add(r); }
                else if (n.StartsWith("SM_Jeep_Indicator")) { var l = Lamp(t, LightType.Point, new Color(1f, 0.55f, 0f), 2.5f, 0f, front ? Vector3.forward : Vector3.back); if (left) { tl.Add(l); tlR.Add(r); } else { tr.Add(l); trR.Add(r); } }
            }
            Lights.head = head.ToArray(); Lights.stop = stop.ToArray(); Lights.reverse = rev.ToArray(); Lights.turnLeft = tl.ToArray(); Lights.turnRight = tr.ToArray();
            Lights.headMesh = headR.ToArray(); Lights.stopMesh = stopR.ToArray(); Lights.reverseMesh = revR.ToArray(); Lights.turnLeftMesh = tlR.ToArray(); Lights.turnRightMesh = trR.ToArray();
            Car.lights = Lights;
            partSnapshots.Clear();
            foreach (var kv in named)
            {
                var t = kv.Value;
                if (t == null || kv.Key.Contains("_Wheel_")) continue;
                var mf = t.GetComponent<MeshFilter>();
                var mr = t.GetComponent<MeshRenderer>();
                partSnapshots.Add(new PartSnapshot
                {
                    part = t,
                    parent = t.parent,
                    localPos = t.localPosition,
                    localRot = t.localRotation,
                    localScale = t.localScale,
                    mesh = mf != null ? mf.sharedMesh : null,
                    mats = mr != null ? (Material[])mr.sharedMaterials.Clone() : null
                });
            }
        }

        // ------------------------------------------------------------------ doors and hood
        private void CacheMovingParts()
        {
            steeringCol = T("SM_Jeep_SteeringWheel");
            driverSide = steeringCol != null && L(steeringCol).x < 0f ? -1f : 1f;
            foreach (var n in new[] { "SM_Jeep_Door_FL", "SM_Jeep_Door_FR" })
            {
                var d = T(n); if (d != null && Mathf.Sign(L(d).x) == driverSide) driverDoor = d;
            }
            if (driverDoor != null) { driverDoorClosed = driverDoor.localRotation; driverDoorSign = OpenSign(driverDoor, Vector3.up, b => Mathf.Abs(b.center.x)); }
            hood = T("SM_Jeep_Hood");
            if (hood != null) { hoodClosed = hood.localRotation; hoodSign = OpenSign(hood, Vector3.right, b => b.center.y); }
        }

        /// <summary>Which way a hinged part opens: try a small turn both ways and keep the one that moves it outward / upward.</summary>
        private float OpenSign(Transform part, Vector3 axis, System.Func<Bounds, float> score)
        {
            var keep = part.localRotation; float before = score(LocalBounds(part));
            part.localRotation = keep * Quaternion.AngleAxis(25f, axis); float plus = score(LocalBounds(part));
            part.localRotation = keep; return plus > before ? 1f : -1f;
        }

        public IEnumerator DriverDoor(bool open, float seconds = 0.6f)
        {
            if (driverDoor == null) yield break;
            Quaternion from = driverDoor.localRotation, to = open ? driverDoorClosed * Quaternion.AngleAxis(68f * driverDoorSign, Vector3.up) : driverDoorClosed;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds) { driverDoor.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            driverDoor.localRotation = to;
            AudioManager.Instance?.PlaySFX(open ? "sfx_door_open" : "sfx_door_close", 0.6f);
        }

        public IEnumerator Hood(bool open, float seconds = 0.9f)
        {
            if (hood == null) yield break;
            Quaternion from = hood.localRotation, to = open ? hoodClosed * Quaternion.AngleAxis(55f * hoodSign, Vector3.right) : hoodClosed;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds) { hood.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            hood.localRotation = to;
        }

        /// <summary>World point just outside the driver's door, where the player gets in and out.</summary>
        public Vector3 DriverExitPoint
        {
            get
            {
                Vector3 p = driverDoor != null ? L(driverDoor) : new Vector3(driverSide * 0.9f, 0f, 0.6f);
                return transform.TransformPoint(new Vector3(driverSide * 1.75f, 0.05f, p.z - 0.55f));
            }
        }
        public Vector3 SeatPoint => steeringCol != null ? steeringCol.position + transform.forward * -0.45f + Vector3.down * 0.35f : transform.TransformPoint(new Vector3(driverSide * 0.4f, 1.0f, 0.1f));

        // ------------------------------------------------------------------ getting in and out (E)
        public string Id => "vehicle_jeep";
        public string Prompt => Occupied ? "" : DamageState >= 3 ? "The jeep is wrecked" : "Drive the jeep";
        public Transform Anchor { get { if (anchor == null) { anchor = new GameObject("JeepDoorAnchor").transform; anchor.SetParent(transform, false); } anchor.position = DriverExitPoint; return anchor; } }
        private Transform anchor;
        public float Radius => Occupied ? 0f : 2.4f;

        public void Interact()
        {
            if (Occupied || Transitioning || DamageState >= 3) return;
            var pc = FindAnyObjectByType<PlayerController>(); if (pc == null) return;
            StartCoroutine(GetIn(pc));
        }

        private IEnumerator GetIn(PlayerController pc)
        {
            Transitioning = true; Occupied = true; enteredFrame = Time.frameCount; player = pc;
            pc.enabled = false;
            LegacyOfTemples.Core.EventBus.RaiseHidePrompt();
            var drv = pc.GetComponent<NpcAnimDriver>(); bool addedDriver = drv == null;
            if (addedDriver) drv = pc.gameObject.AddComponent<NpcAnimDriver>();
            var capsule = pc.GetComponent<CharacterController>();
            var grounding = pc.GetComponent<CharacterGrounding>();
            Vector3 door = DriverExitPoint;
            for (float t = 0f; t < 2f && Vector3.Distance(Flat(pc.transform.position), Flat(door)) > 0.15f; t += Time.deltaTime)
            {   // walk to the door
                Vector3 d = Flat(door - pc.transform.position); pc.transform.rotation = Quaternion.Slerp(pc.transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 8f);
                capsule.Move(d.normalized * Mathf.Min(1.5f * Time.deltaTime, d.magnitude) + Vector3.down * (2f * Time.deltaTime));
                grounding?.SnapToGround(); yield return null;
            }
            if (addedDriver) Destroy(drv);
            if (Vector3.Distance(Flat(pc.transform.position), Flat(door)) > 0.4f)
            {
                pc.enabled = true; Occupied = false; Transitioning = false; player = null;
                ScreenText.Panel("jeep_access", null, "Walk around the obstacle to reach the driver's door.", PanelAnchor.TopCenter, 500f, null, false, 3);
                yield break;
            }
            yield return DriverDoor(true, 0.45f);
            SeatPlayer(true);
            yield return DriverDoor(false, 0.4f);
            orbit = FindAnyObjectByType<OrbitCamera>(); if (orbit != null) orbit.enabled = false;
            var cam = Camera.main;
            if (cam != null) { follow = cam.GetComponent<CarFollowCamera>(); if (follow == null) follow = cam.gameObject.AddComponent<CarFollowCamera>(); follow.target = transform; follow.car = Car; follow.enabled = true; follow.Snap(); }
            Car.Driven = true; Lights.SetHeadLights(true); Audio.engineLevel = 1f;
            Transitioning = false;
        }

        public IEnumerator GetOut()
        {
            if (!Occupied || player == null || Transitioning) yield break;
            if (!TryFindSafeExit(player, out Vector3 p))
            {
                ScreenText.Panel("jeep_access", null, "There is no clear place to get out. Park away from the wall or edge.", PanelAnchor.TopCenter, 560f, null, false, 3);
                yield break;
            }
            Transitioning = true;
            Car.Driven = false; Lights.SignalsOff();
            // Let the parking brakes settle the vehicle before enabling the player's capsule.
            for (float t = 0f; t < 1.5f && Car.CurrentSpeed > 0.4f; t += Time.deltaTime) yield return null;
            yield return DriverDoor(true, 0.45f);
            // Recheck after the car and door have moved.
            if (!TryFindSafeExit(player, out p))
            {
                yield return DriverDoor(false, 0.4f);
                Car.Driven = DamageState < 3; Transitioning = false;
                yield break;
            }
            if (player != null) SeatDriver(player.transform, false, p);
            player.Teleport(p, Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up)));
            player.enabled = true;
            if (follow != null) follow.enabled = false;
            if (orbit != null) { orbit.enabled = true; orbit.SetTarget(player.transform); }
            yield return DriverDoor(false, 0.4f);
            Occupied = false; player = null; Transitioning = false;
        }

        /// <summary>Find walkable ground and enough room for the real character capsule.
        /// Try nearby spots first, then the passenger side and rear; never place a driver inside a wall.</summary>
        public bool TryFindSafeExit(PlayerController pc, out Vector3 position)
        {
            position = default;
            if (pc == null) return false;
            var cc = pc.GetComponent<CharacterController>();
            var grounding = pc.GetComponent<CharacterGrounding>();
            if (cc == null || grounding == null) return false;
            Vector3 door = DriverExitPoint;
            Vector3 side = Vector3.ProjectOnPlane(transform.right * driverSide, Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3[] candidates = { door, door + forward * 1.4f, door - forward * 1.4f,
                door + side * 0.65f, door - side * 3.5f, door - side * 3.5f + forward * 1.4f,
                door - side * 3.5f - forward * 1.4f, transform.position - forward * 3.3f };
            Quaternion face = Quaternion.LookRotation(forward.sqrMagnitude > 0.01f ? forward : Vector3.forward);
            Vector3 scale = pc.transform.lossyScale;
            float radius = cc.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfHeight = Mathf.Max(radius, cc.height * Mathf.Abs(scale.y) * 0.5f);
            foreach (Vector3 candidate in candidates)
            {
                var hits = Physics.RaycastAll(new Vector3(candidate.x, transform.position.y + 1.5f, candidate.z), Vector3.down, 3.5f, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(pc.transform) ||
                        hit.collider.GetComponentInParent<CharacterController>() != null || hit.normal.y < Mathf.Cos(cc.slopeLimit * Mathf.Deg2Rad)) continue;
                    Vector3 root = new Vector3(candidate.x, hit.point.y + 0.005f - grounding.FootOffset * scale.y, candidate.z);
                    Vector3 centre = root + face * Vector3.Scale(cc.center, scale);
                    Vector3 offset = Vector3.up * (halfHeight - radius);
                    bool blocked = false;
                    foreach (var obstacle in Physics.OverlapCapsule(centre - offset, centre + offset, radius, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (obstacle.transform.IsChildOf(pc.transform)) continue;
                        blocked = true; break;
                    }
                    if (!blocked) { position = root; return true; }
                    // A solid upper surface prevents exiting through it onto a hidden lower floor.
                    break;
                }
            }
            return false;
        }

        /// <summary>The investigator sits in the right-hand driver's seat, visible through the windows, hands on the wheel.</summary>
        private void SeatPlayer(bool sit) { if (player != null) SeatDriver(player.transform, sit); }
        /// <summary>Puts a character in (or takes them out of) the driver's seat with the seated pose. Also used by the intro.</summary>
        public void SeatDriver(Transform who, bool sit, Vector3? exitAt = null)
        {
            if (who == null) return; var player = who;
            var sp = player.GetComponent<SeatedPose>();
            if (!sit)
            {   // order matters: stop the pose, put him outside, and only then switch his collision back on (no push, no jump)
                if (sp != null) { sp.enabled = false; Destroy(sp); }
                player.SetParent(null, true);
                if (exitAt.HasValue) player.position = exitAt.Value;
                player.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up));
                Physics.SyncTransforms();
            }
            var cc = player.GetComponent<CharacterController>(); if (cc != null) cc.enabled = !sit;
            foreach (var c in player.GetComponentsInChildren<Collider>()) if (!(c is CharacterController)) c.enabled = !sit;
            if (sit)
            {
                player.transform.SetParent(transform, true);
                if (sp == null) sp = player.gameObject.AddComponent<SeatedPose>();
                sp.car = transform; sp.seatLocal = DriverSeatLocal;
            }

        }
        /// <summary>Top of the driver's seat cushion (the seat on the steering-wheel side), in the jeep's space.</summary>
        public Vector3 DriverSeatLocal
        {
            get
            {
                Transform best = null;
                foreach (var n in new[] { "SM_Jeep_Seat_FL", "SM_Jeep_Seat_FR" }) { var t = T(n); if (t != null && Mathf.Sign(L(t).x) == driverSide) best = t; }
                if (best == null) return new Vector3(driverSide * 0.4f, 0.95f, 0f);
                var b = LocalBounds(best); return new Vector3(b.center.x, b.min.y + 0.30f, b.center.z + 0.02f);   // hips about 0.15 m above the cushion, head clear of the roof
            }
        }

        private void SetPlayerVisible(bool on)
        {
            if (player == null) return;
            foreach (var r in player.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
            var cc = player.GetComponent<CharacterController>(); if (cc != null) cc.enabled = on;
            foreach (var c in player.GetComponentsInChildren<Collider>()) if (!(c is CharacterController)) c.enabled = on;
        }

        private void Update()
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            bool inFight = CombatState.InCombat || Brawl.Active != null;
            if (!Transitioning && !InputGate.GameplayBlocked && !inFight)
            {
                var nearPc = player != null ? player : FindAnyObjectByType<PlayerController>();
                bool closeOnFoot = !Occupied && nearPc != null && Vector3.Distance(nearPc.transform.position, transform.position) <= 28f;
                if (kb != null && kb.rKey.wasPressedThisFrame && (Occupied || closeOnFoot))
                {
                    RepairAndResetToRoad();
                }
                else if (closeOnFoot && (DamageState > 0 || Vector3.Dot(transform.up, Vector3.up) < 0.7f) && nearPc != null && Vector3.Distance(nearPc.transform.position, transform.position) <= 7f)
                {
                    ScreenText.Panel("jeep_repair_hint", null, $"{ScreenText.Key("R")} Repair & Reset Jeep to Road", PanelAnchor.TopCenter, 420f, null, false, 1);
                }
            }
            if (Time.unscaledTime < repairToastUntil)
                ScreenText.Panel("jeep_reset", null, "Jeep repaired and reset cleanly onto the road.", PanelAnchor.TopCenter, 480f, null, false, 3);
            if (Occupied && !Transitioning && Car.Driven && Time.frameCount > enteredFrame + 2 && !InputGate.GameplayBlocked)
            {
                bool exit = (kb != null && kb.eKey.wasPressedThisFrame) || (gp != null && gp.buttonWest.wasPressedThisFrame) || VirtualInput.Down("interact");
                if (exit && Car.CurrentSpeed < 3f) StartCoroutine(GetOut());
                bool horn = (kb != null && kb.gKey.isPressed) || (gp != null && gp.leftStickButton.isPressed);
                Horn(horn);
                ScreenText.Panel("jeep_ui", null,
                    $"<size=140%><b>{Car.CurrentSpeed * 3.6f:0}</b></size> km/h   <color={ScreenText.Gold}>{Car.EffectiveGear.ToString()[0]}</color>" + (DamageState > 0 ? $"   <color={ScreenText.Coral}>{StateName}</color>" : "") +
                    $"\n{ScreenText.Key("W S")} Drive  {ScreenText.Key("A D")} Steer  {ScreenText.Key("Space")} Brake\n{ScreenText.Key("L")} Lights  {ScreenText.Key("G")} Horn  {ScreenText.Key("R")} Repair/Road  {ScreenText.Key("E")} Get out",
                    PanelAnchor.BottomLeft, 560f, null, false, 2);
            }
        }

        private AudioSource hornSrc;
        /// <summary>Two-tone horn made in code (no downloaded sound), held while G is down.</summary>
        private void Horn(bool on)
        {
            if (hornSrc == null)
            {
                if (!on) return;
                int sr = 44100, n = sr; var data = new float[n];
                for (int i = 0; i < n; i++) { float t = i / (float)sr; float v = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 415f * t)) * 0.5f + Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 520f * t)) * 0.5f; data[i] = v * 0.18f; }
                var clip = AudioClip.Create("jeep_horn", n, 1, sr, false); clip.SetData(data, 0);
                hornSrc = gameObject.AddComponent<AudioSource>(); hornSrc.clip = clip; hornSrc.loop = true; hornSrc.spatialBlend = 0.6f; hornSrc.volume = 0.55f; hornSrc.playOnAwake = false;
            }
            if (on && !hornSrc.isPlaying) hornSrc.Play(); else if (!on && hornSrc.isPlaying) hornSrc.Stop();
        }

        // ------------------------------------------------------------------ damage
        public string StateName => DamageState == 0 ? "Normal" : DamageState == 1 ? "Damaged" : DamageState == 2 ? "Wrecked" : "Destroyed";
        private readonly List<GameObject> debris = new List<GameObject>();
        private const int MaxDebris = 18;

        private string Zone(Vector3 local)
        {
            if (local.y > 1.7f) return "top";   // roof and rack of the 2.0 m tall jeep
            if (local.z > 1.35f) return "front";
            if (local.z < -1.35f) return "rear";
            return local.x < 0f ? "left" : "right";
        }

        private float lastHitTime = -10f, lastHitDv;
        private void OnCollisionEnter(Collision c)
        {
            if (rb == null || rb.isKinematic || c.contactCount == 0) return;
            if (c.collider.GetComponentInParent<PlayerController>() != null || debris.Contains(c.gameObject) || c.collider.transform.IsChildOf(transform)) return;
            var cp = c.GetContact(0);
            if (Vector3.Dot(cp.normal, Vector3.up) > 0.7f) return;          // driving over a kerb or a step is not a crash
            if (c.rigidbody != null && c.rigidbody.mass < 60f) return;      // small loose things (props, fallen parts) do not damage it
            float dv = Mathf.Abs(Vector3.Dot(c.relativeVelocity, cp.normal));   // closing speed along the hit: overlap pushes do not count
            Audio?.Impact(dv);
            if (dv < 2.2f) return;                                          // bumps and scrapes do nothing
            // the bounce-back of the same crash arrives a few frames later: within 0.5 s only the part stronger than the first hit counts
            float amount = Mathf.Pow(dv - 2.2f, 1.3f) * 3.5f;
            if (Time.time - lastHitTime < 0.5f) { if (dv <= lastHitDv) return; amount -= Mathf.Pow(lastHitDv - 2.2f, 1.3f) * 3.5f; }
            else lastHitDv = 0f;
            lastHitTime = Time.time; lastHitDv = Mathf.Max(lastHitDv, dv); LastImpact = lastHitDv;
            Debug.Log($"[Jeep] hit '{c.collider.name}' (parent '{(c.collider.transform.parent != null ? c.collider.transform.parent.name : "-")}') at {dv:0.0} m/s, damage {amount:0}");
            ApplyDamage(transform.InverseTransformPoint(cp.point), amount, cp.point, cp.normal);   // one hard wall crash (~40 km/h) leaves it Wrecked, not Destroyed
        }

        /// <summary>Damage at a point (root-local). Public so tests and the cinematic can exercise it.</summary>
        public void ApplyDamage(Vector3 local, float amount, Vector3 worldPoint, Vector3 normal)
        {
            string z = Zone(local); ZoneDamage[z] += amount; TotalDamage += amount;
            Fx.Burst("Spark", worldPoint, Mathf.Clamp((int)(amount * 2f), 6, 40), 4f, 0.08f, 0.5f, new Color(1f, 0.75f, 0.35f, 1f), 1.2f);
            Fx.Burst("Dust", worldPoint, 10, 1.5f, 0.4f, 1.2f, new Color(0.6f, 0.55f, 0.5f, 0.6f), 0.2f);
            AudioManager.Instance?.PlaySFX("sfx_metal_hit", Mathf.Clamp01(0.3f + amount / 40f));
            BreakParts(z, ZoneDamage[z], normal);
            int s = TotalDamage >= 100f ? 3 : TotalDamage >= 55f ? 2 : TotalDamage >= 20f ? 1 : 0;
            if (s > DamageState) SetState(s);
        }

        private static readonly (string prefix, float threshold, bool glass)[] Breakables =
        {
            ("SM_Jeep_Glass", 14f, true), ("SM_Jeep_Headlight", 12f, false), ("SM_Jeep_Indicator", 12f, false), ("SM_Jeep_FogLight", 14f, false),
            ("SM_Jeep_TailLight_", 12f, false), ("SM_Jeep_ReverseLight", 12f, false), ("SM_Jeep_Plate", 18f, false), ("SM_Jeep_TowHooks", 40f, false),
            ("SM_Jeep_Bumper", 30f, false), ("SM_Jeep_BullBar", 38f, false), ("SM_Jeep_MudFlaps", 22f, false), ("SM_Jeep_SideStep", 34f, false),
            ("SM_Jeep_Door_", 48f, false), ("SM_Jeep_Hood", 55f, false), ("SM_Jeep_SpotLight", 20f, false), ("SM_Jeep_RoofRack", 35f, false), ("SM_Jeep_Antenna", 12f, false),
        };

        private void BreakParts(string zone, float zoneDamage, Vector3 normal)
        {
            foreach (var kv in new List<KeyValuePair<string, Transform>>(named))
            {
                var t = kv.Value; if (t == null || !t.gameObject.activeInHierarchy || t.parent == null || !t.IsChildOf(transform)) continue;
                foreach (var b in Breakables)
                {
                    if (!kv.Key.StartsWith(b.prefix) || zoneDamage < b.threshold) continue;
                    var r = t.GetComponent<Renderer>(); if (r == null || !r.enabled) break;
                    if (Zone(LocalBounds(t).center) != zone && !(zone == "top" && b.prefix.Contains("Roof"))) break;
                    if (b.glass) { Shatter(r); }
                    else if (kv.Key == "SM_Jeep_Hood" || kv.Key.StartsWith("SM_Jeep_Door_")) { if (driverDoor == t && Occupied) break; Detach(t, normal); }
                    else Detach(t, normal);
                    if (kv.Key.StartsWith("SM_Jeep_Headlight") || kv.Key.StartsWith("SM_Jeep_FogLight")) Lights.broken[0] = Lights.broken[0] || CountAlive("SM_Jeep_Headlight") == 0;
                    if (kv.Key.StartsWith("SM_Jeep_TailLight_")) Lights.broken[1] = CountAlive("SM_Jeep_TailLight_") == 0;
                    if (kv.Key.StartsWith("SM_Jeep_ReverseLight")) Lights.broken[2] = CountAlive("SM_Jeep_ReverseLight") == 0;
                    PartsBroken++;
                    break;
                }
            }
        }

        private int CountAlive(string prefix) { int n = 0; foreach (var kv in named) if (kv.Key.StartsWith(prefix) && kv.Value != null && kv.Value.IsChildOf(transform)) { var r = kv.Value.GetComponent<Renderer>(); if (r != null && r.enabled) n++; } return n; }

        private void Shatter(Renderer r)
        {
            r.enabled = false;
            Fx.Burst("Spark", r.bounds.center, 34, 2.5f, 0.05f, 1.2f, new Color(0.75f, 0.9f, 1f, 0.9f), 2.5f);  // glass shards
            AudioManager.Instance?.PlaySFX("sfx_glass", 0.6f);
        }

        private void Detach(Transform t, Vector3 normal)
        {
            var r = t.GetComponent<Renderer>(); var b = r != null ? r.bounds : new Bounds(t.position, Vector3.one * 0.3f);
            t.SetParent(null, true);
            foreach (var l in t.GetComponentsInChildren<Light>()) l.enabled = false;
            var col = t.gameObject.AddComponent<BoxCollider>(); col.center = t.InverseTransformPoint(b.center); col.size = Vector3.Max(t.InverseTransformVector(b.size).Abs(), Vector3.one * 0.05f);
            foreach (var own in GetComponentsInChildren<Collider>()) if (own != col) Physics.IgnoreCollision(col, own, true);   // a falling part never hits its own jeep
            var prb = t.gameObject.AddComponent<Rigidbody>(); prb.mass = Mathf.Clamp(b.size.x * b.size.y * b.size.z * 200f, 2f, 45f);
            prb.linearVelocity = (rb != null ? rb.linearVelocity : Vector3.zero) * 0.6f - normal * 1.5f + Vector3.up * 2f; prb.angularVelocity = Random.insideUnitSphere * 4f;
            debris.Add(t.gameObject);
            while (debris.Count > MaxDebris) { var old = debris[0]; debris.RemoveAt(0); if (old != null) old.SetActive(false); }   // pooled: oldest pieces are put away
            StartCoroutine(Cleanup(t.gameObject, 25f));
        }

        private IEnumerator Cleanup(GameObject g, float after) { yield return new WaitForSeconds(after); if (g != null) { g.SetActive(false); debris.Remove(g); } }

        private void SetState(int s)
        {
            DamageState = s;
            var body = T("SM_Jeep_Body"); var hd = T("SM_Jeep_Hood");
            Swap(body, bodyStates, s); if (hd != null && hd.IsChildOf(transform)) Swap(hd, hoodStates, s);
            // Every painted panel still on the car (doors, cabin, hood) takes the same worn or burnt paint as the body, so a wreck is not half clean.
            var worn = s >= 3 ? burntPaint : scuffedPaint;
            if (worn != null)
                foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
                {
                    var ms = r.sharedMaterials; bool changed = false;
                    for (int i = 0; i < ms.Length; i++)
                        if (ms[i] != null && ms[i] != worn && (ms[i].name.Contains("Paint_Body") || ms[i] == scuffedPaint)) { ms[i] = worn; changed = true; if (s >= 3) r.SetPropertyBlock(null, i); }
                    if (changed) r.sharedMaterials = ms;
                }
            if (s >= 3)   // a destroyed jeep has no glass left
                foreach (var kv in named) if (kv.Key.StartsWith("SM_Jeep_Glass") && kv.Value != null) { var gr = kv.Value.GetComponent<Renderer>(); if (gr != null) gr.enabled = false; }
            if (s >= 2 && smoke == null) smoke = Smoke();
            if (s >= 3) { Car.motorForce = 0f; Audio.engineLevel = 0f; Lights.broken[0] = true; if (Occupied) StartCoroutine(GetOut()); }
            Debug.Log($"[Jeep] damage state {StateName} (total {TotalDamage:0}, parts broken {PartsBroken})");
        }

        private void Swap(Transform t, Mesh[] states, int s)
        {
            if (t == null || s >= states.Length || states[s] == null) return;
            var mf = t.GetComponent<MeshFilter>(); var mr = t.GetComponent<MeshRenderer>(); if (mf == null || mr == null) return;
            mf.sharedMesh = states[s];
            var paint = s >= 3 ? burntPaint : scuffedPaint;
            if (paint == null) return;
            var mats = new Material[Mathf.Max(1, states[s].subMeshCount)];
            for (int i = 0; i < mats.Length; i++) mats[i] = i == 0 ? paint : (interior != null ? interior : paint);
            mr.sharedMaterials = mats;
        }

        public void RepairAndResetToRoad()
        {
            foreach (var s in partSnapshots)
            {
                if (s.part == null) continue;
                bool wasDetached = s.part.parent != s.parent || debris.Contains(s.part.gameObject);
                if (wasDetached)
                {
                    var prb = s.part.GetComponent<Rigidbody>(); if (prb != null) Destroy(prb);
                    var pcol = s.part.GetComponent<BoxCollider>(); if (pcol != null) Destroy(pcol);
                }
                s.part.gameObject.SetActive(true);
                if (s.parent != null) s.part.SetParent(s.parent, false);
                s.part.localPosition = s.localPos;
                s.part.localRotation = s.localRot;
                s.part.localScale = s.localScale;
                var mf = s.part.GetComponent<MeshFilter>();
                if (mf != null && s.mesh != null) mf.sharedMesh = s.mesh;
                var mr = s.part.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = true;
                    if (s.mats != null && s.mats.Length > 0) mr.sharedMaterials = s.mats;
                    for (int i = 0; i < mr.sharedMaterials.Length; i++) mr.SetPropertyBlock(null, i);
                }
                foreach (var l in s.part.GetComponentsInChildren<Light>(true)) l.enabled = false;
            }
            debris.Clear();
            TotalDamage = 0f; ZoneDamage.Clear(); PartsBroken = 0; DamageState = 0;
            if (smoke != null) { Destroy(smoke.gameObject); smoke = null; }
            if (Lights != null && Lights.broken != null) for (int i = 0; i < Lights.broken.Length; i++) Lights.broken[i] = false;
            if (Car != null) { Car.motorForce = baseMotorForce; Car.Driven = Occupied; }
            if (driverDoor != null) driverDoor.localRotation = driverDoorClosed;
            if (hood != null) hood.localRotation = hoodClosed;
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

            Vector3 refPos = Occupied ? transform.position : (FindAnyObjectByType<PlayerController>() != null ? FindAnyObjectByType<PlayerController>().transform.position : transform.position);
            FindSafeRoadPose(refPos, out Vector3 safePos, out Quaternion safeRot);
            transform.SetPositionAndRotation(safePos, safeRot);
            if (follow != null && Occupied) follow.Snap();
            AudioManager.Instance?.PlaySFX("sfx_checkpoint", 0.7f);
            repairToastUntil = Time.unscaledTime + 2.8f;
            Debug.Log($"[Jeep] Repaired and reset to road at {safePos}");
        }

        private void FindSafeRoadPose(Vector3 around, out Vector3 safePos, out Quaternion safeRot)
        {
            var list = new List<(Vector3 pos, float yaw)>();
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (Constants.IsNewWorld(scene))
            {
                Vector3[] wps = {
                    new Vector3(0f, 0.2f, -38f), new Vector3(0f, 0.2f, -5f), new Vector3(0f, 0.2f, 35f), new Vector3(0f, 0.2f, 75f),
                    new Vector3(-24f, 0.2f, -8f), new Vector3(-52f, 0.2f, -20f),
                    new Vector3(0f, 0.2f, 92f), new Vector3(-30f, 0.2f, 100f), new Vector3(32f, 0.2f, 108f),
                    new Vector3(26f, 0.2f, 152f), new Vector3(26f, 0.2f, 176f), new Vector3(0f, 0.2f, 195f),
                    new Vector3(0f, 0.2f, 250f), new Vector3(0f, 0.2f, 310f), new Vector3(0f, 0.2f, 370f), new Vector3(0f, 0.2f, 412f)
                };
                foreach (var w in wps) list.Add((w, w.x < -10f && w.z < 50f ? 270f : 0f));
                list.Sort((a, b) => Vector3.SqrMagnitude(Flat(a.pos - around)).CompareTo(Vector3.SqrMagnitude(Flat(b.pos - around))));
            }
            float baseYaw = transform.eulerAngles.y;
            for (int r = 0; r <= 3; r++)
            {
                float d = r * 3.5f;
                Vector3[] dirs = { Vector3.zero, Vector3.right, -Vector3.right, Vector3.forward, -Vector3.forward };
                foreach (var dir in dirs) list.Add((around + dir * d, baseYaw));
            }
            foreach (var c in list)
            {
                if (!Physics.Raycast(c.pos + Vector3.up * 6f, Vector3.down, out RaycastHit h, 18f, ~0, QueryTriggerInteraction.Ignore)) continue;
                Vector3 p = new Vector3(c.pos.x, h.point.y + 0.15f, c.pos.z);
                Quaternion rot = Quaternion.Euler(0f, c.yaw, 0f);
                bool blocked = false;
                foreach (var hit in Physics.OverlapBox(p + Vector3.up * 1.45f, new Vector3(1.1f, 0.65f, 2.2f), rot, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.transform.CompareTag("Player")) continue;
                    blocked = true; break;
                }
                if (!blocked) { safePos = p; safeRot = rot; return; }
            }
            safePos = around + Vector3.up * 0.5f;
            safeRot = Quaternion.Euler(0f, baseYaw, 0f);
        }

        private ParticleSystem Smoke()
        {
            var g = new GameObject("JeepSmoke"); g.transform.SetParent(transform, false); g.transform.localPosition = new Vector3(0f, 1.2f, 1.6f);
            var ps = g.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.startLifetime = 3.5f; m.startSpeed = 1.2f; m.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f); m.startColor = new Color(0.18f, 0.17f, 0.16f, 0.5f); m.maxParticles = 120; m.simulationSpace = ParticleSystemSimulationSpace.World; m.gravityModifier = -0.08f;
            var e = ps.emission; e.rateOverTime = 14f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }); col.color = gr;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
            g.GetComponent<ParticleSystemRenderer>().sharedMaterial = Fx.Res("Dust");
            ps.Play(); return ps;
        }
    }

    internal static class V3 { public static Vector3 Abs(this Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z)); }
}
