using System;
using UnityEngine;

namespace LegacyOfTemples.UI
{
    // Compatibility component for existing scenes. The previous bot advanced the
    // tutorial directly and printed PASS without asserting what happened.
    public class GameQATestBot : MonoBehaviour
    {
        public static GameQATestBot Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-runQABot") >= 0)
                Debug.Log("The old unasserted QA bot is retired. Run GroundingPlayModeTests. For inspected app frames, launch with -captureGrounding followed by an absolute output folder. Capture completion does not mean gameplay passed.");
        }
    }
}
