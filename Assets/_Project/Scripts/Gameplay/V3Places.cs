using UnityEngine;
using LegacyOfTemples.Characters;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// The places of the V3 world (all in the Courtyard scene) with a standing point and a look target, read from the
    /// zone builders at run time. Used by map fast travel and the F9 demo menu. Same points as the performance test.
    /// </summary>
    public static class V3Places
    {
        public struct Place { public string name, objective; public bool ok; public Vector3 at, look; }
        public static readonly string[] LegacyNames = { "Temple gate", "Bazaar and tea lane", "Storeroom", "Vaults", "Stepwell", "Sun Wall", "River road (jeep chase)", "Ruin court (final fight)" };
        static readonly string[] LegacyObjectives = { "obj_v3_pujari", "obj_v3_bazaar", "obj_v3_storeroom", "obj_v3_vaults", "obj_v3_stepwell", "obj_v3_sunwall", "obj_v3_jungle", "obj_v3_jungle" };
        static readonly string[] NewNames = { "Village bazaar and tea lane", "Temple gate and courtyard (pending)", "Underground vaults (pending)", "Stepwell (pending)", "Sun Wall (pending)", "Jungle road and ruins (pending)" };
        public static bool NewWorld => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == Core.Constants.TempleRing;
        public static string[] Names => NewWorld ? NewNames : LegacyNames;
        public static string[] Objectives => NewWorld ? new[] { "nz1", "nz2", "nz3", "nz4", "nz5", "nz6" } : LegacyObjectives;
        public static int Count => Names.Length;

        public static Place Get(int i)
        {
            var p = new Place { name = Names[i], objective = Objectives[i] };
            if (NewWorld)
            {
                if (i == 0 && NewBazaar.Instance != null) { p.ok = true; p.at = new Vector3(0f, 0.05f, -8f); p.look = new Vector3(0f, 2f, 20f); }
                return p;
            }
            switch (i)
            {
                case 0: { var ts = TempleSite.Instance; if (World.Village.GatePos != Vector3.zero) { p.ok = true; p.at = World.Village.GatePos + Vector3.back * 8f; p.look = ts != null ? ts.TowerTop : p.at + Vector3.back * 10f; } break; }
                case 1: { var vl = VillageLife.Instance; if (vl != null) { p.ok = true; p.at = vl.TeaStallStand + Vector3.forward * 4f; p.look = vl.TeaStallStand + Vector3.forward * 40f; } break; }
                case 2: { var z = BreakInPuzzle.Instance; if (z != null) { p.ok = true; p.at = z.transform.TransformPoint(new Vector3(0f, 0f, 9f)); p.look = z.transform.position; } break; }
                case 3: { var z = VaultPuzzle.Instance; if (z != null) { p.ok = true; p.at = z.transform.TransformPoint(new Vector3(-9f, 0f, 0f)); p.look = z.transform.position; } break; }
                case 4: { var z = StepwellPuzzle.Instance; if (z != null) { p.ok = true; p.at = z.transform.TransformPoint(new Vector3(-4f, StepwellPuzzle.Rim, -8.5f)); p.look = z.transform.position; } break; }
                case 5: { var z = SunWallPuzzle.Instance; if (z != null) { p.ok = true; p.at = z.transform.TransformPoint(new Vector3(0f, 0f, -10f)); p.look = z.transform.TransformPoint(new Vector3(0f, 0f, 14f)); } break; }
                case 6: { var z = JungleChase.Instance; if (z != null && z.Road.Count > 4) { p.ok = true; p.at = z.RoadStart; p.look = z.Road[4]; } break; }
                case 7: { var z = JungleChase.Instance; if (z != null && z.Court != null) { p.ok = true; p.at = z.Court.TransformPoint(new Vector3(0f, 0f, 10f)); p.look = z.Court.position; } break; }
            }
            return p;
        }

        /// <summary>The story has reached this place's step (or the game is finished).</summary>
        public static bool Unlocked(int i)
        {
            if (NewWorld) return i == 0 && Puzzles.PuzzleSave.Get("nz1.visited") == 1;
            var story = StoryDialogueSystem.Instance; if (story == null) return false;
            if (story.IsComplete) return true;
            for (int k = 0; k < StoryData.Objectives.Length; k++) if (StoryData.Objectives[k].id == Objectives[i]) return k <= story.ObjectiveIndex;
            return false;
        }

        /// <summary>Move the player there (out of the jeep first). Returns a reason if it could not.</summary>
        public static string TeleportTo(int i)
        {
            var p = Get(i); if (!p.ok) return "That place is not built.";
            var pc = Object.FindAnyObjectByType<PlayerController>(); if (pc == null) return "No player.";
            var jeep = Vehicles.Jeep.Instance; if (jeep != null && jeep.Occupied) return "Get out of the jeep first.";
            Vector3 f = p.look - p.at; f.y = 0f; if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            pc.Teleport(p.at + Vector3.up * 0.1f, Quaternion.LookRotation(f));
            var orbit = Object.FindAnyObjectByType<OrbitCamera>(); if (orbit != null) orbit.SetTarget(pc.transform);
            Debug.Log($"[Travel] {p.name} at {p.at:0.0}");
            return null;
        }
    }
}
