using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds Assets/CarFootball/Scenes/CarFootballScene.unity from CarFootball.fbx.
/// Runs automatically once after compile (if the scene does not exist yet),
/// or manually via menu: CarFootball > Build Scene.
/// </summary>
[InitializeOnLoad]
public static class CarFootballSceneBuilder
{
    const string Root = "Assets/CarFootball";
    const string FbxPath = Root + "/Models/CarFootball.fbx";
    const string ScenePath = Root + "/Scenes/CarFootballScene.unity";
    const string MatDir = Root + "/Materials";
    const string PhysDir = Root + "/Physics";
    static readonly string ReportPath = Path.GetFullPath("Logs/CarFootballSetup.txt");

    static CarFootballSceneBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath) && File.Exists(FbxPath)) Build();
        };
    }

    [MenuItem("CarFootball/Build Scene")]
    public static void Build()
    {
        var log = new StringBuilder();
        try
        {
            EnsureFolder(Root, "Scenes"); EnsureFolder(Root, "Materials"); EnsureFolder(Root, "Physics");

            // ---- TextMeshPro essentials (needed for the HUD) ----
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length == 0)
            {
                var pi = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
                string tmp = pi != null ? Path.Combine(pi.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage") : "";
                if (File.Exists(tmp)) AssetDatabase.ImportPackage(tmp, false);
            }

            // ---- FBX import settings ----
            var importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
            if (importer == null) { AssetDatabase.ImportAsset(FbxPath, ImportAssetOptions.ForceSynchronousImport); importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath); }
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();

            // ---- Materials (URP Lit) ----
            var mGrass = MakeMat("Pitch_Green", new Color(0.12f, 0.6f, 0.15f));
            var mWall = MakeMat("Wall_White", new Color(0.92f, 0.92f, 0.92f));
            var mCar = MakeMat("Car_Red", new Color(0.85f, 0.05f, 0.05f));
            var mWheel = MakeMat("Wheel_Dark", new Color(0.05f, 0.05f, 0.05f));
            var mBall = MakeMat("Ball_Yellow", new Color(1f, 0.8f, 0f));

            // ---- Physics materials ----
            var bouncy = LoadOrCreate(PhysDir + "/BouncyBall.physicMaterial", () => new PhysicsMaterial("BouncyBall"));
            bouncy.dynamicFriction = 0f;
            bouncy.staticFriction = 0f;
            bouncy.bounciness = 0.5f;   // 0.8 launched the big ball out of the arena
            bouncy.bounceCombine = PhysicsMaterialCombine.Maximum;
            bouncy.frictionCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(bouncy);
            var carSlick = LoadOrCreate(PhysDir + "/CarBody.physicMaterial", () => new PhysicsMaterial("CarBody"));
            carSlick.dynamicFriction = 0.05f;
            carSlick.staticFriction = 0.05f;
            carSlick.bounciness = 0f;
            carSlick.frictionCombine = PhysicsMaterialCombine.Minimum;
            carSlick.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(carSlick);
            AssetDatabase.SaveAssets();

            // ---- New scene ----
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = "CarFootball";

            Transform beach = Find(inst.transform, "beach");
            Transform car = Find(inst.transform, "car");
            Transform ball = Find(inst.transform, "ball");

            // ---- Pitch ----
            SetMat(beach, mGrass);
            beach.gameObject.AddComponent<MeshCollider>();
            Bounds pitchB = beach.GetComponent<Renderer>().bounds;
            beach.gameObject.isStatic = true;
            // thick safety ground under the plane so nothing can tunnel through
            var ground = new GameObject("PitchGroundCollider");
            ground.transform.position = new Vector3(pitchB.center.x, pitchB.max.y - 0.5f, pitchB.center.z);
            var gbc = ground.AddComponent<BoxCollider>();
            gbc.size = new Vector3(pitchB.size.x + 16f, 1f, pitchB.size.z + 16f);
            ground.isStatic = true;
            log.AppendLine($"Pitch bounds: center {pitchB.center} size {pitchB.size}  scale {beach.localScale}");

            // ---- Walls ----
            Vector3 goalCenter = Vector3.zero; int goalParts = 0; float gapHalf = 99f;
            foreach (Transform t in inst.GetComponentsInChildren<Transform>())
            {
                if (!t.name.ToLower().Contains("wall") || t.GetComponent<Renderer>() == null) continue;
                SetMat(t, mWall);
                t.gameObject.AddComponent<BoxCollider>();
                t.gameObject.isStatic = true;
                var b = t.GetComponent<Renderer>().bounds;
                log.AppendLine($"{t.name}: center {b.center} size {b.size}");
                if (t.name.ToLower().StartsWith("goal")) { goalCenter += b.center; goalParts++; gapHalf = Mathf.Min(gapHalf, Mathf.Abs(b.center.x) - b.extents.x); }
            }
            goalCenter /= Mathf.Max(1, goalParts);
            log.AppendLine($"Goal opening centre: {goalCenter}");

            // ---- Car ----
            foreach (var r in car.GetComponentsInChildren<Renderer>())
                SetMat(r.transform, r.name.ToLower().StartsWith("wheel") ? mWheel : mCar);
            // Re-orient the parent so its +Z (forward) points at the goal, keeping child world poses.
            Bounds bodyB = FindBody(car).GetComponent<Renderer>().bounds;
            var kids = new Transform[car.childCount];
            for (int i = 0; i < kids.Length; i++) kids[i] = car.GetChild(i);
            foreach (var k in kids) k.SetParent(null, true);
            Bounds carB = kids[0].GetComponent<Renderer>().bounds;
            foreach (var k in kids) carB.Encapsulate(k.GetComponent<Renderer>().bounds);
            car.position = bodyB.center;
            Vector3 toGoal = goalCenter - car.position; toGoal.y = 0f;
            car.rotation = Quaternion.LookRotation(toGoal.normalized, Vector3.up);
            car.localScale = Vector3.one;
            foreach (var k in kids) k.SetParent(car, true);
            // One box collider on the parent covering body + wheels (no WheelColliders)
            var carCol = car.gameObject.AddComponent<BoxCollider>();
            Vector3 localSize = car.InverseTransformVector(carB.size);
            carCol.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            carCol.center = car.InverseTransformPoint(carB.center);
            carCol.sharedMaterial = carSlick;
            var carRb = car.gameObject.AddComponent<Rigidbody>();
            carRb.mass = 100f;
            carRb.useGravity = true;
            carRb.linearDamping = 0.4f;
            carRb.angularDamping = 3f;
            carRb.interpolation = RigidbodyInterpolation.Interpolate;
            carRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            carRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            var cc = car.gameObject.AddComponent<CarController>();
            cc.jumpForce = carRb.mass * 6f; // ~1.8 m jump
            cc.maxSpeed = 10f; cc.maxReverseSpeed = 5f; cc.acceleration = 14f; cc.steeringStrength = 130f;
            log.AppendLine($"Car: pos {car.position} fwd {car.forward} bounds {carB.center}/{carB.size} collider {carCol.center}/{carCol.size}");

            // ---- Ball ----
            SetMat(ball, mBall);
            var sc = ball.gameObject.AddComponent<SphereCollider>();
            sc.sharedMaterial = bouncy;
            var brb = ball.gameObject.AddComponent<Rigidbody>();
            brb.mass = 0.5f;
            brb.useGravity = true;
            brb.linearDamping = 0.1f;
            brb.angularDamping = 0.3f;
            brb.interpolation = RigidbodyInterpolation.Interpolate;
            brb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var ballB = ball.GetComponent<Renderer>().bounds;
            log.AppendLine($"Ball: bounds {ballB.center}/{ballB.size} scale {ball.localScale} radius(world) {sc.radius * ball.lossyScale.x}");

            // ---- Goal: posts, crossbar, side nets, back wall, roof + hidden trigger ----
            EnsureTag("Ball");
            ball.gameObject.tag = "Ball";
            float ballR = sc.radius * ball.lossyScale.x;
            float H = Mathf.Max(2.5f, ballR * 2f + 2.5f);   // goal height: room for a bouncing ball under the crossbar
            float D = ballR * 2f + 0.6f;                     // goal depth
            float postR = 0.15f, outer = gapHalf + 2f * postR;
            var mPost = MakeMat("Goal_Post_White", Color.white);
            var mNet = MakeMat("Goal_Net_Grey", new Color(0.75f, 0.75f, 0.78f));
            Vector3 outDir = goalCenter - pitchB.center; outDir.y = 0f; outDir.Normalize();
            var goal = new GameObject("Goal").transform;
            goal.SetPositionAndRotation(new Vector3(goalCenter.x, pitchB.max.y, goalCenter.z), Quaternion.LookRotation(outDir));
            Prim(PrimitiveType.Cylinder, "GoalPost_Left", goal, new Vector3(-(gapHalf + postR), H / 2f, 0f), Vector3.zero, new Vector3(postR * 2f, H / 2f, postR * 2f), mPost);
            Prim(PrimitiveType.Cylinder, "GoalPost_Right", goal, new Vector3(gapHalf + postR, H / 2f, 0f), Vector3.zero, new Vector3(postR * 2f, H / 2f, postR * 2f), mPost);
            Prim(PrimitiveType.Cylinder, "Crossbar", goal, new Vector3(0f, H, 0f), new Vector3(0f, 0f, 90f), new Vector3(postR * 2f, outer, postR * 2f), mPost);
            Prim(PrimitiveType.Cube, "GoalBackWall", goal, new Vector3(0f, H / 2f, D), Vector3.zero, new Vector3(outer * 2f, H, 0.1f), mNet);
            Prim(PrimitiveType.Cube, "GoalSide_Left", goal, new Vector3(-outer, H / 2f, D / 2f), Vector3.zero, new Vector3(0.1f, H, D), mNet);
            Prim(PrimitiveType.Cube, "GoalSide_Right", goal, new Vector3(outer, H / 2f, D / 2f), Vector3.zero, new Vector3(0.1f, H, D), mNet);
            Prim(PrimitiveType.Cube, "GoalRoof", goal, new Vector3(0f, H + 0.05f, D / 2f), Vector3.zero, new Vector3(outer * 2f, 0.1f, D), mNet);
            Prim(PrimitiveType.Cube, "GoalFloor", goal, new Vector3(0f, -0.04f, D / 2f), Vector3.zero, new Vector3(outer * 2f, 0.1f, D), mGrass);
            // Trigger starts one ball-radius behind the goal line -> fires when the ball centre crosses the line
            var trig = Prim(PrimitiveType.Cube, "GoalTrigger", goal, new Vector3(0f, H / 2f, (ballR + D - 0.1f) / 2f), Vector3.zero, new Vector3(gapHalf * 2f, H, D - 0.1f - ballR), null);
            trig.GetComponent<MeshRenderer>().enabled = false;
            trig.GetComponent<BoxCollider>().isTrigger = true;
            trig.AddComponent<GoalTrigger>();
            log.AppendLine($"Goal: gapHalf {gapHalf} H {H} D {D} trigger {trig.transform.position} / {trig.transform.lossyScale}");

            // ---- Invisible arena barriers ----
            // The FBX walls are only 1 m tall, so a hit ball flew out of the arena and could never be
            // scored. These invisible colliders (tall walls, a wall above the goal and a ceiling) keep
            // the ball in play while leaving the goal mouth open.
            const float top = 12f;
            var barriers = new GameObject("ArenaBarriers").transform;
            void Barrier(string n, Vector3 worldCenter, Quaternion rot, Vector3 size)
            {
                var go = new GameObject(n);
                go.transform.SetParent(barriers, false);
                go.transform.SetPositionAndRotation(worldCenter, rot);
                go.AddComponent<BoxCollider>().size = size;
                go.isStatic = true;
            }
            float y0 = pitchB.max.y;
            Vector3 pc = pitchB.center;
            Barrier("Barrier_Left", new Vector3(pc.x - pitchB.extents.x - 0.25f, y0 + top / 2f, pc.z), Quaternion.identity, new Vector3(0.5f, top, pitchB.size.z + 1f));
            Barrier("Barrier_Right", new Vector3(pc.x + pitchB.extents.x + 0.25f, y0 + top / 2f, pc.z), Quaternion.identity, new Vector3(0.5f, top, pitchB.size.z + 1f));
            Vector3 back = pc - outDir * (Mathf.Abs(Vector3.Dot(pitchB.extents, outDir)) + 0.25f);
            Barrier("Barrier_Back", new Vector3(back.x, y0 + top / 2f, back.z), goal.rotation, new Vector3(pitchB.size.x + 1f, top, 0.5f));
            float halfW = Mathf.Abs(Vector3.Dot(pitchB.extents, goal.right)) + 0.5f;
            float sideW = halfW - outer;
            Barrier("Barrier_GoalSide_Left", goal.TransformPoint(new Vector3(-(outer + sideW / 2f), top / 2f, 0f)), goal.rotation, new Vector3(sideW, top, 0.5f));
            Barrier("Barrier_GoalSide_Right", goal.TransformPoint(new Vector3(outer + sideW / 2f, top / 2f, 0f)), goal.rotation, new Vector3(sideW, top, 0.5f));
            Barrier("Barrier_AboveGoal", goal.TransformPoint(new Vector3(0f, H + 0.2f + (top - H - 0.2f) / 2f, 0f)), goal.rotation, new Vector3(outer * 2f, top - H - 0.2f, 0.5f));
            Barrier("Barrier_Ceiling", new Vector3(pc.x, y0 + top + 0.25f, pc.z), Quaternion.identity, new Vector3(pitchB.size.x + 1f, 0.5f, pitchB.size.z + 1f));
            log.AppendLine($"Barriers: height {top} m around the arena, goal mouth left open");

            // ---- UI (TextMeshPro) ----
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var scoreTxt = MakeText(canvasGo.transform, "ScoreText", "Goals: 0", 64, new Vector2(0f, 1f), new Vector2(40f, -30f), TextAlignmentOptions.TopLeft);
            var timerTxt = MakeText(canvasGo.transform, "TimerText", "2:00", 72, new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAlignmentOptions.Top);
            var ftTxt = MakeText(canvasGo.transform, "FullTimeText", "FULL TIME", 110, new Vector2(0.5f, 0.5f), Vector2.zero, TextAlignmentOptions.Center);
            ftTxt.rectTransform.sizeDelta = new Vector2(1400f, 600f);
            ftTxt.color = new Color(1f, 0.85f, 0.1f);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.carBody = carRb; gm.ballBody = brb;
            gm.scoreText = scoreTxt; gm.timerText = timerTxt; gm.fullTimeText = ftTxt;
            gm.matchDuration = 120f;

            // ---- Camera ----
            var cam = Camera.main;
            cam.transform.position = car.position - car.forward * 7f + Vector3.up * 3.5f;
            cam.transform.LookAt(car.position + Vector3.up);
            cam.farClipPlane = 500f;
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = car; follow.distance = 7f; follow.height = 3.5f;

            // ---- Light ----
            var light = Object.FindFirstObjectByType<Light>();
            if (light != null) { light.transform.rotation = Quaternion.Euler(50f, -30f, 0f); light.shadows = LightShadows.Soft; light.intensity = 1.2f; }

            // ---- Physics settings ----
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            Physics.bounceThreshold = 1f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            log.AppendLine("SCENE SAVED: " + ScenePath);
            Debug.Log("[CarFootball] Scene built.\n" + log);
        }
        catch (System.Exception e)
        {
            log.AppendLine("ERROR: " + e);
            Debug.LogError("[CarFootball] " + e);
        }
        File.WriteAllText(ReportPath, log.ToString());
    }

    static Transform Find(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        throw new System.Exception("Object not found in FBX: " + name);
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localEuler, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale = scale;
        if (type == PrimitiveType.Cylinder)
        {   // swap the capsule for an exact box collider
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.AddComponent<BoxCollider>();
        }
        if (m != null) go.GetComponent<Renderer>().sharedMaterial = m;
        go.isStatic = true;
        return go;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Vector2 anchor, Vector2 pos, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = align; t.color = Color.white;
        t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.2f; t.outlineColor = Color.black;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(600f, 120f);
        return t;
    }

    static void EnsureTag(string tag)
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tm.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++) if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tm.ApplyModifiedPropertiesWithoutUndo();
    }

    static Transform FindBody(Transform car)
    {
        foreach (var r in car.GetComponentsInChildren<Renderer>(true))
            if (r.name.ToLower().Contains("body")) return r.transform;
        throw new System.Exception("No car body found under 'car'");
    }

    static void SetMat(Transform t, Material m)
    {
        var r = t.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = m;
    }

    static Material MakeMat(string name, Color c)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.3f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static T LoadOrCreate<T>(string path, System.Func<T> create) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) { a = create(); AssetDatabase.CreateAsset(a, path); }
        return a;
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

    static void AddToBuildSettings(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == path);
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
