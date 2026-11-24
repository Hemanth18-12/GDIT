# Zone 4 QA — Stepwell and Monsoon Shrine

- **Zone:** Stepwell and Monsoon Shrine (`new_stepwell`)
- **Status:** PASSED (Verified via `NewZonePlayBot.StepwellRun` in `NewBazaarPlayTests.cs`)
- **Evidence:** `Docs/QA/zones/zone_4/editor-round1/run.md`
- **Screenshots (3):**
  1. `Docs/QA/zones/zone_4/editor-round1/01-stepwell-rim.png` (Descent along multi-tier stepwell stone rim at night under heavy monsoon rain)
  2. `Docs/QA/zones/zone_4/editor-round1/02-rising-raft-flooded.png` (Three sluice valves opened, basin flooded, floating wooden raft elevated to form crossing bridge)
  3. `Docs/QA/zones/zone_4/editor-round1/03-monsoon-vessel.png` (Knife smuggler and lookout defeated on wet-stone terrace, Monsoon Ceremonial Vessel and Temple Key Ring recovered)
- **Gameplay Checklist:**
  - **Atmosphere & Environment:** Heavy monsoon rainfall, night lighting volume, wet reflective stone shaders, multi-tier sandstone steps descending 8 metres into the tank excavation.
  - **Sluice Valve Sequence:** Three manual stone sluice valves (`el.nz4.valve.0`, `el.nz4.valve.1`, `el.nz4.valve.2`) turned along the south rim, channelling rainwater runoff into the basin.
  - **Rising Water & Floating Bridge:** Dynamic water basin rises to maximum flood stage (level 3), elevating the heavy wooden raft to create a physical bridge crossing to the high northern terrace.
  - **Combat:** Wet-stone terrace combat against the Knife Smuggler and Stepwell Lookout (2 finishers, parries, dodges).
  - **Item Recovery:** Recovered Treasure 4: Monsoon Ceremonial Vessel + Temple Key Ring (evidence confirming the master keys used in the theft).
- **Performance Sample:** 58.0 fps mean, 119.0 ms maximum in Unity Editor. Score: 3315 (Grade A).
- **Console Errors:** 0 errors.
- **Heritage Content:** Traditional Odisha stepwell (Bawdi) water management, rainwater harvesting architecture, and ceremonial monsoon vessels documented in journal.
