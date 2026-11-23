using System.Collections.Generic;
using UnityEngine;

namespace LegacyOfTemples.Core
{
    /// <summary>All voice lines of the game (built from Docs/VOICE_LINES.csv). Lives at Resources/VoiceDB/VoiceDatabase.asset.
    /// In its own file: Unity can only save a ScriptableObject whose class name matches its file name.</summary>
    [CreateAssetMenu(menuName = "Legacy of the Temple/Voice Database")]
    public sealed class VoiceDatabase : ScriptableObject
    {
        public List<VoiceLine> lines = new List<VoiceLine>();
        public string builtFrom, builtAt;
    }
}
