using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using LegacyOfTemples.Characters;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay.Puzzles;

namespace LegacyOfTemples.Gameplay
{
    public enum BrawlType { Brawler, Knife, Heavy, Boss }

    /// <summary>One robber in a V3 fight: circles, waits for an attack token, telegraphs, strikes, staggers, goes down.</summary>
    public sealed class BrawlEnemy : MonoBehaviour
    {
        public enum State { Approach, Circle, Telegraph, Strike, Recover, Staggered, Down }
        public BrawlType type; public float maxHealth = 60f, damage = 12f, telegraph = 0.7f, speed = 2.6f, reach = 2.3f; public bool unblockable;
        public float Health { get; private set; }
        public State Now { get; private set; }
        public int Phase { get; private set; } = 1;
        public bool Alive => Now != State.Down;
        public bool bossThirdPhase; float stepT;
        internal Brawl fight; float t, nextTry, circleDir = 1f, guardUntil, recentAt; int recent;
        public bool Guarding => Time.time < guardUntil && Now != State.Staggered; Animator anim; readonly HashSet<string> ps = new HashSet<string>(); Renderer[] rends; MaterialPropertyBlock mpb;

        public void Setup(BrawlType ty, float difficulty)
        {
            type = ty;
            switch (ty)
            {
                case BrawlType.Knife: maxHealth = 70f; damage = 9f; telegraph = 0.45f; speed = 3.6f; reach = 2.0f; break;
                case BrawlType.Heavy: maxHealth = 160f; damage = 20f; telegraph = 0.95f; speed = 1.9f; reach = 2.6f; unblockable = true; break;
                case BrawlType.Boss: maxHealth = 320f; damage = 16f; telegraph = 0.7f; speed = 2.8f; reach = 2.8f; break;
                default: maxHealth = 90f; damage = 12f; telegraph = 0.7f; speed = 2.6f; reach = 2.3f; break;
            }
            damage *= difficulty; Health = maxHealth; Now = State.Approach; Phase = 1; nextTry = Time.time + UnityEngine.Random.Range(0.5f, 1.3f); guardUntil = 0f; recent = 0; circleDir = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            anim = GetComponentInChildren<Animator>(); if (anim != null) { anim.applyRootMotion = false; foreach (var p in anim.parameters) ps.Add(p.name); }
            rends = GetComponentsInChildren<Renderer>(); mpb = new MaterialPropertyBlock();
        }
        void Trig(string p) { if (anim != null && ps.Contains(p)) anim.SetTrigger(p); }
        void Flag(string p, bool v) { if (anim != null && ps.Contains(p)) anim.SetBool(p, v); }
        void Num(string p, float v) { if (anim != null && ps.Contains(p)) anim.SetFloat(p, v, 0.12f, Time.deltaTime); }
        void Go(State s) { Now = s; t = 0f; }
        void Glow(float k)
        {   // the telegraph: the attacker flashes warm before the blow lands
            foreach (var r in rends) { if (r == null) continue; r.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", new Color(1f, 0.35f, 0.1f) * k); r.SetPropertyBlock(mpb); }
        }

        public void Tick(Transform player, int index, int count)
        {
            if (Now == State.Down) return;
            t += Time.deltaTime;
            Vector3 to = player.position - transform.position; to.y = 0f; float dist = to.magnitude; Vector3 dir = dist > 0.01f ? to / dist : transform.forward;
            if (Now != State.Staggered) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 9f);
            float moved = 0f; float sp = speed * (Phase == 2 ? 1.25f : 1f);
            switch (Now)
            {
                case State.Approach:
                    if (dist > reach + 0.9f) { Move(dir * sp); moved = sp; } else Go(State.Circle);
                    break;
                case State.Circle:
                    {
                        Vector3 side = Vector3.Cross(Vector3.up, dir) * circleDir; float want = reach + 0.4f + (index % 2) * 0.7f;
                        Vector3 v = side * (sp * 0.55f) + dir * Mathf.Clamp(dist - want, -1f, 1f) * sp; Move(v); moved = v.magnitude;
                        if (dist > reach + 2.2f) Go(State.Approach);
                        else if (Time.time >= nextTry && dist <= reach + 0.6f && fight.TakeToken(this)) { Go(State.Telegraph); GameSfx.At("sfx_cloth", transform.position + Vector3.up, 0.5f); Trig(type == BrawlType.Knife ? "Thrust" : type == BrawlType.Heavy || type == BrawlType.Boss ? "Combo" : "Slash"); }
                        if (t > 2.5f) { circleDir = -circleDir; t = 0f; }
                    }
                    break;
                case State.Telegraph:
                    Glow(Mathf.Clamp01(t / telegraph) * (unblockable ? 2.2f : 1.2f));
                    if (dist > reach * 0.8f) Move(dir * sp * 0.6f);
                    if (t >= telegraph / (Phase > 1 ? 1.2f : 1f)) { Glow(0f); Go(State.Strike); GameSfx.At(type == BrawlType.Heavy || type == BrawlType.Boss ? "sfx_swish_wide" : "sfx_swish_quick", transform.position + Vector3.up * 1.2f, 0.8f); fight.EnemyStrikes(this, dist <= reach + 0.25f); }
                    break;
                case State.Strike:
                    if (t >= 0.25f) Go(State.Recover);
                    break;
                case State.Recover:
                    if (type == BrawlType.Knife) Move(-dir * sp * 0.8f);
                    if (t >= (type == BrawlType.Heavy ? 1.1f : 0.6f)) { fight.ReturnToken(this); nextTry = Time.time + UnityEngine.Random.Range(1.0f, 2.2f) / (Phase > 1 ? 1.4f : 1f); Go(State.Circle); }
                    break;
                case State.Staggered:
                    if (t >= 1.3f) Go(State.Circle);
                    break;
            }
            Num("Speed", moved); Flag("IsMoving", moved > 0.3f);
            if (moved > 0.4f) { stepT -= Time.deltaTime * moved; if (stepT <= 0f) { stepT = 1.15f; GameSfx.Footstep(transform.position, type == BrawlType.Heavy ? 0.55f : 0.4f); } }
        }

