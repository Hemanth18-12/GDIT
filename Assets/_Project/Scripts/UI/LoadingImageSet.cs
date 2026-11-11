using System;
using UnityEngine;

namespace LegacyOfTemples.UI
{
    /// <summary>One image slot per destination. Drag your own picture into "image" in the Inspector
    /// (asset: Assets/_Project/Resources/UI/LoadingImages.asset). Empty slots show a dark gradient and log a warning.</summary>
    public sealed class LoadingImageSet : ScriptableObject
    {
        [Serializable] public class Slot { public string map; public string imageName; public Texture2D image; }
        public Slot[] slots = new Slot[0];
        public Texture2D Find(string imageName) { foreach (var s in slots) if (s.imageName == imageName && s.image != null) return s.image; return null; }
    }
}
