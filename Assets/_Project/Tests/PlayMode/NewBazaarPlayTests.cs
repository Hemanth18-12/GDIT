using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.UI;

namespace LegacyOfTemples.Tests
{
    public class NewBazaarPlayTests
    {
        [UnityTest, Timeout(540000)] public IEnumerator Bazaar_ContinuousWalk_PhysicalLock_RealFight_Recovery()
        {
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene("TempleRing"); yield return null; yield return new WaitForSeconds(3f);
            if (NewWorldDirector.Instance == null) new GameObject("NewWorld_Director").AddComponent<NewWorldDirector>();
            NewWorldDirector.Instance.EnsureZones();
            yield return NewZonePlayBot.BazaarRun(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones/zone_1/editor-round1")));
            Assert.IsTrue(NewZonePlayBot.Finished); Assert.IsTrue(NewZonePlayBot.Passed, NewZonePlayBot.Failure);
        }

        [UnityTest, Timeout(540000)] public IEnumerator Zone2_Courtyard_Reconstruct_Shadow_Lookout_Chronicle()
        {
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene("TempleRing"); yield return null; yield return new WaitForSeconds(3f);
            if (NewWorldDirector.Instance == null) new GameObject("NewWorld_Director").AddComponent<NewWorldDirector>();
            NewWorldDirector.Instance.EnsureZones();
            yield return NewZonePlayBot.CourtyardRun(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones/zone_2/editor-round1")));
            Assert.IsTrue(NewZonePlayBot.Finished); Assert.IsTrue(NewZonePlayBot.Passed, NewZonePlayBot.Failure);
        }

        [UnityTest, Timeout(540000)] public IEnumerator Zone3_Vaults_Plates_Gears_Discs_Rosette()
        {
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene("TempleRing"); yield return null; yield return new WaitForSeconds(3f);
            if (NewWorldDirector.Instance == null) new GameObject("NewWorld_Director").AddComponent<NewWorldDirector>();
            NewWorldDirector.Instance.EnsureZones();
            yield return NewZonePlayBot.VaultsRun(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones/zone_3/editor-round1")));
            Assert.IsTrue(NewZonePlayBot.Finished); Assert.IsTrue(NewZonePlayBot.Passed, NewZonePlayBot.Failure);
        }

        [UnityTest, Timeout(540000)] public IEnumerator Zone4_Stepwell_Sluices_RisingRaft_Vessel()
        {
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene("TempleRing"); yield return null; yield return new WaitForSeconds(3f);
            if (NewWorldDirector.Instance == null) new GameObject("NewWorld_Director").AddComponent<NewWorldDirector>();
            NewWorldDirector.Instance.EnsureZones();
            yield return NewZonePlayBot.StepwellRun(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones/zone_4/editor-round1")));
            Assert.IsTrue(NewZonePlayBot.Finished); Assert.IsTrue(NewZonePlayBot.Passed, NewZonePlayBot.Failure);
        }

        [UnityTest, Timeout(540000)] public IEnumerator Zone5_SunWall_Mirrors_ChariotWheel_SmugglerMap()
        {
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene("TempleRing"); yield return null; yield return new WaitForSeconds(3f);
            if (NewWorldDirector.Instance == null) new GameObject("NewWorld_Director").AddComponent<NewWorldDirector>();
            NewWorldDirector.Instance.EnsureZones();
            yield return NewZonePlayBot.SunWallRun(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones/zone_5/editor-round1")));
            Assert.IsTrue(NewZonePlayBot.Finished); Assert.IsTrue(NewZonePlayBot.Passed, NewZonePlayBot.Failure);
        }

        [UnityTest, Timeout(540000)] public IEnumerator Zone6_Jungle_Trail_InsideMan_Bhaskar_Finial()
        {
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene("TempleRing"); yield return null; yield return new WaitForSeconds(3f);
            if (NewWorldDirector.Instance == null) new GameObject("NewWorld_Director").AddComponent<NewWorldDirector>();
            NewWorldDirector.Instance.EnsureZones();
            yield return NewZonePlayBot.JungleRun(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones/zone_6/editor-round1")));
            Assert.IsTrue(NewZonePlayBot.Finished); Assert.IsTrue(NewZonePlayBot.Passed, NewZonePlayBot.Failure);
        }
    }
}