        void Move(Vector3 v)
        {
            Vector3 p = transform.position + v * Time.deltaTime;
            foreach (var o in fight.Enemies) if (o != this && o.Alive) { Vector3 d = p - o.transform.position; d.y = 0f; if (d.magnitude < 1.1f && d.sqrMagnitude > 1e-4f) p += d.normalized * (1.1f - d.magnitude) * 0.5f; }
            Vector3 fromCentre = p - fight.Centre; fromCentre.y = 0f; if (fromCentre.magnitude > fight.Radius) p = fight.Centre + fromCentre.normalized * fight.Radius + Vector3.up * (p.y - fight.Centre.y);
            if (Physics.Raycast(p + Vector3.up * 1.5f, Vector3.down, out RaycastHit h, 6f, ~0, QueryTriggerInteraction.Ignore) && h.collider.GetComponentInParent<BrawlEnemy>() == null && h.collider.GetComponentInParent<PlayerController>() == null) p.y = h.point.y;
            transform.position = p;
        }

        /// <summary>Take a hit. Returns true if this blow put the enemy down.</summary>
        public bool Hurt(float amount, bool stagger, Vector3 from)
        {
            if (Now == State.Down) return false;
            if (Guarding && !stagger && Phase < 3) { Health -= amount * 0.15f; Trig("Block"); GameSfx.At(type == BrawlType.Knife ? "sfx_blade_block" : "sfx_block", transform.position + Vector3.up * 1.2f, 0.8f); if (Health > 0f) return false; }   // a robber who has just been hit three times guards: only a heavy blow or a finisher gets through
            if (Time.time - recentAt > 2.5f) recent = 0; recent++; recentAt = Time.time;
            if (recent >= 3 && !stagger) { recent = 0; guardUntil = Time.time + 1.2f; }
            Health -= amount; Glow(0f); fight.ReturnToken(this);
            if (type == BrawlType.Boss && Phase == 1 && Health <= maxHealth * 0.6f) { Phase = 2; Debug.Log($"[Brawl] {name} enters phase 2"); }
            if (type == BrawlType.Boss && Phase == 2 && Health <= maxHealth * 0.3f && bossThirdPhase)
            {   // phase 3: the weapon is gone; bare-handed and desperate: faster, shorter warnings, never guards
                Phase = 3; telegraph *= 0.65f; damage *= 1.25f; speed *= 1.2f; reach = 1.9f; guardUntil = 0f; Trig("Hit");
                Debug.Log($"[Brawl] {name} enters phase 3"); fight.OnBossPhase?.Invoke(this, 3);
            }
            if (Health <= 0f) { Go(State.Down); Flag("Down", true); GameSfx.At("sfx_body_fall", transform.position, 1f); GameSfx.At(name == "Bhaskar" ? "sfx_grunt_bhaskar" : "sfx_grunt_pain", transform.position + Vector3.up * 1.6f, 0.9f, 0.1f, 32f, 0.85f); foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false; return true; }
            Trig("Hit"); GameSfx.At(name == "Bhaskar" ? "sfx_grunt_bhaskar" : "sfx_grunt_pain", transform.position + Vector3.up * 1.6f, 0.6f, 0.15f, 28f, type == BrawlType.Heavy ? 0.85f : type == BrawlType.Knife ? 1.12f : 1f);
            Vector3 push = transform.position - from; push.y = 0f; Move(push.normalized * (stagger ? 14f : 6f));
            if (stagger || Now == State.Telegraph) { Go(State.Staggered); GameSfx.At("sfx_stagger", transform.position + Vector3.up, 0.7f); }
            return false;
        }
        public void Stagger() { if (Now != State.Down) { Glow(0f); fight.ReturnToken(this); Trig("Hit"); Go(State.Staggered); } }
        public bool CanBeFinished => Now != State.Down && ((Now == State.Staggered && Health <= maxHealth * 0.35f) || Health <= maxHealth * 0.12f);
    }

