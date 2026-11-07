using UnityEditor;

namespace LegacyOfTemples.Characters.Editor
{
    // The previous sine-wave generator violated the real-animation requirement
    // and produced a submerged humanoid pose. Do not regenerate those clips.
    public static class HumanoidClipGenerator
    {
        [MenuItem("Tools/Legacy/Animation/Real FBX Clips Required")]
        public static void GenerateClips()
        {
            EditorUtility.DisplayDialog("Real animation required",
                "Generated bobbing clips are disabled. Confirm the character models, then supply the real FBX actions listed in Docs/ANIMATION_REQUIRED.md. Destination: Assets/_Project/Animations/Clips/Real/", "OK");
        }
    }
}
