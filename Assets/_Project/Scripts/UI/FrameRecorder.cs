using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Records the game's own frames to JPEG files at a fixed 24 frames per second of game time (Time.captureFramerate),
    /// so a video made from them plays at true speed however long each frame took to save. Only active with the
    /// command line flag "-recordFrames &lt;dir&gt;" (optional "-recordSeconds N", default 180). Used for QA videos.
    /// </summary>
    public sealed class FrameRecorder : MonoBehaviour
    {
        private string dir; private int frames, max;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-recordFrames");
            if (i < 0 || i + 1 >= a.Length) return;
            var go = new GameObject("FrameRecorder"); DontDestroyOnLoad(go); var fr = go.AddComponent<FrameRecorder>();
            fr.dir = a[i + 1]; Directory.CreateDirectory(fr.dir);
            int s = Array.IndexOf(a, "-recordSeconds"); float secs = s >= 0 && s + 1 < a.Length && float.TryParse(a[s + 1], out var v) ? v : 180f;
            fr.max = Mathf.RoundToInt(secs * 24f);
        }

        private IEnumerator Start()
        {
            bool introOnly = System.Array.IndexOf(Environment.GetCommandLineArgs(), "-recordIntroOnly") >= 0;
            if (introOnly) { while (!LegacyOfTemples.Gameplay.IntroCinematic.Rolling) yield return null; }   // record only while the intro plays
            Time.captureFramerate = 24;
            while (frames < max && (!introOnly || LegacyOfTemples.Gameplay.IntroCinematic.Rolling))
            {
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                var bytes = tex.EncodeToJPG(82); Destroy(tex);
                string path = Path.Combine(dir, $"f{frames:00000}.jpg"); frames++;
                System.Threading.Tasks.Task.Run(() => File.WriteAllBytes(path, bytes));
            }
            Time.captureFramerate = 0;
            Debug.Log($"[FrameRecorder] {frames} frames written to {dir}");
        }
    }
}
