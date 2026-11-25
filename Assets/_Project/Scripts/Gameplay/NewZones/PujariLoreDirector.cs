using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>TempleRing-only delivery ledger. Pickup completion remains independent of lore delivery.</summary>
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
        float nextObjective;

        void Awake()
        {
            Instance = this;
            nextLead = new GameObject("Next investigation lead").transform; nextLead.SetParent(transform, false);
            Relics = JsonUtility.FromJson<Data>(Resources.Load<TextAsset>("Story/pujari_lore").text).zones;
            TreasurePickup.OnTaken += OnTaken;
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
            RefreshPending();
        }
        void OnDestroy()
        {
            TreasurePickup.OnTaken -= OnTaken;
            if (Talking) FinishConversation();
            if (Instance == this) Instance = null;
        }
        public TreasurePickup Pickup(int zone)
        {
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
                returnHintUntil = Time.unscaledTime + 12f;
                ObjectiveBanner.Show("RETURN TO PUJARI ANANDA", "Bring " + item.treasureName + " to the Central Temple. [T] Quick return", 8f);
                Play($"lore_ary_z{zone}", "aryan");
                return;
            }
        }
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
            Vector3 at = new Vector3(2.5f, 0.1f, 138.7f);
            if (Physics.Raycast(at + Vector3.up * 5f, Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore)) at.y = hit.point.y + 0.08f;
            player.Teleport(at, Quaternion.identity);
            return true;
        }
        public void HandOver()
        {
            if (Talking || DialogueIsOpen == null || DialogueIsOpen() || Brawl.Active != null) return;
            RefreshPending();
            if (PendingZone < 1) { ObjectiveBanner.Show("PUJARI ANANDA", "Recover a teaching relic, then return to learn its heritage.", 5f); Play("lore_return_01", "pujari"); return; }
            Talking = true; talkingZone = PendingZone;
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
            if (PendingZone < 1)
            {
                if (all) EventBus.RaiseUpdateObjective("Mission completed", "All six teaching relics returned to Pujari Ananda.", "Central Temple Courtyard");
                else for (int next = 1; next <= 6; next++) if (PuzzleSave.Get($"nz{next}.complete") == 0)
                {
                    EventBus.RaiseUpdateObjective("Continue the investigation", "Explore " + V3Places.Names[next - 1] + ".", "Zone " + next);
                    break;
                }
            }
        }
        void FinishConversation()
        {
            StopVoices?.Invoke();
            if (orbit != null) orbit.ClearCutsceneOverride();
            Talking = false; talkingZone = -1;
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
        void LateUpdate()
        {
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
                for (int zone = 1; zone <= 6; zone++) if (PuzzleSave.Get($"nz{zone}.complete") == 0)
                {
                    nextLead.position = V3Places.Get(zone - 1).at;
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
                ScreenText.Panel("lore_return", "RETURN TO PUJARI ANANDA", "[T] Quick return to the Central Temple · [M] Map", PanelAnchor.TopRight, 620f);
        }
    }
}
