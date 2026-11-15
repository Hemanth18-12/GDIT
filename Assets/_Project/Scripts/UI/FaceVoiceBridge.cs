using UnityEngine;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.UI
{
    /// <summary>When a voice line starts, tell that character's face to follow the clip (lip sync, see FaceAnimator).</summary>
    public static class FaceVoiceBridge
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { VoicePlayer.OnVoice -= OnVoice; VoicePlayer.OnVoice += OnVoice; }

        static void OnVoice(string speaker, string tier)
        {
            if (VoicePlayer.Instance == null || string.IsNullOrEmpty(speaker)) return;
            var who = Find(speaker.ToLowerInvariant());
            if (who != null) FaceAnimator.For(speaker, who).Speak(VoicePlayer.Instance.Source);
        }

        static GameObject Find(string s)
        {
            if (s.Contains("ary") || s.Contains("investigator") || s.Contains("hero"))
            { var pc = Object.FindAnyObjectByType<PlayerController>(); return pc != null ? pc.gameObject : null; }
            string npc = s.Contains("puj") || s.Contains("priest") || s.Contains("ananda") ? "npc_priest" : s.Contains("mee") || s.Contains("villager") ? "npc_villager" : null;
            foreach (var n in Object.FindObjectsByType<NPCDialogueTrigger>()) if (n.Id == npc || n.Id.ToLowerInvariant().Contains(s)) return n.gameObject;
            return null;
        }
    }
}
