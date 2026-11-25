using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Original Courtyard and TempleRing delivery ledger. Pickup completion remains independent of lore delivery.</summary>
    [DefaultExecutionOrder(950)]
    public sealed class PujariLoreDirector : MonoBehaviour
    {
        [Serializable] public sealed class Relic
        {
            public int zone;
            public string name, title, extra, care, aryan, source;
            public string[] lines, choices;
        }
        [Serializable] sealed class Data { public Relic[] zones; }
        public static Action<string, string, Transform> PlayVoice;
        public static Action StopVoices;
        public static Action AdvanceLine;
        float nextLineAt = -1f;
        public static Func<bool> DialogueIsOpen;
        public static PujariLoreDirector Instance { get; private set; }
        public Relic[] Relics { get; private set; }
        public Transform Priest { get; private set; }
        public int PendingZone { get; private set; } = -1;
        public bool Talking { get; private set; }
        int talkingZone = -1;
        float returnHintUntil, cardUntil;
        Relic card;
        OrbitCamera orbit;
        Transform nextLead;
        float nextObjective, returnAt = -1f;
        public bool Ready { get; private set; }
        public bool Original => SceneManager.GetActiveScene().name == Constants.Courtyard;
        public static bool AutoReturnEnabled = true;
        bool transferring;
        static readonly string[] Objectives = { "obj_v3_bazaar", "obj_v3_storeroom", "obj_v3_vaults", "obj_v3_stepwell", "obj_v3_sunwall", "obj_v3_jungle" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void BootOriginal()
        {
            SceneManager.sceneLoaded += (scene, mode) => { if (scene.name == Constants.Courtyard) SpawnOriginal(); };
            if (SceneManager.GetActiveScene().name == Constants.Courtyard) SpawnOriginal();
        }
        static void SpawnOriginal() { if (Instance == null) new GameObject("Pujari Lore — Original Route").AddComponent<PujariLoreDirector>(); }
        public bool Owns(TreasurePickup item)
        { for (int i = 1; i <= 6; i++) if (Pickup(i) == item) return true; return false; }
        public bool DeferStory(TreasurePickup item)
        { if (!Owns(item)) return false; for (int i = 1; i <= 6; i++) if (Pickup(i) == item) return PuzzleSave.Get($"lore.z{i}.handed") == 0; return false; }


        void Awake()
        {
            Instance = this;
            nextLead = new GameObject("Next investigation lead").transform; nextLead.SetParent(transform, false);
            Relics = JsonUtility.FromJson<Data>(Resources.Load<TextAsset>("Story/pujari_lore").text).zones;
            TreasurePickup.OnTaken += OnTaken;
            if (Original) return;
            var prefab = Resources.Load<GameObject>("NPC/Priest");
            var npc = Instantiate(prefab, new Vector3(2.5f, 0.7f, 141f), Quaternion.Euler(0f, 180f, 0f), transform);
            npc.name = "Pujari Ananda — Central Temple";
            // Disable the prefab's old dialogue; this NPC owns the six delivery conversations.
            foreach (var trigger in npc.GetComponentsInChildren<NPCDialogueTrigger>()) Destroy(trigger);
            if (npc.GetComponent<NpcAnimDriver>() == null) npc.AddComponent<NpcAnimDriver>();
            FaceAnimator.For("pujari", npc);
            var handover = npc.AddComponent<PujariLoreHandover>(); handover.director = this;
            var reach = npc.AddComponent<SphereCollider>(); reach.isTrigger = true; reach.radius = 1.2f; reach.center = Vector3.up;
            Priest = npc.transform;
        }
        IEnumerator Start()
        {
            yield return null;
            if (Original)
            {
                while (Pickup(1) == null || Pickup(2) == null || Pickup(3) == null || Pickup(4) == null || Pickup(5) == null || Pickup(6) == null) yield return null;
                while (Priest == null)
                {
                    foreach (var npc in FindObjectsByType<NPCDialogueTrigger>()) if (npc.CharacterId == Constants.Priest) { Priest = npc.transform; break; }
                    if (Priest == null) yield return null;
                }
                FaceAnimator.For("pujari", Priest.gameObject);
                // Retain the initial conversation on the original priest. Its story handler routes pending deliveries here.
                for (int zone = 1; zone <= 6; zone++)
                {
                    var item = Pickup(zone);
                    if (string.IsNullOrEmpty(item.saveId)) item.saveId = item.Id;
                    item.treasureName = Relics[zone - 1].name;
                }
                Ready = true; RefreshPending();
                if (PendingZone > 0) ScheduleReturn();
                yield break;
            }
            // Ground the rendered original model on the actual platform, without changing its animation or source.
            var colliders = Priest.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders) collider.enabled = false;
            if (Physics.Raycast(Priest.position + Vector3.up * 4f, Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                float bottom = float.MaxValue;
                foreach (var renderer in Priest.GetComponentsInChildren<Renderer>()) bottom = Mathf.Min(bottom, renderer.bounds.min.y);
                if (bottom < float.MaxValue) Priest.position += Vector3.up * (hit.point.y - bottom + 0.02f);
            }
            foreach (var collider in colliders) collider.enabled = true;
            Ready = true; RefreshPending();
            if (PendingZone > 0) ScheduleReturn();
        }
        void OnDestroy()
        {
            TreasurePickup.OnTaken -= OnTaken;
            if (Talking) FinishConversation();
            if (Instance == this) Instance = null;
        }
        public TreasurePickup Pickup(int zone)
        {
            if (Original)
            {
                switch (zone)
                {
                    case 1: return BazaarTail.Instance?.Bell; case 2: return BreakInPuzzle.Instance?.Chronicle;
                    case 3: return VaultPuzzle.Instance?.Rosette; case 4: return StepwellPuzzle.Instance?.Vessel;
                    case 5: return SunWallPuzzle.Instance?.Case; case 6: return JungleChase.Instance?.Finial;
                    default: return null;
                }
            }
            var world = NewWorldDirector.Instance;
            if (world == null) return null;
            switch (zone)
            {
                case 1: return world.Bazaar.Bell; case 2: return world.Courtyard.Chronicle;
                case 3: return world.Vaults.Rosette; case 4: return world.Stepwell.Vessel;
                case 5: return world.SunWall.SmugglerMap; case 6: return world.Jungle.GoldenFinial;
                default: return null;
            }
        }
        void OnTaken(TreasurePickup item)
        {
            for (int zone = 1; zone <= 6; zone++) if (Pickup(zone) == item)
            {
                PuzzleSave.Set($"lore.z{zone}.carried", 1);
                RefreshPending();
                ScheduleReturn();
                ObjectiveBanner.Show("RETURN TO PUJARI ANANDA", "Bring " + item.treasureName + " to Pujari Ananda. Returning in 3 seconds", 8f);
                Play($"lore_ary_z{zone}", "aryan");
                return;
            }
        }
        bool ZoneRecovered(int zone) => Original ? (Pickup(zone) != null && (Pickup(zone).Taken || PuzzleSave.Get(Pickup(zone).Id) == 1)) : PuzzleSave.Get($"nz{zone}.complete") == 1;
        public void RefreshPending()
        {
            PendingZone = -1;
            // Pickup's stable save ID also reconstructs pending deliveries after a reload or older save.
            int oldest = int.MaxValue;
            for (int zone = 1; zone <= 6; zone++)
            {
                var item = Pickup(zone);
                if (item == null || PuzzleSave.Get($"lore.z{zone}.handed") == 1) continue;
                if (PuzzleSave.Get(item.Id) != 1 && PuzzleSave.Get($"lore.z{zone}.carried") != 1) continue;
                int order = PuzzleSave.Get($"lore.z{zone}.order");
                if (order == 0) { order = PuzzleSave.Get("lore.order") + 1; PuzzleSave.Set("lore.order", order); PuzzleSave.Set($"lore.z{zone}.order", order); }
                if (order < oldest) { oldest = order; PendingZone = zone; }
            }
            if (PendingZone > 0) PlayerController.ObjectiveFocus = Priest;
        }
        public bool QuickReturn(bool fromMap = false)
        {
            RefreshPending();
            if (PendingZone < 1 || Talking || Brawl.Active != null || (!fromMap && InputGate.GameplayBlocked) || (Jeep.Instance != null && Jeep.Instance.Occupied)) return false;
            var player = ZoneKit.Player;
            if (player == null) return false;
            // Clear ground in front of the platform, rather than teleporting into the priest or temple collider.
            Vector3 at = Original ? Priest.position + Priest.forward * 2.2f : new Vector3(2.5f, 0.1f, 138.7f);
            if (Physics.Raycast(at + Vector3.up * 5f, Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y + 0.08f;
            player.Teleport(at, Quaternion.LookRotation(Vector3.ProjectOnPlane(Priest.position - at, Vector3.up)));
            return true;
        }
        public void HandOver()
        {
            if (Talking || DialogueIsOpen == null || DialogueIsOpen() || Brawl.Active != null) return;
            RefreshPending();
            if (PendingZone < 1) { ObjectiveBanner.Show("PUJARI ANANDA", "Recover a teaching relic, then return to learn its heritage.", 5f); Play("lore_return_01", "pujari"); return; }
            returnAt = -1f;
            Talking = true; talkingZone = PendingZone;
            Gesture(ZoneKit.Player != null ? ZoneKit.Player.gameObject : null, "Interact");
            Gesture(Priest.gameObject, "Point");
            var relic = Relics[talkingZone - 1];
            StopVoices?.Invoke();
            orbit = FindAnyObjectByType<OrbitCamera>();
            if (orbit != null)
            {
                var head = Priest.position + Vector3.up * 1.5f;
                var cameraAt = head + Vector3.back * 3.1f + Vector3.right * 1.2f;
                orbit.SetCutsceneOverride(cameraAt, Quaternion.LookRotation(head - cameraAt));
            }
            string[] lines = { "Aryan: " + relic.aryan, "Pujari Ananda: " + relic.lines[0], "Pujari Ananda: " + relic.lines[1], "Pujari Ananda: " + relic.lines[2] };
            EventBus.RaiseRequestDialogue("Pujari Ananda", lines, relic.choices, choice =>
            {
                EventBus.RaiseRequestDialogue("Pujari Ananda", new[] { choice == 0 ? relic.extra : relic.care }, null, null, CompleteDelivery);
            }, null);
        }
        void CompleteDelivery()
        {
            int zone = talkingZone;
            if (zone > 0 && PuzzleSave.Get($"lore.z{zone}.handed") == 0)
            {
                PuzzleSave.Set($"lore.z{zone}.handed", 1);
                HeritageNotes.Discover($"lore_z{zone}");
                ScoreSystem.HeritageHandover(Pickup(zone).zoneId);
            }
            if (Original && zone > 0) StoryDialogueSystem.Instance?.CompleteMinigame(Pickup(zone).StoryId);
            card = Relics[zone - 1]; cardUntil = Time.unscaledTime + 12f;
            FinishConversation(); RefreshPending();
            bool all = true;
            for (int i = 1; i <= 6; i++) if (PuzzleSave.Get($"lore.z{i}.handed") == 0) all = false;
            if (all)
            {
                PuzzleSave.Set("story.completed", 1);
                PuzzleSave.Set("lore.completed", 1);
                ObjectiveBanner.Show("MISSION COMPLETED", "All six teaching relics returned. Every artifact carries a memory.", 10f);
                // The delivery ledger drives the completion summary below.
            }
            else ObjectiveBanner.Show("HERITAGE RECORDED · +500", PendingZone > 0 ? "Another relic is ready for Pujari Ananda." : "Follow the map to the next unrecovered relic.", 5f);
            if (PendingZone > 0) ScheduleReturn();
            if (PendingZone < 1)
            {
                if (all) EventBus.RaiseUpdateObjective("Mission completed", "All six teaching relics returned to Pujari Ananda.", "Central Temple Courtyard");
                else for (int next = 1; next <= 6; next++) if (!ZoneRecovered(next))
                {
                    EventBus.RaiseUpdateObjective("Continue the investigation", "Explore " + V3Places.Names[Original ? next : next - 1] + ".", "Zone " + next);
                    break;
                }
            }
        }
        void FinishConversation()
        {
            StopVoices?.Invoke();
            if (orbit != null) orbit.ClearCutsceneOverride();
            Talking = false; talkingZone = -1; nextLineAt = -1f;
        }
        public static bool TryPlayLine(string speaker, string text)
        {
            var lore = Instance;
            if (lore == null || !lore.Talking || lore.talkingZone < 1) return false;
            var relic = lore.Relics[lore.talkingZone - 1];
            string id = null;
            if (text == relic.aryan) id = $"lore_ary_z{relic.zone}";
            for (int i = 0; i < relic.lines.Length; i++) if (text == relic.lines[i]) id = $"lore_puj_z{relic.zone}_{i + 1}";
            if (text == relic.extra) id = $"lore_puj_z{relic.zone}_extra";
            if (text == relic.care) id = $"lore_puj_z{relic.zone}_care";
            if (id == null) return false;
            Play(id, speaker == "Aryan" ? "aryan" : "pujari");
            lore.nextLineAt = Time.time + (VoiceLibrary.Clip(id) != null ? VoiceLibrary.Clip(id).length : 5f) + 0.5f;
            // The legacy story listener frames Aryan first; this callback then owns the heritage two-shot.
            if (lore.orbit != null)
            {
                Vector3 head = lore.Priest.position + Vector3.up * 1.5f;
                Vector3 cameraAt = head + new Vector3(-2.4f, 0.4f, -3.8f);
                lore.orbit.SetCutsceneOverride(cameraAt, Quaternion.LookRotation(head - cameraAt));
            }
            return true;
        }
        static void Play(string id, string speaker)
        {
            var line = VoiceLibrary.Line(id);
            if (line == null || line.clip == null) return;
            PlayVoice?.Invoke(id, speaker, speaker == "pujari" ? Instance.Priest : null);
        }
        static void Gesture(GameObject who, string trigger)
        {
            var animator = who != null ? who.GetComponentInChildren<Animator>() : null;
            if (animator != null) foreach (var parameter in animator.parameters)
                if (parameter.name == trigger && parameter.type == AnimatorControllerParameterType.Trigger) { animator.SetTrigger(trigger); break; }
        }
        public void ScheduleReturn()
        {
            returnAt = Time.time + 3f; returnHintUntil = Time.unscaledTime + 12f;
        }
        IEnumerator ReturnAndExplain()
        {
            transferring = true;
            var jeep = Jeep.Instance;
            if (jeep != null && jeep.Occupied) yield return jeep.GetOut();
            if (QuickReturn()) { yield return null; HandOver(); }
            transferring = false;
            if (!Talking && PendingZone > 0) returnAt = Time.time + 1f;
        }
        public void SkipLevel()
        {
            if (!Ready || Talking) return;
            RefreshPending();
            if (PendingZone > 0) { returnAt = Time.time; return; }
            if (!Original) { NewWorldDirector.Instance?.SkipCurrentTask(); return; }
            int zone = 1;
            for (; zone <= 6; zone++) if (!ZoneRecovered(zone)) break;
            if (zone > 6) { ObjectiveBanner.Show("ALL RELICS RETURNED", "The heritage explanations are in your journal.", 4f); return; }
            if (Brawl.Active != null) Brawl.Active.ResolveSkipped(false);
            PuzzleSave.Set("fight." + Pickup(zone).zoneId, 1);
            if (zone == 1) BazaarTail.Instance.SkipForHandover();
            if (zone == 2) Solve(BreakInPuzzle.Instance.Group);
            if (zone == 3) Solve(VaultPuzzle.Instance.Group);
            if (zone == 4) { StepwellPuzzle.Instance.SluiceA.SetState(1); StepwellPuzzle.Instance.SluiceB.SetState(1); StepwellPuzzle.Instance.Drain.SetState(0); Solve(StepwellPuzzle.Instance.Group); }
            if (zone == 5) Solve(SunWallPuzzle.Instance.Group);
            if (zone == 6) JungleChase.Instance.SkipForHandover();
            StoryDialogueSystem.Instance?.PrepareHeritageSkip(Objectives[zone - 1]);
            var item = Pickup(zone); item.enabled = true; item.Interact();
            ScoreSystem.FinishZone(item.zoneId);
            ObjectiveBanner.Show("LEVEL SKIPPED", "Returning to Ananda in 3 seconds for the heritage story.", 3f);
        }
        static void Solve(PuzzleGroup group)
        {
            if (group == null) return;
            foreach (var c in group.conditions) if (c.element != null) c.element.SetState(c.state, true, true);
            group.Evaluate();
        }
        void LateUpdate()
        {
            if (!Ready) return;
            if (PendingZone > 0 && AutoReturnEnabled && !Talking && !transferring && returnAt >= 0f && Time.time >= returnAt && !InputGate.GameplayBlocked && Brawl.Active == null)
                StartCoroutine(ReturnAndExplain());
            if (Talking && nextLineAt > 0f && Time.time >= nextLineAt && Time.timeScale > 0f)
            { nextLineAt = -1f; AdvanceLine?.Invoke(); }
            if (Talking)
                ScreenText.Panel("relic_history_title", Relics[talkingZone - 1].title, "<b>JAGANNATH TEMPLE · PURI</b>\n" + Relics[talkingZone - 1].lines[0] + "\n\n<size=80%>Recovered object: fictional teaching replica. Listen to Ananda below.</size>", PanelAnchor.TopRight, 620f);

            if (Talking && (DialogueIsOpen == null || !DialogueIsOpen())) FinishConversation();
            if (PendingZone > 0)
            {
                PlayerController.ObjectiveFocus = Priest;
                if (Time.unscaledTime >= nextObjective)
                {
                    nextObjective = Time.unscaledTime + 0.3f;
                    EventBus.RaiseUpdateObjective("Return to Pujari Ananda", "Bring " + Relics[PendingZone - 1].name + " to the Central Temple.", "Central Temple Courtyard");
                }
            }
            else if (!Talking)
            {
                PlayerController.ObjectiveFocus = null;
                for (int zone = 1; zone <= 6; zone++) if (!ZoneRecovered(zone))
                {
                    nextLead.position = V3Places.Get(Original ? zone : zone - 1).at;
                    PlayerController.ObjectiveFocus = nextLead; break;
                }
            }
            if (!Talking && !InputGate.GameplayBlocked && Time.unscaledTime < cardUntil && card != null && PendingZone < 1 && PuzzleSave.Get("lore.completed") == 0)
                ScreenText.Panel("heritage_card", card.title, card.lines[1] + "\n\nTeaching replica · Recorded in Journal > Heritage Notes.", PanelAnchor.TopRight, 620f);
            if (PuzzleSave.Get("lore.completed") == 1 && !Talking && !InputGate.GameplayBlocked)
            {
                float seconds = 0f; foreach (var score in ScoreSystem.Zones) seconds += score.seconds;
                ScreenText.Panel("lore_complete", "MISSION COMPLETED", "Every temple holds a story. Every artifact carries a memory.\nProtecting heritage means protecting our history.\n\n6 / 6 teaching relics returned · Score " + ScoreSystem.Total + " · Time " + seconds.ToString("0") + " s", PanelAnchor.Center, 1100f);
            }
            var kb = Keyboard.current;
            if (PendingZone > 0 && kb != null && kb.tKey.wasPressedThisFrame && !InputGate.GameplayBlocked) QuickReturn();
            if (PendingZone > 0 && Time.unscaledTime < returnHintUntil && !InputGate.GameplayBlocked)
                ScreenText.Panel("lore_return", "RETURN TO PUJARI ANANDA", "Returning to Ananda in " + Mathf.CeilToInt(Mathf.Max(0f, returnAt - Time.time)) + " seconds · [T] Return now", PanelAnchor.TopRight, 620f);
        }
    }
}
