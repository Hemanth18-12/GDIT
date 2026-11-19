using UnityEditor;
using UnityEngine;
namespace LegacyOfTemples.World.Editor
{
    public static class ClipSpeedProbe
    {
        public static void Run()
        {
            foreach (var n in new[] { "Idle", "Walking", "Running" })
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Animations/Clips/Hero/" + n + ".fbx"))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview"))
                        Debug.Log($"[ClipSpeed] {n}/{c.name}: averageSpeed={c.averageSpeed} ({new Vector2(c.averageSpeed.x, c.averageSpeed.z).magnitude:0.00} m/s) length={c.length:0.00}s loop={c.isLooping}");
        }
    }
}