    /// <summary>
    /// A V3 fight: short and heavy. The investigator has light attacks (three-hit combo), a heavy attack, a dodge, a block
    /// with a perfect-parry window and a finisher on a staggered or nearly beaten robber. At most two robbers attack at once.
    /// A defeat restarts the fight on the spot with no loading. Keys: left mouse light, R heavy, V dodge, Q block, F finisher.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class Brawl : MonoBehaviour
    {
        public static Brawl Active { get; private set; }
        public string zoneId = "zone", fightId = "zone"; public Vector3 Centre; public float Radius = 9f;
        public readonly List<BrawlEnemy> Enemies = new List<BrawlEnemy>();
        public float Health { get; private set; } = 100f;
        public float Stamina { get; private set; } = 100f;
        /// <summary>Demo menu hook: refill the player's health and stamina in the current fight.</summary>
        public static void DemoRestorePlayer() { if (Active != null) { Active.Health = 100f; Active.Stamina = 100f; } }
        public bool Won { get; private set; }
        public int Retries { get; private set; }
        public int MaxCombo { get; private set; }
        public int Parries { get; private set; }
        public int Finishers { get; private set; }
        public float DamageTaken { get; private set; }
        public float Seconds { get; private set; }
        public event Action<Brawl> OnWon;
        public Action<BrawlEnemy, int> OnBossPhase;
        public const int MaxAttackers = 2;

        PlayerController player; Animator heroAnim; readonly HashSet<string> heroParams = new HashSet<string>(); CharacterController cc;
        readonly List<BrawlEnemy> tokens = new List<BrawlEnemy>(); readonly List<(Vector3 pos, Quaternion rot)> starts = new List<(Vector3, Quaternion)>();
        Vector3 playerStart; Quaternion playerStartRot;
        float busyUntil, dodgeUntil, blockStart = -10f, lastHitTime = -10f, strikeAt = -1f; bool strikeHeavy, blocking; int chain, combo; Vector3 dodgeDir;
        float shake; Camera cam; float baseFov = -1f, punch;

        public struct Spec { public string prefab; public BrawlType type; public float scale; public Color tint; public string name; public bool threePhases; }
        public static Spec Robber(string name, int model, BrawlType type, float scale, Color tint) => new Spec { prefab = model == 2 ? "NPC/Robber2_Cine" : "NPC/Robber1_Cine", type = type, scale = scale, tint = tint, name = name };

        /// <summary>Start a fight around a point. Enemies are the existing robber models, resized and tinted.</summary>
        public static Brawl Begin(string zone, Vector3 centre, float radius, IList<Spec> specs, Action<Brawl> won, string fightId = null)
        {
            if (Active != null) return Active;
            var b = new GameObject("Brawl_" + zone).AddComponent<Brawl>(); b.zoneId = zone; b.fightId = fightId ?? zone; b.Centre = centre; b.Radius = radius; if (won != null) b.OnWon += won;
            b.player = ZoneKit.Player; if (b.player == null) { Destroy(b.gameObject); return null; }
            b.cc = b.player.GetComponent<CharacterController>(); b.heroAnim = b.player.GetComponentInChildren<Animator>();
            if (b.heroAnim != null) foreach (var p in b.heroAnim.parameters) b.heroParams.Add(p.name);
            b.playerStart = b.player.transform.position; b.playerStartRot = b.player.transform.rotation;
            float diff = GameOptions.DamageTaken;
            for (int i = 0; i < specs.Count; i++)
            {
                var pf = Resources.Load<GameObject>(specs[i].prefab); if (pf == null) { Debug.LogWarning("[Brawl] missing " + specs[i].prefab); continue; }
                float a = (i / (float)Mathf.Max(1, specs.Count)) * Mathf.PI * 2f + 0.6f; Vector3 pos = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Mathf.Min(radius - 1f, 5.5f);
                if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out RaycastHit h, 10f, ~0, QueryTriggerInteraction.Ignore)) pos.y = h.point.y;
                var g = Instantiate(pf, pos, Quaternion.LookRotation(centre - pos)); g.name = specs[i].name; g.transform.localScale *= specs[i].scale;
                foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) if (!(mb is BrawlEnemy)) mb.enabled = false;   // cinematic helpers off
                foreach (var cdr in g.GetComponentsInChildren<Collider>()) Destroy(cdr);
                var mpb = new MaterialPropertyBlock(); foreach (var r in g.GetComponentsInChildren<Renderer>()) { r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", specs[i].tint); r.SetPropertyBlock(mpb); }
                var e = g.AddComponent<BrawlEnemy>(); e.fight = b; e.bossThirdPhase = specs[i].threePhases; e.Setup(specs[i].type, diff); b.Enemies.Add(e); b.starts.Add((pos, g.transform.rotation));
            }
            Active = b; Checkpoints.Save(zone, "before_fight", b.playerStart, b.player.transform.eulerAngles.y, DawnClock.Instance != null ? DawnClock.Instance.Minutes * 60f : 0f);
            ObjectiveBanner.Show("FIGHT", specs.Count == 1 ? specs[0].name : specs.Count + " robbers", 2.5f);
            Debug.Log($"[Brawl] {zone}: fight begins against {b.Enemies.Count}");
            return b;
        }

        internal bool TakeToken(BrawlEnemy e) { if (tokens.Contains(e)) return true; if (tokens.Count >= MaxAttackers) return false; tokens.Add(e); return true; }
        internal void ReturnToken(BrawlEnemy e) { tokens.Remove(e); }
        public int Attacking => tokens.Count;
        public int Standing { get { int n = 0; foreach (var e in Enemies) if (e.Alive) n++; return n; } }
        bool Dodging => Time.time < dodgeUntil;
        void Hero(string p) { if (heroAnim != null && heroParams.Contains(p)) heroAnim.SetTrigger(p); }
        void HeroFlag(string p, bool v) { if (heroAnim != null && heroParams.Contains(p) && heroAnim.GetBool(p) != v) heroAnim.SetBool(p, v); }

        void Update()
        {
            if (Won || player == null) return;
            Seconds += Time.deltaTime; ScoreSystem.AddTime(zoneId, Time.deltaTime);
            Stamina = Mathf.Min(100f, Stamina + (blocking ? 4f : 16f) * Time.deltaTime);
            ReadInput();
            if (Dodging && cc != null && cc.enabled) cc.Move(dodgeDir * 9f * Time.deltaTime);
            if (strikeAt > 0f && Time.time >= strikeAt) { strikeAt = -1f; Land(strikeHeavy); }
            if (Time.time - lastHitTime > 2.2f) combo = 0;
            for (int i = 0; i < Enemies.Count; i++) Enemies[i].Tick(player.transform, i, Enemies.Count);
            if (Standing == 0) Win();
            ScreenText.Panel("v3_brawl", null,
                $"<color={ScreenText.Coral}><b>HEALTH</b></color> {Bar(Health / 100f)}   <color={ScreenText.Green}><b>STAMINA</b></color> {Bar(Stamina / 100f)}" + (combo > 1 ? $"   <color={ScreenText.Gold}><b>{combo} HIT COMBO</b></color>" : "") +
                $"\n{ScreenText.Key("Mouse")} Strike  {ScreenText.Key("R")} Heavy  {ScreenText.Key("V")} Dodge  {ScreenText.Key("Q")} Block  {ScreenText.Key("F")} Finish", PanelAnchor.BottomCenter, 900f, null, false, 5);
        }
        static string Bar(float k) { int n = Mathf.Clamp(Mathf.RoundToInt(k * 12f), 0, 12); return "<mspace=0.5em>" + new string('█', n) + "<color=#555555>" + new string('█', 12 - n) + "</color></mspace>"; }

        void ReadInput()
        {
            if (InputGate.GameplayBlocked) return;
            var kb = Keyboard.current; var mouse = Mouse.current; var gp = Gamepad.current;
            bool light = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (gp != null && gp.buttonWest.wasPressedThisFrame) || VirtualInput.Down("light");
            bool heavy = (kb != null && kb.rKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame) || VirtualInput.Down("heavy");
            bool dodge = (kb != null && kb.vKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame) || VirtualInput.Down("dodge");
            bool finish = (kb != null && kb.fKey.wasPressedThisFrame) || (gp != null && gp.rightShoulder.wasPressedThisFrame) || VirtualInput.Down("finisher");
            bool block = (kb != null && kb.qKey.isPressed) || (gp != null && gp.leftShoulder.isPressed) || VirtualInput.Block;
            if (block && !blocking) { blockStart = Time.time; GameSfx.At("sfx_cloth", player.transform.position + Vector3.up, 0.35f); }
            blocking = block && Stamina > 1f && Time.time >= busyUntil; HeroFlag("Block", blocking);
            if (Time.time < busyUntil) return;
            if (finish) { var e = Nearest(2.8f, 100f, true); if (e != null) { StartCoroutine(Finisher(e)); return; } }
            if (dodge && Stamina >= 25f) { Stamina -= 25f; Vector3 f = player.transform.forward; Vector2 mv = VirtualInput.Move; if (kb != null) { if (kb.sKey.isPressed) mv.y -= 1f; if (kb.aKey.isPressed) mv.x -= 1f; if (kb.dKey.isPressed) mv.x += 1f; }
                dodgeDir = mv.sqrMagnitude > 0.1f && cam != null ? (Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized * mv.x + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * mv.y).normalized : -f;
                dodgeUntil = Time.time + 0.34f; busyUntil = Time.time + 0.42f; Hero("Roll"); GameSfx.At("sfx_dodge", player.transform.position, 0.8f); GameSfx.At("sfx_cloth", player.transform.position + Vector3.up, 0.5f); return; }
            if (heavy && Stamina >= 20f) { Stamina -= 20f; Swing(true); }
            else if (light) Swing(false);
        }
        void Swing(bool heavy)
        {
            var e = Nearest(3.2f, 90f, false); if (e != null) player.FaceTowards(e.transform.position);
            Hero(heavy ? "Heavy" : "Attack" + (1 + (chain++ % 3)));
            GameSfx.At(heavy ? "sfx_swish_wide" : "sfx_swish_quick", player.transform.position + Vector3.up * 1.2f, heavy ? 0.9f : 0.7f, 0.08f);
            strikeHeavy = heavy; strikeAt = Time.time + (heavy ? 0.42f : 0.16f); busyUntil = Time.time + (heavy ? 0.8f : 0.34f);
        }
        void Land(bool heavy)
        {
            var e = Nearest(heavy ? 2.9f : 2.5f, 75f, false); if (e == null) { chain = 0; return; }
            combo++; lastHitTime = Time.time; if (combo > MaxCombo) { MaxCombo = combo; ScoreSystem.Combo(zoneId, MaxCombo); }
            bool third = !heavy && combo % 3 == 0; float dmg = heavy ? 22f : third ? 12f : 8f;
            bool down = e.Hurt(dmg, heavy || third, player.transform.position);
            Vector3 hp = e.transform.position + Vector3.up * 1.2f;
            Fx.Burst("Spark", hp, heavy ? 22 : 10, 3.5f, 0.07f, 0.4f, new Color(1f, 0.8f, 0.45f, 1f), 1.2f); Fx.Burst("Dust", hp, 6, 1.2f, 0.3f, 0.8f, new Color(0.7f, 0.65f, 0.6f, 0.5f), 0.2f);
            GameSfx.At(heavy || third ? "sfx_hit_heavy" : "sfx_hit_light", hp, heavy ? 1f : 0.85f, 0.1f);
            StartCoroutine(HitStop(heavy ? 0.09f : 0.045f)); shake = Mathf.Max(shake, heavy ? 0.22f : 0.1f);
            if (down) Debug.Log($"[Brawl] {e.name} is down ({Standing} left)");
        }

        IEnumerator HitStop(float seconds) { float keep = Time.timeScale; Time.timeScale = 0.05f; yield return new WaitForSecondsRealtime(seconds); if (Time.timeScale < 0.5f) Time.timeScale = 1f; }

        IEnumerator Finisher(BrawlEnemy e)
        {
            if (!e.CanBeFinished) yield break;
            busyUntil = Time.time + 1f; player.FaceTowards(e.transform.position); Hero("Heavy"); Finishers++;
            Time.timeScale = 0.3f; punch = 1f;
            yield return new WaitForSecondsRealtime(0.45f);
            e.Hurt(9999f, true, player.transform.position); Fx.Burst("Spark", e.transform.position + Vector3.up * 1.3f, 30, 4f, 0.08f, 0.5f, new Color(1f, 0.85f, 0.5f, 1f), 1.2f); GameSfx.At("sfx_hit_heavy", e.transform.position + Vector3.up * 1.3f, 1f, 0.02f, 40f, 0.85f); shake = 0.3f;
            yield return new WaitForSecondsRealtime(0.45f);
            Time.timeScale = 1f; combo++; lastHitTime = Time.time; if (combo > MaxCombo) { MaxCombo = combo; ScoreSystem.Combo(zoneId, MaxCombo); }
            Debug.Log($"[Brawl] finisher on {e.name} ({Standing} left)");
        }

        BrawlEnemy Nearest(float range, float cone, bool finishable)
        {
            BrawlEnemy best = null; float bd = float.MaxValue;
            foreach (var e in Enemies)
            {
                if (!e.Alive || (finishable && !e.CanBeFinished)) continue;
                Vector3 to = e.transform.position - player.transform.position; to.y = 0f; float d = to.magnitude;
                if (d > range || (d > 0.8f && Vector3.Angle(player.transform.forward, to) > cone)) continue;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        /// <summary>An enemy's blow lands now (called by the enemy at the end of its telegraph).</summary>
        internal void EnemyStrikes(BrawlEnemy e, bool inReach)
        {
            if (Won || !inReach || Dodging) return;
            bool facing = Vector3.Angle(player.transform.forward, e.transform.position - player.transform.position) < 100f;
            if (blocking && facing && !e.unblockable)
            {
                if (Time.time - blockStart <= GameOptions.CounterWindow) { Parries++; e.Stagger(); Fx.Burst("Spark", player.transform.position + Vector3.up * 1.3f + player.transform.forward * 0.5f, 18, 3f, 0.06f, 0.35f, new Color(0.8f, 0.9f, 1f, 1f), 1f); GameSfx.At("sfx_blade_block", player.transform.position + Vector3.up * 1.3f, 1f, 0.05f); GameSfx.At("sfx_stagger", e.transform.position + Vector3.up, 0.7f); StartCoroutine(HitStop(0.07f)); return; }
                Stamina = Mathf.Max(0f, Stamina - 18f); GameSfx.At("sfx_block", player.transform.position + Vector3.up * 1.2f, 0.9f); Take(e.damage * 0.2f, e); return;
            }
            Take(e.damage, e);
        }
        void Take(float dmg, BrawlEnemy from)
        {
            Health -= dmg; DamageTaken += dmg; ScoreSystem.AddDamage(zoneId, dmg); combo = 0; chain = 0; Hero("Hit"); shake = Mathf.Max(shake, 0.16f);
            GameSfx.At("sfx_hit_taken", player.transform.position + Vector3.up * 1.2f, 0.9f); GameSfx.At("sfx_grunt_aryan", player.transform.position + Vector3.up * 1.6f, 0.75f, 0.08f);
            if (Health <= 0f) StartCoroutine(Retry());
        }

        /// <summary>Defeat: no loading screen. Everyone back to where the fight began, full health, and it starts again.</summary>
        IEnumerator Retry()
        {
            Retries++; busyUntil = Time.time + 1.4f; tokens.Clear(); Time.timeScale = 1f;
            ObjectiveBanner.Show("KNOCKED DOWN", "Get up. Try again.", 1.6f);
            yield return new WaitForSecondsRealtime(1.2f);
            player.Teleport(playerStart, playerStartRot); Health = 100f; Stamina = 100f; combo = 0;
            for (int i = 0; i < Enemies.Count; i++) { var e = Enemies[i]; e.transform.SetPositionAndRotation(starts[i].pos, starts[i].rot); var an = e.GetComponentInChildren<Animator>(); if (an != null) { foreach (var p in an.parameters) if (p.name == "Down") an.SetBool("Down", false); } e.Setup(e.type, GameOptions.DamageTaken); }
            Debug.Log($"[Brawl] {zoneId}: retry {Retries}");
        }

        void Win()
        {
            Won = true; Time.timeScale = 1f; HeroFlag("Block", false); PuzzleSave.Set("fight." + fightId, 1);
            ScoreSystem.FinishZone(zoneId); if (Active == this) Active = null;
            ObjectiveBanner.Show("THE ROBBERS ARE DOWN", "", 2.5f); AudioManager.Instance?.PlaySFX("sfx_puzzle_success", 0.8f);
            Debug.Log($"[Brawl] {zoneId}: won in {Seconds:0.0}s, damage taken {DamageTaken:0}, max combo {MaxCombo}, parries {Parries}, finishers {Finishers}, retries {Retries}");
            OnWon?.Invoke(this);
            StartCoroutine(Clear());
        }
        IEnumerator Clear() { yield return new WaitForSeconds(12f); foreach (var e in Enemies) if (e != null) Destroy(e.gameObject); Destroy(gameObject); }
        void OnDestroy() { if (Active == this) Active = null; if (cam != null && baseFov > 0f) cam.fieldOfView = baseFov; }

        void LateUpdate()
        {   // limited camera shake and the finisher's punch-in, added after the follow camera has moved
            if (cam == null) { cam = Camera.main; if (cam != null) baseFov = cam.fieldOfView; }
            if (cam == null) return;
            if (shake > 0.001f) { cam.transform.position += UnityEngine.Random.insideUnitSphere * Mathf.Min(shake, 0.3f) * 0.35f; shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 1.4f); }
            punch = Mathf.MoveTowards(punch, 0f, Time.unscaledDeltaTime * 1.6f); cam.fieldOfView = baseFov - 12f * Mathf.SmoothStep(0f, 1f, punch);
        }

        public static bool WonBefore(string zone) => PuzzleSave.Get("fight." + zone) == 1;
    }
}
