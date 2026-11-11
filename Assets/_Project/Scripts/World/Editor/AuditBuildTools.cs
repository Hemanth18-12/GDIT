using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>Plain build (no scene edits). Writes result + warnings to Docs/QA/last-build.txt.</summary>
    public static class AuditBuildTools
    {
        public static readonly string[] Scenes = {
            "Assets/_Project/Scenes/Dev/MainMenu.unity", "Assets/_Project/Scenes/Environments/Courtyard.unity",
            "Assets/_Project/Scenes/Environments/Sanctum.unity", "Assets/_Project/Scenes/Environments/PuzzleWall.unity",
            "Assets/_Project/Scenes/Environments/ShrineCrime.unity", "Assets/_Project/Scenes/Environments/JunglePath.unity" };

        [MenuItem("Tools/Legacy/QA/Build App (no scene changes)")]
        public static void Build()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            string repo = Directory.Exists(Path.Combine(root, "Docs")) ? root : Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string app = Path.Combine(repo, "Builds/LegacyOfTheTemple.app");
            // Build stamp shown in the pause menu, so the player can tell a fresh build from an old one.
            File.WriteAllText(Path.Combine(Application.dataPath, "_Project/Resources/build_stamp.txt"), "build " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            AssetDatabase.Refresh();
            LoadingImageTools.Sync();
            bool diag = UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled;
            BuildReport report;
            try
            {
                UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled = false;
                PlayerSettings.runInBackground = true;
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = Scenes, locationPathName = app, target = BuildTarget.StandaloneOSX, options = BuildOptions.Development
                });
            }
            finally { UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled = diag; }
            var sb = new StringBuilder($"{report.summary.result}: errors={report.summary.totalErrors}, warnings={report.summary.totalWarnings}\n");
            foreach (var step in report.steps)
                foreach (var m in step.messages)
                    if (m.type == LogType.Warning || m.type == LogType.Error) sb.AppendLine($"{step.name}: {m.type}: {m.content}");
            File.WriteAllText(Path.Combine(repo, "Docs/QA/last-build.txt"), sb.ToString());
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Build failed");
        }
    }
}
