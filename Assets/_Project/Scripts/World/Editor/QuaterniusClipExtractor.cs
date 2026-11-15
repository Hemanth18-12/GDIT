using System.IO;
using UnityEditor;
using UnityEngine;

namespace LegacyOfTemples.World.Editor
{
    public static class QuaterniusClipExtractor
    {
        private const string FbxPath = "Assets/_Project/Animations/Clips/Quaternius/Quaternius_Animations.fbx";
        private const string OutDir = "Assets/_Project/Animations/Clips/Hero";

        [MenuItem("Tools/Legacy/Extract Quaternius Clips")]
        public static void Extract()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[QuaterniusClipExtractor] FBX not found: " + FbxPath);
                return;
            }

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }

            Directory.CreateDirectory(OutDir);
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
            int count = 0;

            foreach (var asset in allAssets)
            {
                if (asset is AnimationClip srcClip && !srcClip.name.StartsWith("__preview__"))
                {
                    string clipName = srcClip.name;
                    // Remove rig prefix if present
                    if (clipName.Contains("|"))
                    {
                        clipName = clipName.Substring(clipName.LastIndexOf('|') + 1);
                    }

                    string outPath = $"{OutDir}/{clipName}.anim";
                    AnimationClip newClip = Object.Instantiate(srcClip);
                    newClip.name = clipName;

                    // Configure loop settings
                    var settings = AnimationUtility.GetAnimationClipSettings(newClip);
                    if (clipName.EndsWith("_Loop") || clipName.Contains("Loop") || clipName == "Driving_Loop" || clipName == "Idle_Talking_Loop")
                    {
                        settings.loopTime = true;
                    }
                    else
                    {
                        settings.loopTime = false;
                    }
                    AnimationUtility.SetAnimationClipSettings(newClip, settings);

                    AssetDatabase.CreateAsset(newClip, outPath);
                    count++;
                    Debug.Log($"[QuaterniusClipExtractor] Extracted {clipName} -> {outPath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[QuaterniusClipExtractor] Successfully extracted {count} clips to {OutDir}");
        }
    }
}
