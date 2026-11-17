using System.IO;
using UnityEngine;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Screenshots when the game runs with no window (-batchmode): the main camera renders into an off-screen image.
    /// Used by the test bot and the frame recorder so tests never need to show a window. The on-screen HUD is not in these images.
    /// </summary>
    public static class WindowlessShot
    {
        public const int W = 1600, H = 900;
        static RenderTexture rt;

        public static Texture2D Grab()
        {
            var cam = Camera.main; var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            if (cam == null) return tex;
            if (rt == null) { rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 }; rt.Create(); }
            var keep = cam.targetTexture; var active = RenderTexture.active;
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            cam.targetTexture = keep; RenderTexture.active = active;
            return tex;
        }

        public static void Save(string path)
        {
            var tex = Grab(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.Destroy(tex);
        }
    }
}
