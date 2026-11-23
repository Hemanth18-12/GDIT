using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LegacyOfTemples.Core;
using LegacyOfTemples.UI;

/// <summary>The voice-over system: data, every trigger, priority and no overlap, cooldowns, once-only, frequency, story lines.</summary>
public class VoiceSystemTests
{
    VoiceManager VM => VoiceManager.Instance;

    [UnitySetUp] public IEnumerator Setup() { yield return null; VoiceManager.TestMode = true; VoiceManager.ResetForTests(); GameOptions.VoiceFrequency = 1; }
    [UnityTearDown] public IEnumerator TearDown() { VoiceManager.TestMode = false; VoiceManager.ResetForTests(); GameOptions.VoiceFrequency = 1; yield return null; }

    [UnityTest]
    public IEnumerator Database_Has_Every_Line_With_A_Mono_Clip()
    {
        var db = VoiceLibrary.Db; Assert.NotNull(db, "Resources/VoiceDB/VoiceDatabase.asset");
        Assert.GreaterOrEqual(db.lines.Count, 130);
        var ids = new HashSet<string>();
        foreach (var l in db.lines)
        {
            Assert.IsTrue(ids.Add(l.id), "duplicate " + l.id);
            Assert.NotNull(l.clip, l.id + " has no clip");
            Assert.Greater(l.clip.length, 0.3f, l.id); Assert.Less(l.clip.length, 9f, l.id);
            Assert.AreEqual(1, l.clip.channels, l.id + " not mono");
            Assert.That(l.priority, Is.InRange(1, 5), l.id);
        }
        Assert.NotNull(VM, "VoiceManager boots by itself");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Every_Trigger_Can_Speak()
    {
        var triggers = new List<string>(); foreach (var l in VoiceLibrary.Db.lines) if (l.trigger != "story" && !triggers.Contains(l.trigger)) triggers.Add(l.trigger);
        var silent = new List<string>();
        foreach (var t in triggers)
        {
            VoiceManager.ResetForTests();
            string id = VoiceManager.Fire(t, null, "enemy=robber;evidence=list;speaker=villager_f");
            if (id == null) { id = VoiceManager.Fire(t, null, "enemy=kishore;evidence=ledger;speaker=pujari"); }
            if (id == null) silent.Add(t);
            yield return null;
        }
        Assert.IsEmpty(silent, "triggers that never speak: " + string.Join(", ", silent));
        Debug.Log($"[VoiceTest] {triggers.Count} triggers all speak");
    }

    [UnityTest]
    public IEnumerator Higher_Priority_Interrupts_And_Voices_Never_Overlap()
    {
        Assert.NotNull(VoiceManager.Fire("jump"));                      // priority 1
        yield return null;
        Assert.AreEqual(1, VM.Current.priority);
        Assert.NotNull(VoiceManager.Fire("puzzle_solved"));             // priority 3 interrupts after a short fade
        float t = 0f; while (t < 0.5f && VM.Current.priority != 3) { Assert.IsFalse(VM.Source2D.isPlaying && VM.Source3D.isPlaying, "two voices at once"); t += Time.unscaledDeltaTime; yield return null; }
        Assert.AreEqual(3, VM.Current.priority, "the priority 3 line plays");
        VoiceManager.ResetCooldownsForTests();
        Assert.IsNull(VoiceManager.Fire("jump"), "a priority 1 line is dropped while a priority 3 line plays");
        VoiceManager.ResetCooldownsForTests();
        Assert.NotNull(VoiceManager.Fire("fight_start"), "a priority 3 line waits in the queue");
        Assert.AreEqual(1, VM.QueueCount);
        string first = VM.Current.id; t = 0f;
        while (t < 8f && (VM.Current == null || VM.Current.id == first)) { Assert.IsFalse(VM.Source2D.isPlaying && VM.Source3D.isPlaying); t += Time.unscaledDeltaTime; yield return null; }
        Assert.NotNull(VM.Current, "the queued line plays after the first one ends");
        Assert.AreEqual("fight_start", VM.Current.trigger);
    }

    [UnityTest]
    public IEnumerator Cooldown_OnceOnly_And_No_Immediate_Repeat()
    {
        Assert.NotNull(VoiceManager.Fire("fight_start")); VoiceManager.StopAll("test");
        Assert.IsNull(VoiceManager.Fire("fight_start"), "cooldown blocks the same trigger");
        VoiceManager.ResetCooldownsForTests();
        var a = VoiceManager.Fire("fight_start"); VoiceManager.StopAll("test"); VoiceManager.ResetCooldownsForTests();
        var b = VoiceManager.Fire("fight_start"); VoiceManager.StopAll("test");
        Assert.AreNotEqual(a, b, "the same line is never picked twice in a row");
        Assert.NotNull(VoiceManager.Fire("clock_half")); VoiceManager.StopAll("test"); VoiceManager.ResetCooldownsForTests();
        Assert.IsNull(VoiceManager.Fire("clock_half"), "a once-only line plays only once");
        yield return null;
    }

    [UnityTest]
    public IEnumerator VoiceOnEvent_Helper_Asks_The_Manager()
    {
        var go = new GameObject("VoiceOnEventTest"); var h = go.AddComponent<VoiceOnEvent>(); h.trigger = "jeep_enter";
        h.Fire(); yield return null;
        Assert.NotNull(VoiceManager.Instance.Current, "the helper's request was played");
        Assert.AreEqual("jeep_enter", VoiceManager.Instance.Current.trigger);
        VoiceManager.StopAll("test"); VoiceManager.ResetCooldownsForTests();
        h.FireTrigger("fight_start"); yield return null;
        Assert.AreEqual("fight_start", VoiceManager.Instance.Current.trigger, "FireTrigger(name) asks for another trigger");
        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator Chatter_Line_Is_Not_Said_Again_Within_The_Repeat_Gap()
    {
        // vault_plate has one line (priority 2, no conditions, chance 1): after its 10 s cooldown it must still stay quiet for RepeatGap (45 s on Normal)
        VoiceManager.TestMode = false;
        Assert.AreEqual("pz_vault_plate_01", VoiceManager.Fire("vault_plate")); VoiceManager.StopAll("test");
        VoiceManager.EndCooldownsForTests();   // cooldown and chatter gap over, but the line was said just now
        Assert.IsNull(VoiceManager.Fire("vault_plate"), "the only line of a chatter trigger is not repeated within the repeat gap");
        StringAssert.StartsWith("said ", VoiceManager.Log[VoiceManager.Log.Count - 1].reason);
        Debug.Log($"[VoiceTest] repeat gap {VoiceManager.RepeatGap:0} s on Normal; second vault_plate: {VoiceManager.Log[VoiceManager.Log.Count - 1].result} ({VoiceManager.Log[VoiceManager.Log.Count - 1].reason})");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Frequency_Setting_Scales_Chance_Cooldown_And_Gap()
    {
        VoiceLine chatter = null; foreach (var l in VoiceLibrary.Db.lines) if (l.trigger == "run_long") chatter = l;
        Assert.NotNull(chatter);
        var c = new float[3]; var cd = new float[3]; var gap = new float[3];
        for (int f = 0; f < 3; f++) { GameOptions.VoiceFrequency = f; c[f] = VoiceManager.EffectiveChance(chatter); cd[f] = VoiceManager.EffectiveCooldown(chatter); gap[f] = VoiceManager.EffectiveChatterGap; }
        Assert.Less(c[0], c[1]); Assert.Less(c[1], c[2]);
        Assert.Greater(cd[0], cd[1]); Assert.Greater(cd[1], cd[2]);
        Assert.Greater(gap[0], gap[1]); Assert.Greater(gap[1], gap[2]);
        Debug.Log($"[VoiceTest] run_long chance {c[0]:0.00}/{c[1]:0.00}/{c[2]:0.00}, cooldown {cd[0]:0}/{cd[1]:0}/{cd[2]:0} s, chatter gap {gap[0]:0}/{gap[1]:0}/{gap[2]:0} s (Low/Normal/High)");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Story_Line_Uses_The_New_Voice_And_Wins_Over_Event_Lines()
    {
        Assert.NotNull(VoiceManager.Fire("puzzle_solved")); yield return null;
        var clip = VoiceLibrary.Clip("v3_puj_01"); Assert.NotNull(clip, "story line v3_puj_01 is in the voice database");
        VoiceManager.PlayStory("v3_puj_01", "pujari", "Five treasures. Gone before sunrise.", clip, "calm", true, 1f);
        yield return null;
        Assert.IsTrue(VM.Current.story); Assert.AreEqual(clip, VM.Current.clip);
        VoiceManager.ResetCooldownsForTests();
        Assert.IsNull(VoiceManager.Fire("jump"), "chatter cannot cut into a story line");
    }
}
