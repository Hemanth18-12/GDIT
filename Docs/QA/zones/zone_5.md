# Zone 5 QA — Sun Wall and Chariot-Wheel Plaza

- **Zone:** Sun Wall and Chariot-Wheel Plaza (`new_sunwall`)
- **Status:** PASSED (Verified via `NewZonePlayBot.SunWallRun` in `NewBazaarPlayTests.cs`)
- **Evidence:** `Docs/QA/zones/zone_5/editor-round1/run.md`
- **Screenshots (3):**
  1. `Docs/QA/zones/zone_5/editor-round1/01-sunwall-plaza.png` (Broad sunlit sandstone plaza facing monumental Sun Wall with carved Lotus, Conch, Wheel, Kalash panels)
  2. `Docs/QA/zones/zone_5/editor-round1/02-light-beam-reflected.png` (Pedestal mirrors rotated to bounce sunlight beam across plaza into central Chariot Wheel hub socket)
  3. `Docs/QA/zones/zone_5/editor-round1/03-smuggler-map.png` (Grand 5.2m Konark Chariot Wheel unseals cliffside door; Kishore boss defeated; Smuggler River Map & empty 5th case recovered)
- **Gameplay Checklist:**
  - **Sun Wall Monument:** 12-metre-high monumental sandstone wall with 4 carved Kalinga relief panels (Lotus, Conch, Wheel, Kalash) and central Konark chariot wheel assembly.
  - **Light Reflection Mechanics:** Sunlight emitted from the raised Sun Altar; rotatable bronze mirrors (`el.nz5.mirror_a`, `el.nz5.mirror_b`) angled to bounce the light beam across the courtyard into the central chariot wheel hub socket (`el.nz5.wheel_socket`).
  - **Monumental Chariot Wheel:** Giant 5.2m stone wheel rotated on its axis (`el.nz5.chariot_wheel`), grinding open the hidden cliffside sanctuary door.
  - **Boss Combat:** Mini-boss encounter against Kishore (Winged Smuggler), featuring 11-hit combo string, dodges, heavy strikes, and cinematic finisher.
  - **Item Recovery:** Recovered Treasure 5: Smuggler's River Map + Empty 5th Relic Case, charting the smuggler escape route along the north river and pointing to Zone 6.
- **Performance Sample:** 58.9 fps mean, 74.4 ms maximum in Unity Editor. Score: 3840 (Grade S).
- **Console Errors:** 0 errors.
- **Heritage Content:** Konark Sun Temple chariot wheel astronomy, stone relief motifs, and solar alignment traditions documented in journal.
