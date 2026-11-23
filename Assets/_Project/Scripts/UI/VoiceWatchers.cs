using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Gameplay;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Watches the game and fires voice triggers when something changes (edge-triggered, so a state that lasts does not
    /// keep talking). Reading state here keeps the voice system out of the gameplay scripts.
    /// Triggers: walking, running, jumping, landing, areas, drops, rain, being lost or stuck; the jeep; puzzles and hints;
    /// the pot and lantern, vault, stepwell and Sun Wall steps; the bazaar tail; clues and treasures; fights; the Dawn
    /// Clock; NPCs near the player. Every name used here is a trigger column value in Docs/VOICE_LINES.csv.
    /// </summary>
    public sealed class VoiceWatchers : MonoBehaviour
    {
        static void Fire(string t, Transform at = null, string ctx = null) => VoiceManager.Fire(t, at, ctx);
        float slow; string scene = "";

        // movement
        bool wasMoving, airborne; float runFor, runWindow, airTop, stuckFor, lostFor; int jumps = -1; Vector3 lastPos; string zone = "";
        // jeep
        bool wasIn, wasNear, offSaid, fastArmed = true, driftArmed = true; float inFor, startAt = -1f, stuckJeep, nearArmT = -1f; int dmgState = -1; float dmgTotal = -1f, lastImpact = 0f;
        // puzzles
        readonly HashSet<PuzzleElement> hooked = new HashSet<PuzzleElement>(); readonly HashSet<PuzzleGroup> groups = new HashSet<PuzzleGroup>();
        readonly Dictionary<PuzzleGroup, int> met = new Dictionary<PuzzleGroup, int>(); readonly HashSet<PuzzleGroup> started = new HashSet<PuzzleGroup>(), almost = new HashSet<PuzzleGroup>(), noticed = new HashSet<PuzzleGroup>();
        readonly Dictionary<PuzzleGroup, int> hints = new Dictionary<PuzzleGroup, int>(); int storyHints = -1; string hintObjective = ""; int hintTier;
        bool potWhole, lampOn, flagUp, gateOpen, flooded, levelled, socketLit, wallOpen, ringsSeen;
        // world, fights, clock, NPCs
        readonly Dictionary<string, int> evidence = new Dictionary<string, int>(); bool boardReady; readonly Dictionary<TreasurePickup, bool> items = new Dictionary<TreasurePickup, bool>();
        Brawl fight; readonly HashSet<Brawl> finished = new HashSet<Brawl>(); float hurtSeen, tauntAt; int combo, finishers, dodges, retries; bool lowSaid; readonly HashSet<BrawlEnemy> downSeen = new HashSet<BrawlEnemy>();
        BazaarTail.Phase tail = BazaarTail.Phase.Waiting; int spotted; float lastClock = -1f; bool rainOn;
        readonly Dictionary<Transform, float> npcSaid = new Dictionary<Transform, float>();

        void Awake()
        {
            DawnClock.OnPenalty += (m, why) => { if (why != "fast travel") Fire("clock_penalty"); };
            StoryDialogueSystem.OnObjectiveChanged += _ => lostFor = 0f;
            SceneManager.activeSceneChanged += (a, b) => ResetScene();
        }

        void ResetScene()
        {
            hooked.Clear(); groups.Clear(); finished.Clear(); met.Clear(); started.Clear(); almost.Clear(); noticed.Clear(); hints.Clear(); items.Clear(); npcSaid.Clear(); downSeen.Clear();
            jumps = -1; dmgState = -1; dmgTotal = -1f; wasIn = wasNear = false; fight = null; lastClock = -1f; zone = ""; evidence.Clear();
            potWhole = lampOn = flagUp = gateOpen = flooded = levelled = socketLit = wallOpen = ringsSeen = false; tail = BazaarTail.Phase.Waiting; spotted = 0;
        }

        void Update()
        {
            var s = SceneManager.GetActiveScene().name;
            if (s != scene) { scene = s; ResetScene(); }
            if (s == "MainMenu" || IntroCinematic.Playing) return;
            var pc = VoiceManager.Player; if (pc == null) return;
            float dt = Time.deltaTime; if (dt <= 0f) return;
            Movement(pc, dt); Jeep_(pc, dt); Fights(pc);
            slow -= dt; if (slow > 0f) return; slow = 0.25f;   // the slower checks, four times a second
            Puzzles(pc); Specific(pc); Tail(); Clues(); Clock(); Surroundings(pc); Npcs(pc);
            lostFor += 0.25f;
            if (lostFor > 90f && Brawl.Active == null && !VoiceManager.InJeep) { lostFor = 0f; Fire("lost"); }
        }

        // ------------------------------------------------------------------ walking, running, jumping, areas
        void Movement(PlayerController pc, float dt)
        {
            var cc = pc.GetComponent<CharacterController>(); bool grounded = cc == null || cc.isGrounded;
            bool moving = pc.enabled && pc.IsMoving && !VoiceManager.InJeep;
            if (moving && !wasMoving) Fire("move_first");
            wasMoving = moving;
            bool running = moving && pc.IsRunning;
            runFor = running ? runFor + dt : 0f;
            if (runFor >= 10f && runFor - dt < 10f) Fire("run_long");
            runWindow = Mathf.Max(0f, runWindow + (running ? dt : -dt * 0.6f));
            if (runWindow >= 28f) { runWindow = 0f; Fire("run_tired"); }
            if (jumps < 0) jumps = pc.JumpCount;
            if (pc.JumpCount > jumps) { jumps = pc.JumpCount; Fire("jump"); }
            float y = pc.transform.position.y;
            if (!grounded) { if (!airborne) { airborne = true; airTop = y; } airTop = Mathf.Max(airTop, y); }
            else if (airborne) { airborne = false; if (airTop - y > 2.5f && pc.enabled) Fire("land_hard"); }
            // pushing into something: wants to move but hardly moves (dead end)
            float speed = (pc.transform.position - lastPos).magnitude / dt; lastPos = pc.transform.position;
            stuckFor = moving && speed < 0.3f && Brawl.Active == null ? stuckFor + dt : 0f;
            if (stuckFor > 2.5f) { stuckFor = -8f; Fire("dead_end"); }
            if (moving && speed > 0.5f) lostFor = Mathf.Max(0f, lostFor - dt * 0.2f);   // exploring slowly counts for a little
        }

        void Surroundings(PlayerController pc)
        {
            string z = VoiceManager.CurrentZone;
            if (z != zone) { zone = z; if (z.Length > 0) Fire("area_enter", null, "zone=" + z); }
            var w = LegacyOfTemples.World.WeatherManager.Instance; bool rain = w != null && w.Intensity > 0.3f;
            if (rain && !rainOn) Fire("rain"); rainOn = rain;
            // a long drop just ahead of the player
            if (pc.enabled && pc.IsMoving && !VoiceManager.InJeep)
            {
                Vector3 ahead = pc.transform.position + pc.transform.forward * 1.3f + Vector3.up * 0.6f;
                bool hit = Physics.Raycast(ahead, Vector3.down, out RaycastHit h, 12f, ~0, QueryTriggerInteraction.Ignore);
                float drop = hit ? pc.transform.position.y - h.point.y : 12f;
                if (drop > 3.2f && Physics.Raycast(pc.transform.position + Vector3.up * 0.3f, Vector3.down, 1.0f, ~0, QueryTriggerInteraction.Ignore)) Fire("danger_drop");
            }
        }

        // ------------------------------------------------------------------ the jeep
        void Jeep_(PlayerController pc, float dt)
        {
            var j = Jeep.Instance; if (j == null) { wasIn = false; return; }
            bool inside = j.Occupied;
            float dist = Vector3.Distance(pc.transform.position, j.transform.position);
            bool near = !inside && dist < 5f && pc.enabled;
            if (near && !wasNear) Fire("jeep_approach");
            wasNear = near;
            if (inside && !wasIn) { Fire("jeep_enter"); startAt = Time.time + 1.6f; inFor = 0f; offSaid = false; lostFor = 0f; }
            if (!inside && wasIn) Fire(inFor >= 20f ? "jeep_park" : "jeep_exit");
            wasIn = inside;
            // damage (works for a parked jeep too)
            if (dmgState < 0) { dmgState = j.DamageState; dmgTotal = j.TotalDamage; lastImpact = j.LastImpact; }
            if (j.DamageState > dmgState)
            {
                Fire(j.DamageState >= 3 ? "jeep_engine_dead" : j.DamageState == 2 ? "jeep_crash_heavy" : "jeep_crash_light");
                if (j.DamageState == 2) Fire("jeep_smoke");
                dmgState = j.DamageState; dmgTotal = j.TotalDamage;
            }
            else if (j.TotalDamage > dmgTotal + 0.5f) { dmgTotal = j.TotalDamage; if (inside) Fire("jeep_crash_light"); }
            if (!inside) return;
            inFor += dt;
            if (startAt > 0f && Time.time >= startAt) { startAt = -1f; Fire("jeep_start"); }
            float v = j.Car.CurrentSpeed;
            if (!offSaid && v > 4f) { offSaid = true; Fire("jeep_drive_off"); }
            if (fastArmed && v > 16.7f) { fastArmed = false; Fire("jeep_fast"); } else if (v < 11f) fastArmed = true;
            var rb = j.GetComponent<Rigidbody>();
            float side = rb != null ? Mathf.Abs(Vector3.Dot(rb.linearVelocity, j.transform.right)) : 0f;
            if (driftArmed && v > 7f && side > 4.5f) { driftArmed = false; Fire("jeep_drift"); } else if (side < 1.5f) driftArmed = true;
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame && v > 11f) Fire("jeep_handbrake");
            bool gas = kb != null && (kb.wKey.isPressed || kb.sKey.isPressed) || j.Car.autopilot && Mathf.Abs(j.Car.autoThrottle) > 0.1f;
            stuckJeep = gas && Mathf.Abs(v) < 0.3f ? stuckJeep + dt : 0f;
            if (stuckJeep > 3f) { stuckJeep = -10f; Fire("jeep_stuck"); }
            // a near miss: something solid right ahead at speed, then no crash within a second
            if (nearArmT < 0f && v > 12f && rb != null && Physics.SphereCast(j.transform.position + Vector3.up * 0.9f, 0.9f, rb.linearVelocity.normalized, out RaycastHit h, 6f, ~0, QueryTriggerInteraction.Ignore) && !h.collider.transform.IsChildOf(j.transform) && h.normal.y < 0.6f)
            { nearArmT = Time.time + 1.1f; lastImpact = j.LastImpact; }
            if (nearArmT > 0f && Time.time >= nearArmT) { nearArmT = -1f; if (Mathf.Approximately(j.LastImpact, lastImpact) && j.DamageState == dmgState) Fire("jeep_near_miss"); }
            if (JungleChase.Instance != null && JungleChase.Instance.Now == JungleChase.Phase.Court && JungleChase.Instance.Built) Fire("jeep_arrive");
        }

        // ------------------------------------------------------------------ puzzles (all) and hints
        void Puzzles(PlayerController pc)
        {
            foreach (var g in PuzzleGroup.All)
            {
                if (g == null) continue;
                if (groups.Add(g))
                {
                    met[g] = Count(g); hints[g] = g.HintsUsed;
                    g.OnSolved += x => { lostFor = 0f; if (x.zoneId != SunWallPuzzle.ZoneId) Fire("puzzle_solved"); };
                    foreach (var c in g.conditions) if (c.element != null && hooked.Add(c.element)) { var grp = g; c.element.Changed += e => PartChanged(grp, e); }
                }
                if (g.HintsUsed > hints[g]) { hints[g] = g.HintsUsed; Fire("hint_" + Mathf.Clamp(g.HintsUsed, 1, 3)); }
                // noticing a puzzle the first time the player comes close
                if (!g.Solved && !noticed.Contains(g))
                    foreach (var c in g.conditions) if (c.element != null && Vector3.Distance(c.element.transform.position, pc.transform.position) < 3.5f) { noticed.Add(g); Fire("notice"); break; }
            }
            // hints from the guide (story hints, keys 1-3 in H)
            var st = StoryDialogueSystem.Instance;
            if (st != null)
            {
                string obj = st.Current != null ? st.Current.id : "";
                if (obj != hintObjective) { hintObjective = obj; hintTier = 0; storyHints = st.HintsUsed; }
                if (storyHints < 0) storyHints = st.HintsUsed;
                if (st.HintsUsed > storyHints) { storyHints = st.HintsUsed; hintTier = Mathf.Min(3, hintTier + 1); Fire("hint_" + hintTier); }
            }
        }

        static int Count(PuzzleGroup g) { int n = 0; foreach (var c in g.conditions) if (c.element != null && c.element.State == c.state) n++; return n; }

        void PartChanged(PuzzleGroup g, PuzzleElement e)
        {
            lostFor = 0f;
            if (g.Solved) return;
            if (started.Add(g)) Fire("puzzle_start");
            // the specific puzzles speak about their own steps instead of the general lines
            var bi = BreakInPuzzle.Instance;
            if (bi != null && g == bi.Group)
            {
                bool shard = false; foreach (var so in bi.Sockets) if (so == e) shard = true;
                if (shard && e.State == 1) Fire("pot_shard");
                met[g] = Count(g); return;
            }
            var sw = SunWallPuzzle.Instance; var sp = StepwellPuzzle.Instance;
            if ((sw != null && g == sw.Group) || (sp != null && g == sp.Group)) { met[g] = Count(g); return; }   // their own lines (beam, flood, level)
            int before = met.TryGetValue(g, out var b) ? b : 0, now = Count(g); met[g] = now; int total = g.conditions.Count;
            if (now == total - 1 && before < total - 1 && almost.Add(g)) Fire("puzzle_almost");
            else if (now > before) Fire("puzzle_step");
            else if (now < before) Fire("puzzle_wrong");
        }

        void Hook(PuzzleElement e, string trigger, int whenState)
        {
            if (e == null || !hooked.Add(e)) return;
            e.Changed += x => { lostFor = 0f; if (whenState < 0 || x.State == whenState) Fire(trigger); };
        }

        void Specific(PlayerController pc)
        {
            var bi = BreakInPuzzle.Instance;
            if (bi != null && bi.Built)
            {
                if (bi.PotWhole && !potWhole) Fire("pot_whole"); potWhole = bi.PotWhole;
                bool lamp = bi.LampSpot != null && bi.LampSpot.State == 1; if (lamp && !lampOn && bi.PotWhole) Fire("lantern_stain"); lampOn = lamp;
                bool flag = bi.Flagstone != null && bi.Flagstone.State == 1; if (flag && !flagUp) Fire("flagstone"); flagUp = flag;
            }
            var vp = VaultPuzzle.Instance;
            if (vp != null && vp.Plates != null) foreach (var pl in vp.Plates) Hook(pl, "vault_plate", 1);
            var spw = StepwellPuzzle.Instance;
            if (spw != null) { Hook(spw.SluiceA, "stepwell_sluice", -1); Hook(spw.SluiceB, "stepwell_sluice", -1); }
            var swl = SunWallPuzzle.Instance;
            if (swl != null && swl.Mirrors != null) foreach (var m in swl.Mirrors) Hook(m, "sunwall_mirror", -1);
            if (vp != null)
            {
                if (vp.GateOpen && !gateOpen) Fire("vault_gate"); gateOpen = vp.GateOpen;
                if (!ringsSeen && Vector3.Distance(pc.transform.position, vp.transform.TransformPoint(new Vector3(5f, 0f, 0f))) < 3f) { ringsSeen = true; Fire("vault_rings"); }
            }
            var sp = StepwellPuzzle.Instance;
            if (sp != null && sp.Basin != null)
            {
                if (sp.Basin.State >= sp.Basin.maxLevel && !flooded) { flooded = true; Fire("stepwell_flood"); }
                if (flooded && sp.Basin.State == 2 && !levelled) { levelled = true; Fire("stepwell_level"); }
            }
            var sw = SunWallPuzzle.Instance;
            if (sw != null && sw.Sockets != null)
            {
                int lit = 0; foreach (var k in sw.Sockets) if (k != null && k.State == 1) lit++;
                if (lit == 1 && !socketLit && !sw.Group.Solved) { socketLit = true; Fire("sunwall_socket"); }
                if (sw.Group.Solved && !wallOpen) { wallOpen = true; Fire("sunwall_open"); }
            }
        }

        // ------------------------------------------------------------------ the bazaar tail
        void Tail()
        {
            var bt = BazaarTail.Instance; if (bt == null || bt.Courier == null) return;
            var now = bt.Now;
            if (now != tail)
            {
                if (now == BazaarTail.Phase.Walking && tail == BazaarTail.Phase.Waiting) Fire("bazaar_follow");
                if (tail == BazaarTail.Phase.LookingBack && now == BazaarTail.Phase.Walking && bt.Spotted == spotted) Fire("bazaar_lookback");
                if (now == BazaarTail.Phase.Arrived) Fire("bazaar_yard");
                tail = now;
            }
            if (bt.Spotted > spotted) { spotted = bt.Spotted; if (spotted < 3) Fire("bazaar_spotted"); }
            var pc = VoiceManager.Player;
            if (now == BazaarTail.Phase.Walking && pc != null && Vector3.Distance(pc.transform.position, bt.Courier.position) > 28f) Fire("bazaar_far");
        }

        // ------------------------------------------------------------------ clues, the evidence board, treasures
        void Clues()
        {
            foreach (var p in EvidenceBoard.Pieces)
            {
                int v = PuzzleSave.Get(p.key);
                if (!evidence.TryGetValue(p.key, out int was)) { evidence[p.key] = v; continue; }   // first look after loading: no line
                if (v == 1 && was == 0) { lostFor = 0f; Fire("clue_found", null, "evidence=" + p.key.Replace("evidence.", "")); }
                evidence[p.key] = v;
            }
            if (EvidenceBoard.CanName && !boardReady) Fire("board_ready"); boardReady = EvidenceBoard.CanName;
            foreach (var t in FindObjectsByType<TreasurePickup>())
            {
                if (!items.TryGetValue(t, out bool was)) { items[t] = t.Taken; continue; }
                if (t.Taken && !was) { lostFor = 0f; Fire("item_found"); }
                items[t] = t.Taken;
            }
        }

        // ------------------------------------------------------------------ fights
        void Fights(PlayerController pc)
        {
            var b = Brawl.Active;
            if (b != fight)
            {
                if (fight != null && fight.Won && finished.Add(fight)) Fire("fight_won");
                fight = b;
                if (b != null && !b.Won)
                {
                    lostFor = 0f; hurtSeen = b.DamageTaken; combo = b.MaxCombo; finishers = b.Finishers; dodges = b.Dodges; retries = b.Retries; lowSaid = false; downSeen.Clear();
                    var e = FirstEnemy(b); Fire("enemy_spotted", e != null ? e.transform : null, "enemy=" + Kind(e)); Fire("fight_start");
                    tauntAt = Time.time + Random.Range(7f, 12f);
                }
            }
            if (b == null) return;
            if (b.Won) { if (finished.Add(b)) Fire("fight_won"); return; }
            if (b.DamageTaken > hurtSeen + 0.5f) { hurtSeen = b.DamageTaken; Fire("fight_hurt"); }
            if (!lowSaid && b.Health < 30f) { lowSaid = true; Fire("fight_low"); }
            if (b.MaxCombo >= 3 && b.MaxCombo > combo) { combo = b.MaxCombo; Fire("fight_combo"); }
            if (b.Finishers > finishers) { finishers = b.Finishers; Fire("fight_finish"); }
            if (b.Dodges > dodges) { dodges = b.Dodges; Fire("fight_dodge"); }
            if (b.Retries > retries) { retries = b.Retries; Fire("fight_retry"); }
            foreach (var e in b.Enemies) if (e != null && !e.Alive && downSeen.Add(e)) Fire("enemy_down", e.transform, "enemy=" + Kind(e));
            if (Time.time > tauntAt)
            {
                tauntAt = Time.time + Random.Range(9f, 16f);
                var alive = b.Enemies.FindAll(x => x != null && x.Alive);
                if (alive.Count > 0) { var e = alive[Random.Range(0, alive.Count)]; Fire("fight_taunt", e.transform, "enemy=" + Kind(e)); }
            }
        }

        static BrawlEnemy FirstEnemy(Brawl b) { foreach (var e in b.Enemies) if (e != null) return e; return null; }
        static string Kind(BrawlEnemy e)
        {
            if (e == null) return "robber";
            string n = e.name.ToLowerInvariant();
            return n.Contains("kishore") ? "kishore" : n.Contains("bhaskar") ? "bhaskar" : n.Contains("courier") ? "courier" : "robber";
        }

        // ------------------------------------------------------------------ the Dawn Clock
        void Clock()
        {
            var c = DawnClock.Instance; if (c == null) return;
            float m = c.Minutes, dawn = DawnClock.DawnMinute;
            if (lastClock < 0f) { lastClock = m; return; }   // no warnings for what was already past when the scene loaded
            if (lastClock < dawn * 0.5f && m >= dawn * 0.5f) Fire("clock_half");
            if (lastClock < dawn - 5f && m >= dawn - 5f) Fire("clock_5");
            if (lastClock < dawn - 1f && m >= dawn - 1f) Fire("clock_1");
            lastClock = m;
        }

        // ------------------------------------------------------------------ NPCs near the player
        void Npcs(PlayerController pc)
        {
            if (Brawl.Active != null || VoiceManager.InJeep) return;
            Vector3 p = pc.transform.position; Transform best = null; string who = null; float bd = 4f;
            foreach (var n in FindObjectsByType<NPCDialogueTrigger>())
            {
                string s = n.Id == "npc_priest" ? "pujari" : n.Id == "npc_villager" ? "meera" : null; if (s == null || !n.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(p, n.transform.position); if (d < bd) { bd = d; best = n.transform; who = s; }
            }
            var mc = MadhavCameos.Instance;
            if (mc != null && mc.Madhav != null && mc.Madhav.gameObject.activeInHierarchy) { float d = Vector3.Distance(p, mc.Madhav.position); if (d < bd) { bd = d; best = mc.Madhav; who = "madhav"; } }
            var vl = VillageLife.Instance;
            if (vl != null) foreach (var t in vl.Npcs) { if (t == null || !t.gameObject.activeInHierarchy) continue; float d = Vector3.Distance(p, t.position); if (d < Mathf.Min(bd, 3.5f)) { bd = d; best = t; who = t.name.Contains("Male") ? "villager_m" : "villager_f"; } }
            if (best == null) return;
            if (npcSaid.TryGetValue(best, out float last) && Time.time - last < 60f) return;
            npcSaid[best] = Time.time;
            Fire("npc_near", best, "speaker=" + who);
        }
    }
}
