# Zone 1 QA — Village Bazaar and Tea Lane

- **Zone:** Village Bazaar and Tea Lane (`new_bazaar`)
- **Status:** PASSED (Verified via `NewZonePlayBot.BazaarRun` in `NewBazaarPlayTests.cs`)
- **Evidence:** `Docs/QA/zones/zone_1/editor-round1/run.md`
- **Screenshots (3):**
  1. `Docs/QA/zones/zone_1/editor-round1/01-market-arrival.png` (Arrival by Jeep on two-bend laterite road)
  2. `Docs/QA/zones/zone_1/editor-round1/02-dyers-shortcut.png` (Dyers' lane shortcut & sound-led tailing)
  3. `Docs/QA/zones/zone_1/editor-round1/03-cart-recovery.png` (Physical sequence lock opened, robber defeated, Dawn Bell recovered)
- **Gameplay Checklist:**
  - Traversal: Two-bend approach road driven with Jeep WheelColliders, parked and safely exited on foot without collision glitches.
  - Tailing: Sound-led courier tailing across 3 looping lanes; courier dropped red cloth bag-tie.
  - Physical Puzzle: Dropped cloth strip picked up via `E` input, fitted into cart latch socket (`el.nz1.seal`); 3-peg sequence lock solved with market receipt order (CUP, BELL, THREAD) with soft-reset verified.
  - Combat: Full street fight won using Brawl system (light combo, dodge, block/parry, finisher).
  - Item Recovery: Stolen Dawn Bell recovered on defeat. Inside-man list added to evidence board.
- **Performance Sample:** 58.8 fps mean, 80.3 ms maximum in Unity Editor.
- **Console Errors:** 0 errors.
- **Heritage Content:** UNESCO-recognized bell metal craft & brass vessel traditions of Kantilo/Odisha documented in journal.
