using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build.Reporting;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.World.Editor
{
    public static class GroundingTools
    {
        public static string EvidenceRoot => Path.GetFullPath(Path.Combine(Application.dataPath,
            new DirectoryInfo(Application.dataPath).Parent.Name == "My project" ? "../../Docs/QA/2026-11-06" : "../Docs/QA/2026-11-06"));

        [MenuItem("Tools/Legacy/QA/Calibrate Player and Stair Colliders")]
        public static void ApplyCalibration()
        {
            var templeImporter = (ModelImporter)AssetImporter.GetAtPath("Assets/_Project/Models/Environment/Temple_Jagannath.fbx");
            if (!templeImporter.HasPreBakeCollisionMesh(false))
            {
                templeImporter.SetPreBakeCollisionMesh(false, true);
                templeImporter.SaveAndReimport();
            }
            const string path = "Assets/_Project/Prefabs/Characters/Investigator.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var grounding = root.GetComponent<CharacterGrounding>();
            if (grounding == null) grounding = root.AddComponent<CharacterGrounding>();
            grounding.Calibrate();
            root.GetComponentInChildren<Animator>().enabled = false;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Environments/Courtyard.unity");
            var ramp = GameObject.Find("StairRamp_Smooth");
            if (ramp != null) ramp.GetComponent<Collider>().enabled = false;
            var temple = GameObject.Find("Temple_Jagannath");
            if (temple != null)
            {
                // Source gate z=19, Vimana z=11: align the gate with the steps and
                // place the hero outside the building instead of inside its base.
                temple.transform.position = new Vector3(0, 0, -20);
                foreach (var renderer in temple.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!renderer.name.Contains("FLOOR")) continue;
                    var collider = renderer.GetComponent<MeshCollider>();
                    if (collider == null) collider = renderer.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                }
            }
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void BuildForEvidence()
        {
            ApplyCalibration();
            string build = Path.GetFullPath(Path.Combine(EvidenceRoot, "../../../Builds/LegacyOfTheTemple.app"));
            // This local QA build needs no cloud diagnostics. Avoid an automatic
            // native-symbol upload without credentials; restore the editor setting.
            bool diagnosticsEnabled = UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled;
            bool backgroundEnabled = PlayerSettings.runInBackground;
            string connectPath = Path.Combine(Application.dataPath, "../ProjectSettings/UnityConnectSettings.asset");
            string connectSettings = File.ReadAllText(connectPath);
            BuildReport report;
            try
            {
                UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled = false;
                PlayerSettings.runInBackground = true;
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/Dev/MainMenu.unity", "Assets/_Project/Scenes/Environments/Courtyard.unity",
                    "Assets/_Project/Scenes/Environments/Sanctum.unity", "Assets/_Project/Scenes/Environments/PuzzleWall.unity",
                    "Assets/_Project/Scenes/Environments/ShrineCrime.unity", "Assets/_Project/Scenes/Environments/JunglePath.unity" },
                locationPathName = build, target = BuildTarget.StandaloneOSX, options = BuildOptions.Development
                });
            }
            finally
            {
                UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled = diagnosticsEnabled;
                PlayerSettings.runInBackground = backgroundEnabled;
                File.WriteAllText(connectPath, connectSettings);
            }
            File.WriteAllText(Path.Combine(EvidenceRoot, "build-result.txt"),
                $"{report.summary.result}: errors={report.summary.totalErrors}, warnings={report.summary.totalWarnings}, bytes={report.summary.totalSize}\n");
            var messages = new StringBuilder();
            foreach (var step in report.steps)
                foreach (var message in step.messages)
                    if (message.type == LogType.Warning || message.type == LogType.Error)
                        messages.AppendLine($"{step.name}: {message.type}: {message.content}");
            File.WriteAllText(Path.Combine(EvidenceRoot, "build-warnings.txt"), messages.ToString());
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0 || report.summary.totalWarnings != 0)
                throw new System.Exception("Build did not satisfy zero-error/zero-warning requirement; inspect the build log.");
        }

        public static void PrepareLookAndBuild()
        {
            ApplyCalibration();
            CinematicLookTools.Apply();
            BuildForEvidence();
        }

        [MenuItem("Tools/Legacy/QA/Measure Player and Floors")]
        public static void Diagnose()
        {
            Directory.CreateDirectory(EvidenceRoot);
            var report = new StringBuilder();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Investigator.prefab");
            var player = Object.Instantiate(prefab);
            player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var cc = player.GetComponent<CharacterController>();
            report.AppendLine($"Controller: center={cc.center:F5}, height={cc.height:F5}, radius={cc.radius:F5}, skin={cc.skinWidth:F5}, step={cc.stepOffset:F5}");
            foreach (var a in player.GetComponentsInChildren<Animator>(true))
                report.AppendLine($"Animator {a.name}: rootMotion={a.applyRootMotion}, human={a.isHuman}, scale={a.transform.lossyScale:F5}");
            foreach (var r in player.GetComponentsInChildren<Renderer>(true))
            {
                var bounds = r.bounds;
                if (r is SkinnedMeshRenderer skinned)
                {
                    var mesh = new Mesh();
                    skinned.BakeMesh(mesh, true);
                    float low = float.PositiveInfinity, high = float.NegativeInfinity;
                    foreach (var v in mesh.vertices)
                    {
                        float y = skinned.transform.TransformPoint(v).y;
                        low = Mathf.Min(low, y);
                        high = Mathf.Max(high, y);
                    }
                    report.AppendLine($"Mesh {r.name}: actual vertex Y=[{low:F5},{high:F5}], renderer bounds=[{bounds.min.y:F5},{bounds.max.y:F5}], scale={r.transform.lossyScale:F5}");
                    Object.DestroyImmediate(mesh);
                }
                else report.AppendLine($"Mesh {r.name}: bounds={bounds}");
            }
            Object.DestroyImmediate(player);
            foreach (string name in new[] { "Courtyard", "Sanctum", "PuzzleWall", "ShrineCrime", "JunglePath" })
            {
                var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/Environments/{name}.unity");
                report.AppendLine($"\n{name}");
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var c in root.GetComponentsInChildren<Collider>())
                        if (c.name.Contains("Floor") || c.name.Contains("Ground") || c.name.Contains("Ramp"))
                            report.AppendLine($"Floor {c.name}: type={c.GetType().Name}, top={c.bounds.max.y:F5}, enabled={c.enabled}, trigger={c.isTrigger}, scale={c.transform.lossyScale}");
                foreach (var p in Object.FindObjectsByType<PlayerController>())
                    report.AppendLine($"Player spawn: {p.transform.position:F5}");
                if (name == "Courtyard")
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var r in root.GetComponentsInChildren<Renderer>())
                            if (r.name.Contains("BASE") || r.name.Contains("FLOOR") || r.name.Contains("PLINTH") || r.name.Contains("STEP"))
                                report.AppendLine($"Architecture {r.name}: bounds={r.bounds}, collider={r.GetComponent<Collider>() != null}");
            }
            File.WriteAllText(Path.Combine(EvidenceRoot, "grounding-diagnosis.txt"), report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
