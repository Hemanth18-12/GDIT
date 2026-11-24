using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Characters;
using LegacyOfTemples.World;
using LegacyOfTemples.Gameplay.Puzzles;
using LegacyOfTemples.Gameplay.Vehicles;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>Replacement Zone 1: a new T market, three looping lanes, a sound-led tail and physical cart lock.</summary>
    public sealed class NewBazaar : MonoBehaviour
    {
        public const string Zone = "new_bazaar";
        public static NewBazaar Instance { get; private set; }
        public enum Stage { Observe, Tail, LookBack, Cart, Fight, Recover, Complete }
        public Stage Now { get; private set; }
        public bool Built { get; private set; }
        public Transform Courier { get; private set; }
        public Carryable Thread { get; private set; }
        public PlacementSocket Seal { get; private set; }
        public SequenceLock CartLock { get; private set; }
        public SequenceKey[] Keys { get; private set; }
        public TreasurePickup Bell { get; private set; }
        public PuzzleGroup Group { get; private set; }
        public readonly List<Vector3> Route = new List<Vector3>();
        public readonly List<Transform> Crowd = new List<Transform>();
        public Vector3 Yard => new Vector3(32f, 0f, 42f);
        public int Spotted { get; private set; }
        public int Leg { get; private set; }
        public float Seconds { get; private set; }
        public bool RouteFinished => PuzzleSave.Get("nz1.tail") == 1;
        AudioSource bagRing;
        Animator courierAnimator;
        bool hasSpeed, checkpoint;
        float turnTime, bellTime, crowdTime;
        GameObject cartGate;
        readonly List<Light> lanterns = new List<Light>();
        Stage shownStage = (Stage)(-1);
        float dusk;
        public AudioClip RingClip => bagRing != null ? bagRing.clip : null;
        static readonly int[] LookStops = { 1, 3, 5 };
        void Awake() { Instance = this; Build(); }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Build()
        {
            var r = transform;
            // Continuous walkable ground surrounding excavated Stepwell (-67..-43, z 109..141) and spanning south -> north jungle (z -140..520)
            MarketArt.Box(r, "Walkable laterite south", new Vector3(0f, -0.6f, -16f), new Vector3(340f, 1.2f, 248f), "red_laterite_soil_stones");
            MarketArt.Box(r, "Walkable laterite north jungle", new Vector3(0f, -0.6f, 331f), new Vector3(340f, 1.2f, 378f), "red_laterite_soil_stones");
            MarketArt.Box(r, "Walkable laterite west", new Vector3(-118.5f, -0.6f, 125f), new Vector3(103f, 1.2f, 34f), "red_laterite_soil_stones");
            MarketArt.Box(r, "Walkable laterite mid", new Vector3(-30.5f, -0.6f, 125f), new Vector3(25f, 1.2f, 34f), "red_laterite_soil_stones");
            MarketArt.Box(r, "Walkable laterite east", new Vector3(94f, -0.6f, 125f), new Vector3(152f, 1.2f, 34f), "red_laterite_soil_stones");
            MarketArt.Road(r, new[] { new Vector3(-55f, 0f, -100f), new Vector3(-55f, 0f, -64f), new Vector3(0f, 0f, -48f), new Vector3(0f, 0f, 0f) }, 9f);
            MarketArt.Road(r, new[] { new Vector3(-40f, 0f, 0f), new Vector3(40f, 0f, 0f) }, 11f);
            MarketArt.Road(r, new[] { new Vector3(0f, 0f, -12f), new Vector3(0f, 0f, 58f) }, 8f);
            MarketArt.Road(r, new[] { new Vector3(-23f, 0f, 0f), new Vector3(-23f, 0f, 30f), new Vector3(32f, 0f, 30f), new Vector3(32f, 0f, 0f) }, 5f);
            MarketArt.Road(r, new[] { new Vector3(-23f, 0f, 16f), new Vector3(32f, 0f, 16f) }, 4f);
            MarketArt.Road(r, new[] { new Vector3(-34f, 0f, 48f), new Vector3(45f, 0f, 48f), new Vector3(45f, 0f, 0f) }, 4f);
            Color ochre = new Color(0.88f, 0.65f, 0.38f), indigo = new Color(0.43f, 0.53f, 0.65f), lime = new Color(0.82f, 0.8f, 0.62f);
            for (int i = 0; i < 4; i++)
            {
                float x = -30f + i * 20f;
                MarketArt.House(r, new Vector3(x, 0f, -13f), 180f, i % 2 == 0 ? ochre : lime, new[] { "Tea house", "Grain merchant", "Brass workshop", "Bicycle repair" }[i], i);
                MarketArt.House(r, new Vector3(x, 0f, 10f), 0f, i % 2 == 0 ? indigo : ochre, new[] { "Indigo dyer", "Clay & kiln", "Bell maker", "Spice trader" }[i], i + 1);
            }
            foreach (float x in new[] { -14f, 14f })
                for (int i = 0; i < 2; i++) MarketArt.House(r, new Vector3(x, 0f, 23f + i * 20f), x < 0f ? 270f : 90f, i == 0 ? ochre : lime, i == 0 ? "Tea lane rooms" : "Weaver's home", i);
            for (int i = 0; i < 8; i++)
            {
                ArtUpgrade.Place(r, "oak_tree", new Vector3(i % 2 == 0 ? -48f : 52f, 0f, -35f + i * 12f), 0.9f, i * 43f);
                ArtUpgrade.Place(r, "fern_02", new Vector3(i % 2 == 0 ? -39f : 41f, 0f, -18f + i * 8f), 1.1f, i * 17f, false);
            }
            // These taller homes screen the temple direction on the two-bend arrival road.
            MarketArt.House(r, new Vector3(-10f, 0f, -36f), 90f, indigo, "Village guest house", 0);
            MarketArt.House(r, new Vector3(11f, 0f, -25f), 270f, ochre, "Festival store", 1);
            MarketArt.Box(r, "Low tea stall", new Vector3(-5.3f, 0.55f, -1f), new Vector3(1.2f, 1.1f, 3.4f), "weathered_planks");
            ArtUpgrade.Place(r, "brass_pot_01", new Vector3(-5.3f, 1.1f, -1.5f), 1.6f, 0f, false);
            // Physical market clues: receipts numbered in Roman strokes; the cart uses the same symbols.
            MarketArt.Sign(r, new Vector3(-30f, 1.8f, -6.9f), "I  /  CUP", 3f, 180f);
            MarketArt.Sign(r, new Vector3(10f, 1.8f, -6.9f), "II  /  BELL", 3f, 180f);
            MarketArt.Sign(r, new Vector3(-30f, 1.8f, 5.9f), "III  /  THREAD", 3f);
            MarketArt.Sign(r, new Vector3(-5.2f, 2.1f, -0.8f), "TEA LANE", 3f, 90f);
            // A bell/clock post is the central landmark, never a quest marker.
            MarketArt.Box(r, "Clock post", new Vector3(-6f, 3.7f, 5f), new Vector3(0.3f, 7.4f, 0.3f), "weathered_planks");
            var clock = ZoneKit.Round(r, "Clock dial", new Vector3(-6f, 6f, 4.85f), new Vector3(1.5f, 0.09f, 1.5f), ZoneKit.Tinted("clay_plaster", new Color(0.82f, 0.72f, 0.5f), 0.2f)); clock.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            MarketArt.Sign(r, new Vector3(-6f, 6f, 4.65f), "V  :  XLV", 1.3f);
            for (int i = 0; i < 16; i++)
            {
                Vector3 p = new Vector3(i % 2 == 0 ? -5.2f : 5.2f, 0f, 5f + i * 3f);
                if (i % 3 == 0) ArtUpgrade.Place(r, "planter_box_01", p, 1.1f, i * 19f);
                MarketArt.Box(r, "Tea lane lamp post", p + Vector3.up * 1.45f, new Vector3(0.14f, 2.9f, 0.14f), "medieval_wood", false);
                var l = new GameObject("Tea lane lantern").AddComponent<Light>(); l.transform.SetParent(r, false); l.transform.localPosition = p + Vector3.up * 3f;
                l.type = LightType.Point; l.range = 8f; l.color = new Color(1f, 0.66f, 0.3f); l.intensity = 1.2f; lanterns.Add(l);
                ArtUpgrade.Place(r, "wooden_lantern_01", l.transform.position - Vector3.up * 0.18f, 0.8f, 0f, false);
            }
            // Shared yard has two exits: the tea lane and east loop. No sealed dead end after the fight.
            MarketArt.Box(r, "Yard wall north", new Vector3(32f, 1.7f, 51f), new Vector3(22f, 3.4f, 0.4f), "yellow_plaster");
            MarketArt.Box(r, "Yard wall east", new Vector3(43f, 1.7f, 44f), new Vector3(0.4f, 3.4f, 14f), "yellow_plaster");
            for (int i = 0; i < 4; i++)
            {
                var cloth = ZoneKit.Part(r, "Indigo cloth drying", new Vector3(23f + i * 4.8f, 2.2f, 49.6f), new Vector3(3.2f, 2.5f, 0.04f), ZoneKit.Tinted("clay_plaster", i % 2 == 0 ? indigo : new Color(0.72f, 0.25f, 0.12f), 0.1f), false);
                cloth.transform.localRotation = Quaternion.Euler(0f, i % 2 == 0 ? 6f : -5f, 0f);
                ArtUpgrade.Place(r, "Barrel_01", new Vector3(23f + i * 5f, 0f, 49f), 1.1f, 23f * i);
            }
            BuildCart(); BuildCourier(); BuildCrowd();
            var look = new GameObject("Bazaar afternoon volume").AddComponent<ZoneLook>(); look.transform.SetParent(r, false); look.transform.localPosition = new Vector3(0f, 8f, 0f); look.zone = ZoneLook.Zone.Bazaar; look.size = new Vector3(210f, 40f, 255f); look.Build(); ZoneAtmosphere.Register(look, Quaternion.Euler(30f, -65f, 0f));
            foreach (var entry in new[] { ("amb_village", 0.28f), ("amb_birds", 0.1f) })
            {
                var src = r.gameObject.AddComponent<AudioSource>(); src.clip = Resources.Load<AudioClip>("Audio/" + entry.Item1); src.volume = entry.Item2; src.loop = true; src.spatialBlend = 0f; AudioManager.Router?.Invoke(src, "Ambience"); if (src.clip != null) src.Play();
            }
            Now = Bell.Taken ? Stage.Complete : Brawl.WonBefore(Zone) ? Stage.Recover : RouteFinished ? Stage.Cart : Stage.Observe;
            Courier.gameObject.SetActive(!RouteFinished); cartGate.SetActive(CartLock.State != 1); ShowBell(Now == Stage.Recover);
            Built = true; Debug.Log("[NewBazaar] built: T market, 3 loop links, 16 villagers, tail / seal / sequence / street fight");
        }
        void BuildCourier()
        {
            Route.AddRange(new[] { new Vector3(-18f, 0f, 0f), new Vector3(-23f, 0f, 7f), new Vector3(-23f, 0f, 30f), new Vector3(0f, 0f, 30f), new Vector3(0f, 0f, 16f), new Vector3(32f, 0f, 16f), new Vector3(32f, 0f, 42f) });
            var pf = Resources.Load<GameObject>("NPC/Robber1_Cine"); var g = Instantiate(pf, Route[0], Quaternion.LookRotation(Route[1] - Route[0]), transform); g.name = "Courier with red thread bag";
            foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
            foreach (var cd in g.GetComponentsInChildren<Collider>()) Destroy(cd);
            Courier = g.transform; courierAnimator = g.GetComponentInChildren<Animator>();
            if (courierAnimator != null) foreach (var p in courierAnimator.parameters) if (p.name == "Speed") hasSpeed = true;
            var bag = ZoneKit.Part(Courier, "Bell bag", new Vector3(0.32f, 0.9f, -0.12f), new Vector3(0.35f, 0.44f, 0.22f), ZoneKit.Tinted("clay_plaster", new Color(0.38f, 0.27f, 0.15f), 0.1f), false);
            ZoneKit.Part(Courier, "Red bag thread", new Vector3(0.32f, 1.1f, -0.12f), new Vector3(0.4f, 0.08f, 0.25f), ZoneKit.Tinted("clay_plaster", new Color(0.82f, 0.06f, 0.03f), 0.1f), false);
            bagRing = bag.AddComponent<AudioSource>(); bagRing.clip = Resources.Load<AudioClip>("Audio/sfx_bell_muffled"); if (bagRing.clip == null) bagRing.clip = AudioManager.Instance?.GetClip("sfx_bell_ring"); bagRing.spatialBlend = 1f; bagRing.minDistance = 5f; bagRing.maxDistance = 45f; AudioManager.Router?.Invoke(bagRing, "SFX");
            var cloth = ZoneKit.Part(transform, "Dropped red cloth strip", new Vector3(-23f, 0.12f, 17f), new Vector3(0.5f, 0.06f, 0.8f), ZoneKit.Tinted("clay_plaster", new Color(0.8f, 0.05f, 0.03f), 0.08f), false);
            ZoneKit.Reach(cloth, 0.6f); Thread = cloth.AddComponent<Carryable>(); Thread.itemKey = "nz1.redthread"; Thread.displayName = "red bag tie";
        }
        void BuildCart()
        {
            var cartRoot = new GameObject("Cart bay beside east lane").transform; cartRoot.SetParent(transform, false); cartRoot.localPosition = Vector3.right * 5f;
            MarketArt.Box(cartRoot, "Cart bed", new Vector3(32f, 0.7f, 34.5f), new Vector3(3.8f, 0.2f, 2.2f), "weathered_planks");
            for (int i = -1; i <= 1; i += 2) { var wheel = ZoneKit.Round(cartRoot, "Cart wheel", new Vector3(32f + i * 2f, 0.5f, 34.5f), new Vector3(1.1f, 0.12f, 1.1f), ArtUpgrade.Mat("medieval_wood")); wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); }
            cartGate = MarketArt.Box(cartRoot, "Cart locked lid", new Vector3(32f, 1.35f, 34.5f), new Vector3(3.8f, 0.25f, 2.3f), "medieval_wood");
            var socket = new GameObject("Bag tie latch"); socket.transform.SetParent(cartRoot, false); socket.transform.localPosition = new Vector3(29.7f, 1.05f, 34.5f); ZoneKit.Reach(socket, 0.45f);
            Seal = socket.AddComponent<PlacementSocket>(); Seal.elementId = "nz1.seal"; Seal.acceptsKey = "nz1.redthread"; Seal.radius = 2.4f;
            MarketArt.Sign(cartRoot, new Vector3(29.6f, 1.6f, 34.2f), "BAG TIE", 1.3f);
            CartLock = new GameObject("Cart sound lock").AddComponent<SequenceLock>(); CartLock.transform.SetParent(cartRoot, false); CartLock.transform.localPosition = new Vector3(32f, 0f, 34f); CartLock.elementId = "nz1.cart"; CartLock.required.AddRange(new[] { 0, 2, 1 });
            Keys = new SequenceKey[3]; var names = new[] { "CUP", "THREAD", "BELL" };
            for (int i = 0; i < 3; i++)
            {
                var g = ZoneKit.Round(cartRoot, names[i] + " lock peg", new Vector3(31f + i, 1.05f, 33.2f), new Vector3(0.36f, 0.13f, 0.36f), ZoneKit.Tinted("clay_plaster", new Color(0.68f, 0.46f, 0.17f), 0.65f), false); ZoneKit.Reach(g, 0.4f);
                var k = g.AddComponent<SequenceKey>(); k.owner = CartLock; k.index = i; k.verb = "Press " + names[i].ToLowerInvariant() + " peg"; k.radius = 2.1f; k.enabled = false;
                k.sound = g.AddComponent<AudioSource>(); k.sound.clip = Resources.Load<AudioClip>("Audio/sfx_gear_click"); k.pitch = 0.8f + i * 0.3f; k.sound.spatialBlend = 1f; k.sound.maxDistance = 18f; Keys[i] = k;
                MarketArt.Sign(cartRoot, new Vector3(31f + i, 1.45f, 33.1f), names[i], 0.9f);
            }
            Group = new GameObject("Bazaar physical puzzle").AddComponent<PuzzleGroup>(); Group.transform.SetParent(cartRoot, false); Group.transform.localPosition = new Vector3(0f, 0f, 16f);
            Group.puzzleId = "nz1.market"; Group.zoneId = Zone; Group.hintRadius = 130f; Group.lockWhenSolved = false;
            Group.conditions.Add(new PuzzleGroup.Condition { element = Seal, state = 1 }); Group.conditions.Add(new PuzzleGroup.Condition { element = CartLock, state = 1 });
            Group.hints = new[] { "Listen for a bag ringing near the tea stalls. The back lanes connect.", "His red bag tie fits the cart latch. Shop receipts carry three numbered symbols.", "Follow through the dyer's loop. Place the dropped tie at the cart, then press CUP, BELL, THREAD." };
            var bell = ArtUpgrade.Place(cartRoot, "brass_goblets", new Vector3(37f, 0.1f, 42f), 1.2f, 0f, false);
            bell.name = "Fictional Dawn Bell — drop after fight"; ZoneKit.Reach(bell, 0.7f); Bell = bell.AddComponent<TreasurePickup>(); Bell.treasureName = "Dawn Bell (new bazaar)"; Bell.zoneId = Zone;
        }
        void BuildCrowd()
        {
            for (int i = 0; i < 16; i++)
            {
                var pf = Resources.Load<GameObject>(i % 3 == 0 ? "NPC/Villager_NPC" : "NPC/Villager_Male_NPC");
                var at = new Vector3(-33f + (i % 8) * 9f, 0f, i < 8 ? -3f : 3.2f);
                var npc = Instantiate(pf, at, Quaternion.Euler(0f, i * 137.5f, 0f), transform); npc.name = "Market villager " + (i + 1);
                foreach (var mb in npc.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
                foreach (var cd in npc.GetComponentsInChildren<Collider>()) Destroy(cd);
                if (i % 4 == 0) { var walker = npc.AddComponent<RoadWalker>(); walker.a = at; walker.b = at + Vector3.right * 4f; walker.speed = 0.65f; }
                else { var routine = npc.AddComponent<VillagerRoutine>(); routine.mode = VillagerRoutine.Mode.Talk; routine.offset = i * 0.7f; }
                Crowd.Add(npc.transform);
            }
        }
        IEnumerator Start()
        {
            yield return null;
            PlayerPrefs.SetString("LastPlayedScene", "TempleRing"); PlayerPrefs.Save();
            if (Jeep.Instance == null) Jeep.Spawn(new Vector3(-55f, 0.6f, -90f), Quaternion.identity);
            // New layout has its own saved checkpoint; do not import a checkpoint from the old courtyard.
            if (Checkpoints.Has && Checkpoints.Current.zone == Zone && ZoneKit.Player != null) Checkpoints.Restore(ZoneKit.Player.transform);
            if (Brawl.WonBefore(Zone)) { Now = Stage.Recover; ShowBell(true); }
            cartGate.SetActive(CartLock.State != 1);
            PlayerController.ObjectiveFocus = null;
            if (PuzzleSave.Get(Bell.Id) == 1) { Now = Stage.Complete; ShowBell(false); }
        }
        void ShowBell(bool show) { Bell.enabled = show; foreach (var rr in Bell.GetComponentsInChildren<Renderer>()) rr.enabled = show; }
        public bool CourierSees(Vector3 at)
        {
            Vector3 from = Courier.position + Vector3.up * 1.5f, to = at + Vector3.up * 1.2f, d = to - from;
            if (d.magnitude > 18f || Vector3.Dot(Courier.forward, d.normalized) < 0.45f) return false;
            return !Physics.Linecast(from, to, ~0, QueryTriggerInteraction.Ignore);
        }
        void Update()
        {
            var pc = ZoneKit.Player; if (!Built || pc == null || Time.timeScale <= 0f || InputGate.GameplayBlocked) return;
            if (!checkpoint) { checkpoint = true; if (!Checkpoints.Has || Checkpoints.Current.zone != Zone) Checkpoints.Save(Zone, "Banyan arrival", new Vector3(-52f, 0.05f, -90f), 0f, 0f); }
            if (shownStage != Now)
            {
                shownStage = Now;
                string[] action = { "Listen through the market", "Keep his bell in earshot", "Wait behind market cover", "Fit the tie. Read the receipts.", "Recover the courier's bell", "Take the fallen bell", "Bazaar secured" };
                EventBus.RaiseUpdateObjective(action[(int)Now], Now == Stage.Complete ? "Bell and handwritten list recovered." : "Observe the world. H offers three hints.", "Zone 1 / Village bazaar");
            }
            if (pc.transform.position.z > -6f) PuzzleSave.Set("nz1.visited", 1);
            if (Now == Stage.Complete) return;
            Seconds += Time.deltaTime; ScoreSystem.AddTime(Zone, Time.deltaTime);
            if (Now != Stage.Observe) dusk = Mathf.MoveTowards(dusk, 1f, Time.deltaTime / 90f);
            foreach (var l in lanterns) l.intensity = Mathf.Lerp(1.2f, 5f, dusk);
            float gap = Vector3.Distance(pc.transform.position, Courier.position);
            bellTime -= Time.deltaTime;
            if (bellTime <= 0f && Now < Stage.Cart) { bellTime = gap > 28f ? 1.6f : 3.6f; if (bagRing.clip != null) bagRing.Play(); }
            if (Time.time > crowdTime && Now < Stage.Cart)
            {
                crowdTime = Time.time + 0.4f;
                foreach (var person in Crowd) if (Vector3.Distance(person.position, Courier.position) < 9f) { Vector3 d = Courier.position - person.position; d.y = 0f; if (d.sqrMagnitude > 0.1f) person.rotation = Quaternion.LookRotation(d); }
            }
            bool moving = Now == Stage.Tail;
            if (courierAnimator != null && hasSpeed) courierAnimator.SetFloat("Speed", moving ? 1.7f : 0f);
            switch (Now)
            {
                case Stage.Observe:
                    if (gap < 17f) { Now = Stage.Tail; Leg = 1; VoiceEvents.Fire("bazaar_follow"); }
                    break;
                case Stage.Tail:
                    if (gap > 40f) break; // lost sight: louder bell, courier waits; no hard fail
                    Vector3 d = Route[Leg] - Courier.position; d.y = 0f;
                    if (d.magnitude < 0.18f)
                    {
                        int reached = Leg; Leg++;
                        if (Leg >= Route.Count) { Now = Stage.Cart; PuzzleSave.Set("nz1.tail", 1); Courier.gameObject.SetActive(false); Debug.Log("[NewBazaar] tail completed"); }
                        else if (System.Array.IndexOf(LookStops, reached) >= 0) { Now = Stage.LookBack; turnTime = 0f; }
                    }
                    else { Courier.position += d.normalized * Mathf.Min(1.9f * Time.deltaTime, d.magnitude); Courier.rotation = Quaternion.Slerp(Courier.rotation, Quaternion.LookRotation(d), Time.deltaTime * 7f); }
                    break;
                case Stage.LookBack:
                    turnTime += Time.deltaTime;
                    Vector3 back = Route[Mathf.Max(0, Leg - 2)] - Courier.position; back.y = 0f;
                    if (back.sqrMagnitude > 0.01f) Courier.rotation = Quaternion.Slerp(Courier.rotation, Quaternion.LookRotation(back), Time.deltaTime * 5f);
                    if (turnTime > 0.9f && turnTime < 2.3f && CourierSees(pc.transform.position)) { Spotted++; ScoreSystem.Spotted(Zone); DawnClock.Instance?.Penalty(1f, "courier suspicion"); VoiceEvents.Fire("bazaar_spotted"); turnTime = 2.3f; Debug.Log("[NewBazaar] detected; soft fail, tail continues"); }
                    if (turnTime > 2.8f) Now = Stage.Tail;
                    break;
                case Stage.Cart:
                    foreach (var key in Keys) key.enabled = Seal.State == 1;
                    if (Seal.State == 1 && CartLock.State == 1)
                    {
                        cartGate.SetActive(false); Now = Stage.Fight; StoryDialogueSystem.Instance?.Say("v3:bazaar_cornered"); GameSfx.At("sfx_wall_open", Yard, 0.7f);
                        if (Brawl.WonBefore(Zone)) Won(); else Brawl.Begin(Zone, Yard, 8f, new[] { Brawl.Robber("Courier — red scarf", 1, BrawlType.Knife, 0.96f, new Color(0.8f, 0.63f, 0.48f)), Brawl.Robber("Dyer yard lookout", 2, BrawlType.Brawler, 1f, new Color(0.65f, 0.68f, 0.8f)) }, _ => Won());
                    }
                    break;
                case Stage.Recover:
                    if (Bell.Taken) { Now = Stage.Complete; PuzzleSave.Set("evidence.list", 1); PuzzleSave.Set("nz1.complete", 1); ScoreSystem.FinishZone(Zone); Debug.Log("[NewBazaar] COMPLETE: bell + list, score=" + ScoreSystem.Points(ScoreSystem.Zone(Zone)) + " grade=" + ScoreSystem.Grade(ScoreSystem.Zone(Zone))); }
                    break;
            }
        }
        void Won() { Now = Stage.Recover; ShowBell(true); StoryDialogueSystem.Instance?.Say("done:obj_v3_bazaar"); }
    }
}
