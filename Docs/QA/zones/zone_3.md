# Zone 3 QA — Underground Storage Vaults

- **Zone:** Underground Storage Vaults (`new_vaults`)
- **Status:** PASSED (Verified via `NewZonePlayBot.VaultsRun` in `NewBazaarPlayTests.cs`)
- **Evidence:** `Docs/QA/zones/zone_3/editor-round1/run.md`
- **Screenshots (3):**
  1. `Docs/QA/zones/zone_3/editor-round1/01-vault-entrance.png` (Underground stone vault entry with iron bar security gate)
  2. `Docs/QA/zones/zone_3/editor-round1/02-gear-train-discs.png` (Assembled bronze gear train and 3-disc stone rotation mechanism aligned to ceiling light arcs 2, 5, 3)
  3. `Docs/QA/zones/zone_3/editor-round1/03-rosette-recovery.png` (Vault sentinels defeated in close-quarters brawl, Sun Rosette and key-keeper duty ledger recovered)
- **Gameplay Checklist:**
  - **Vault Structure:** Full underground stone hall excavated beneath courtyard with vaulted ceilings, arched entry stairs, and massive support pillars with brass diya lanterns.
  - **Pushable Blocks & Pressure Plates:** Two heavy sandstone blocks pushed onto corresponding floor plates (`el.nz3.plate.0`, `el.nz3.plate.1`), releasing the counterweight to open the iron security gate. Reset lever provided for fail-safe puzzle recovery.
  - **Mechanical Gear Train:** Two bronze loose gears carried and mounted onto drive pegs (`el.nz3.peg.0`, `el.nz3.peg.1`); manual crank turned to transmit rotational power to inner disc apparatus.
  - **Light-Arc Disc Alignment:** Three heavy carved stone discs aligned to match the natural light arcs entering through the ceiling stone grate (Answer = 2, 5, 3), unsealing the relic chamber.
  - **Combat:** Multi-enemy combat encounter against the Heavy Vault Sentinel and Corridor Enforcer in the inner treasure chamber (11-hit combo, 2 finishers).
  - **Item Recovery:** Recovered Treasure 3: Carved Sandstone Rosette + Key-Keeper Duty Ledger signed "M." (evidence piece revealing insider access).
- **Performance Sample:** 58.1 fps mean, 115.7 ms maximum in Unity Editor. Score: 3900 (Grade S).
- **Console Errors:** 0 errors.
- **Heritage Content:** Sandstone architectural carvings and temple treasury storage practices documented in journal.
