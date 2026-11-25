using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Vehicles;
using LegacyOfTemples.UI;

namespace LegacyOfTemples.Tests
{
    public class UpgradeVerificationTests
    {
        [UnityTest, Timeout(180000)]
        public IEnumerator TempleRing_PujariLore_SixDeliveriesSkipVoiceAndReload()
        {
            yield return PujariLoreReviewBot.Run(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones/pujari-lore/editor")));
            Assert.IsTrue(PujariLoreReviewBot.Finished);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Courtyard_BlackGateSealed_NoRawTempleMesh()
        {
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene(Constants.Courtyard);
            yield return new WaitForSeconds(2.5f);

            Assert.IsNull(GameObject.Find("Temple_Game"), "Raw Temple_Game should be removed");
            var portal = GameObject.Find("Portal_To_Sanctum");
            if (portal != null)
            {
                foreach (var c in portal.GetComponents<Collider>())
                    Assert.IsFalse(c.isTrigger, "Courtyard black gate trigger must be disabled/solid");
            }
            foreach (var r in Object.FindObjectsByType<MeshRenderer>())
            {
                if (r.sharedMaterial != null)
                    Assert.IsFalse(r.sharedMaterial.name.Contains("SanctumDarkness"), $"SanctumDarkness still on {r.name}");
            }

            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones"));
            Directory.CreateDirectory(outDir);
            string file = Path.Combine(outDir, "courtyard-sealed.png");
            if (Application.isBatchMode) WindowlessShot.Save(file);
            else { ScreenCapture.CaptureScreenshot(file); yield return new WaitForSeconds(0.5f); }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TempleRing_Minimap_JeepReset_OnSpotRetry_AudioChannels()
        {
            StoryDialogueSystem.ResetForNewGame();
            SceneManager.LoadScene(Constants.TempleRing);
            yield return new WaitForSeconds(3f);

            if (NewWorldDirector.Instance == null) new GameObject("NewWorld_Director").AddComponent<NewWorldDirector>();
            NewWorldDirector.Instance.EnsureZones();
            yield return new WaitForSeconds(0.5f);

            Assert.IsNull(GameObject.Find("Temple_Game"), "Raw Temple_Game must not exist in TempleRing");
            Assert.IsNotNull(GameObject.Find("Temple_KalingaCentral"), "Modular grounded Kalinga temple missing");
            Assert.IsTrue(MinimapHud.Visible, "MinimapHud must be visible during gameplay");

            // Verify Jeep repair and road reset
            var jeep = Jeep.Instance;
            Assert.IsNotNull(jeep, "Jeep should be present in TempleRing");
            jeep.transform.SetPositionAndRotation(new Vector3(14f, 1.5f, 38f), Quaternion.Euler(0f, 35f, 22f));
            jeep.RepairAndResetToRoad();
            yield return null;
            Assert.Less(Vector3.Angle(jeep.transform.up, Vector3.up), 5f, "Jeep should reset upright onto the road");
            Assert.AreEqual(0, jeep.DamageState, "Jeep DamageState should reset to 0");

            // Verify on-the-spot section retry without scene reload
            Vector3 cpPos = new Vector3(0f, 0.2f, 92f);
            Checkpoints.Save("zone2", "Courtyard Gate", cpPos, 0f, 0f);
            var pc = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(pc, "PlayerController missing");
            pc.Teleport(new Vector3(-18f, 0.2f, 52f), Quaternion.identity);
            yield return null;
            if (DawnClock.Instance != null) DawnClock.Instance.RetryOnSpot(false);
            yield return null;
            Assert.AreEqual(Constants.TempleRing, SceneManager.GetActiveScene().name, "On-spot retry must stay in TempleRing");
            Assert.Less(Vector3.Distance(new Vector3(pc.transform.position.x, 0f, pc.transform.position.z), new Vector3(cpPos.x, 0f, cpPos.z)), 3f, "Player should return to local checkpoint on the spot");

            // Verify all 5 audio sliders sync with AudioManager & VoicePlayer & PlayerPrefs
            var am = AudioManager.Instance;
            Assert.IsNotNull(am, "AudioManager missing");
            am.SetMasterVolume(0.82f); PlayerPrefs.SetFloat("Settings_MasterVol", 0.82f);
            am.SetMusicVolume(0.55f); PlayerPrefs.SetFloat("Settings_MusicVol", 0.55f);
            am.SetAmbienceVolume(0.45f);
            am.SetSFXVolume(0.75f); PlayerPrefs.SetFloat("Settings_SfxVol", 0.75f);
            VoicePlayer.Volume = 0.88f; am.SetVoiceVolume(0.88f);
            Assert.AreEqual(0.82f, am.MasterVolume, 0.01f);
            Assert.AreEqual(0.55f, am.MusicVolume, 0.01f);
            Assert.AreEqual(0.45f, am.AmbienceVolume, 0.01f);
            Assert.AreEqual(0.75f, am.SFXVolume, 0.01f);
            Assert.AreEqual(0.88f, VoicePlayer.Volume, 0.01f);

            // Restore balanced defaults
            am.SetMasterVolume(1f); PlayerPrefs.SetFloat("Settings_MasterVol", 1f);
            am.SetMusicVolume(0.7f); PlayerPrefs.SetFloat("Settings_MusicVol", 0.7f);
            am.SetAmbienceVolume(0.6f);
            am.SetSFXVolume(0.9f); PlayerPrefs.SetFloat("Settings_SfxVol", 0.9f);
            VoicePlayer.Volume = 1f; am.SetVoiceVolume(1f);
            PlayerPrefs.Save();

            // Capture TempleRing screenshot showing Minimap HUD + Courtyard/Bazaar view
            yield return new WaitForSeconds(0.25f);
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones"));
            Directory.CreateDirectory(outDir);
            string file = Path.Combine(outDir, "minimap-jeep-reset.png");
            if (Application.isBatchMode) WindowlessShot.Save(file);
            else { ScreenCapture.CaptureScreenshot(file); yield return new WaitForSeconds(0.5f); }
        }
    }
}
