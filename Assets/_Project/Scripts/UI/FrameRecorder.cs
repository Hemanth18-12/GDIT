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
            var go = new GameObject("FrameRecorder"); DontDestroyOnLoad(go); var fr = go.AddComponent<FrameRecorder>(); Recording = true;
            fr.dir = a[i + 1]; Directory.CreateDirectory(fr.dir);
            int s = Array.IndexOf(a, "-recordSeconds"); float secs = s >= 0 && s + 1 < a.Length && float.TryParse(a[s + 1], out var v) ? v : 180f;
            fr.max = Mathf.RoundToInt(secs * 24f);
        }

        /// <summary>With "-recordWaitForBot" the recording starts only when the test bot sets this (skips the menu and loading).</summary>
        public static bool BotReady;
        /// <summary>The bot asks the recording to end early (before -recordSeconds); Finished is set once the frames and the audio file are written.</summary>
        public static bool StopRequested, Finished;
        /// <summary>True when this run records (the -recordFrames flag).</summary>
        public static bool Recording { get; private set; }

        private IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-recordWaitForBot") >= 0) while (!BotReady) yield return null;
            bool introOnly = System.Array.IndexOf(Environment.GetCommandLineArgs(), "-recordIntroOnly") >= 0;
            if (introOnly) { while (!LegacyOfTemples.Gameplay.IntroCinematic.Rolling) yield return null; }   // record only while the intro plays
            bool realTime = Application.isBatchMode;   // windowless: sample at real time so the game (and the test bot) run at normal speed
            if (!realTime) Time.captureFramerate = 24;
            // "-recordAudio": also record the game's mixed sound in step with the frames (AudioRenderer, offline at 24 fps)
            bool audio = !realTime && Array.IndexOf(Environment.GetCommandLineArgs(), "-recordAudio") >= 0;
            int ch = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2; var pcm = new System.Collections.Generic.List<float>();
            if (audio) audio = AudioRenderer.Start();
            float nextAt = Time.realtimeSinceStartup;
            while (frames < max && !StopRequested && (!introOnly || LegacyOfTemples.Gameplay.IntroCinematic.Rolling))
            {
                yield return new WaitForEndOfFrame();
                if (realTime) { if (Time.realtimeSinceStartup < nextAt) continue; nextAt += 1f / 24f; }
                var tex = Application.isBatchMode ? WindowlessShot.Grab() : ScreenCapture.CaptureScreenshotAsTexture();   // no window: render the camera instead
                var bytes = tex.EncodeToJPG(82); Destroy(tex);
                string path = Path.Combine(dir, $"f{frames:00000}.jpg"); frames++;
                System.Threading.Tasks.Task.Run(() => File.WriteAllBytes(path, bytes));
                if (audio)
                {
                    int n = AudioRenderer.GetSampleCountForCaptureFrame();
                    var buf = new Unity.Collections.NativeArray<float>(n * ch, Unity.Collections.Allocator.Temp);
                    AudioRenderer.Render(buf); pcm.AddRange(buf); buf.Dispose();
                }
            }
            if (audio) { AudioRenderer.Stop(); WriteWav(Path.Combine(dir, "audio.wav"), pcm, ch, AudioSettings.outputSampleRate); Debug.Log($"[FrameRecorder] audio: {pcm.Count / ch / (float)AudioSettings.outputSampleRate:0.0} s, {ch} channels, {AudioSettings.outputSampleRate} Hz"); }
            Time.captureFramerate = 0;
            Debug.Log($"[FrameRecorder] {frames} frames written to {dir}");
            Finished = true;
        }

        /// <summary>16-bit PCM WAV.</summary>
        private static void WriteWav(string path, System.Collections.Generic.List<float> data, int ch, int rate)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = data.Count * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16); w.Write((short)1); w.Write((short)ch); w.Write(rate); w.Write(rate * ch * 2); w.Write((short)(ch * 2)); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (var f in data) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(f * 32767f), -32768, 32767));
            }
        }
    }
}
