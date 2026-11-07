using System;
using UnityEditor;
using UnityEngine;

namespace LegacyOfTemples.World.Editor
{
    // The former 25-item file/type-existence checklist made unsupported gameplay
    // claims. Real verification now lives in PlayMode tests and inspected app frames.
    public static class ComprehensiveGameVerifier
    {
        [MenuItem("Tools/Legacy/QA/Gameplay Verification Instructions")]
        public static void RunChecklist()
        {
            Debug.Log("Run GroundingPlayModeTests in the Test Runner, build and play the macOS app, and inspect screenshots against Docs/QA/QA_REPORT.md. Grounding tests do not certify animation, UI, story, Showcase or a full performance pass.");
        }

        public static void RunChecklistBatch()
        {
            throw new InvalidOperationException("The legacy existence checklist is retired. Run real PlayMode tests and inspect app evidence. No automatic completion score is available.");
        }

        [MenuItem("Tools/Legacy/Build Standalone OSX")]
        public static void BuildStandaloneOSX() => GroundingTools.BuildForEvidence();
    }
}
