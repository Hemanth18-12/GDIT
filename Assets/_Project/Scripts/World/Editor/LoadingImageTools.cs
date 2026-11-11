using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using LegacyOfTemples.UI;

namespace LegacyOfTemples.World.Editor
{
    /// <summary>Fills the loading image slots from Assets/_Project/UI/Loading (run automatically before a build).</summary>
    public static class LoadingImageTools
    {
        private const string Folder = "Assets/_Project/UI/Loading", AssetPath = "Assets/_Project/Resources/UI/LoadingImages.asset";
        private static readonly (string map, string image)[] Map = {
            ("Courtyard (start and final return)", "loading_01_dawn_courtyard"), ("ShrineCrime and PuzzleWall", "loading_02_gate_night"),
            ("JunglePath", "loading_03_jungle_path"), ("Sanctum", "loading_04_shrine") };

        [MenuItem("Legacy of the Temple/Sync loading images")]
        public static void Sync()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/UI")) AssetDatabase.CreateFolder("Assets/_Project/Resources", "UI");
            var set = AssetDatabase.LoadAssetAtPath<LoadingImageSet>(AssetPath);
            if (set == null) { set = ScriptableObject.CreateInstance<LoadingImageSet>(); AssetDatabase.CreateAsset(set, AssetPath); }
            var slots = new List<LoadingImageSet.Slot>(set.slots);
            foreach (var m in Map)
            {
                var slot = slots.Find(s => s.imageName == m.image);
                if (slot == null) { slot = new LoadingImageSet.Slot { map = m.map, imageName = m.image }; slots.Add(slot); }
                if (slot.image != null) continue; // keep a picture the user assigned by hand
                foreach (string ext in new[] { ".png", ".jpg", ".jpeg" })
                {
                    string p = Folder + "/" + m.image + ext;
                    if (!File.Exists(p)) continue;
                    var imp = (TextureImporter)AssetImporter.GetAtPath(p);
                    if (imp != null && (imp.mipmapEnabled || imp.maxTextureSize != 2048)) { imp.mipmapEnabled = false; imp.maxTextureSize = 2048; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport(); }
                    slot.image = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                }
                if (slot.image == null) Debug.LogWarning("[LoadingImages] Missing " + Folder + "/" + m.image + ".png (a dark gradient will be shown)");
            }
            set.slots = slots.ToArray();
            EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
            Debug.Log("[Batch] loading images synced: " + slots.FindAll(s => s.image != null).Count + "/" + slots.Count);
        }
    }
}
