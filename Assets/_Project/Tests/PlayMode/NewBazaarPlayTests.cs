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
            yield return NewZonePlayBot.BazaarRun(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones/zone_1/editor-round1")));
            Assert.IsTrue(NewZonePlayBot.Finished); Assert.IsTrue(NewZonePlayBot.Passed, NewZonePlayBot.Failure);
        }
    }
}
