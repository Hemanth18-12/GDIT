# LEGACY OF THE TEMPLE — Loading Screens and Visual Bible

Status: PHASE A document. Stay in **URP**. No HDRP migration. Target: 60 FPS at 1920 × 1080 on this Mac.

**Licence check status:** asset licences below are stated from general knowledge and were **not
re-verified online in this phase**. Each must be confirmed on its download page in Phase E and
recorded in ATTRIBUTION.md.

## Part A — Loading screens

### A1. Layout (reference 1920 × 1080)

```
┌──────────────────────────────────────────────────────────────┐
│  full-screen image, slow zoom 100% → 106% over 12 s          │
│                                                              │
│                                                              │
│  ░░ dark gradient, bottom 38% of the screen ░░               │
│  CHAPTER 2                                (Cinzel, 28, gold) │
│  THE EMPTY STANDS                         (Cinzel, 56, ivory)│
│  Dawn in 02:15                            (Noto Sans, 30)    │
│  "A lamp that is still warm was lit by someone nearby."      │
│  ▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▭▭▭▭▭▭▭▭▭  thin gold bar, 4 px, 62%       │
└──────────────────────────────────────────────────────────────┘
```

- **Image:** fills the screen (cover, centre crop). Zoom is slow and linear with a 1% drift toward
  the image's focal point. A second copy cross-fades in over 0.6 s; fade to black 0.4 s on exit.
- **Gradient:** black, 0% to 85% alpha, bottom 38%, so text always has contrast. A light vignette on top.
- **Progress bar:** 4 px tall, 60% of the screen width, left-aligned with the text, gold (#D4A64A) on
  a 20%-white track. It follows the real `AsyncOperation.progress`, smoothed, and never moves backwards.
- **Minimum display time:** 2.5 s so the tip can be read; "Press Enter to continue" appears only if
  loading finished early and the player has seen this tip before.
- **Dawn Clock line:** "Dawn in H:MM", the same value as the HUD. The clock does not run while loading.
- **Tip:** one per load, rotating without repeats, 24 px italic, maximum 90 characters.
- All text stays in the bottom gradient band. Nothing is ever drawn over a shrine figure in an image.

### A2. Image mapping

Files you will provide in `Assets/_Project/UI/Loading/` (PNG or JPG, 16:9, at least 1920 × 1080,
ideally 2560 × 1440; import as Sprite, sRGB, no mipmaps, max size 2048, high-quality compression).

| Going to | Image | Chapter title |
|---|---|---|
| Courtyard (start) | loading_01_dawn_courtyard.png | CHAPTER 1 — 3:10 AM |
| Sanctum (Storage Hall) | loading_04_shrine.png | CHAPTER 2 — THE EMPTY STANDS |
| ShrineCrime (Outer Shrine) | loading_02_gate_night.png | CHAPTER 3 — TRACKS IN THE DARK |
| PuzzleWall (Sun Wall) | loading_02_gate_night.png | CHAPTER 4 — THE WALL THAT REMEMBERS |
| JunglePath (Jungle Ruins) | loading_03_jungle_path.png | CHAPTER 5 — BEFORE FIRST LIGHT |
| Courtyard (final return) | loading_01_dawn_courtyard.png | FIRST LIGHT |

If a file is missing, the loader falls back to a dark sandstone gradient with the same text, so the
game never breaks.

### A3. Notes on the three images received so far

Only three images were attached to the brief (shrine interior, night gate, jungle path). The dawn
courtyard image (loading_01) has not been seen yet.

1. **The hero does not match.** All three show a woman explorer with a ponytail, a backpack and
   tactical gear. Our hero is Aryan: a man in a hat and a long coat. Options, best first:
   - **Regenerate** the images with a matching figure (man, brimmed hat, long dark coat, seen from
     behind, unarmed), or with no figure at all.
   - **Crop** the figure out. The night gate works well cropped to its right two-thirds (door, lion,
     lamps). The jungle path works cropped to its right 60% (path, distant temple, moon).
   - **Overlay:** let the dark gradient and a left-side vignette cover the figure. Weakest option;
     the silhouette still reads as someone else.
2. **The explorer appears to carry a holstered weapon** (shrine and gate images). Aryan is unarmed
   and the game is non-lethal. A weapon inside a sanctum also works against the respect rule.
   Regenerate or crop.
3. **The shrine image puts the deity at the centre of the frame.** The project rule is that the
   shrine figure is a respectful background element. Recommended: use a version framed on the lamps,
   pillars and light shafts, with the shrine soft and off-centre, or crop to the left half. If it is
   kept as it is, no text, bar or logo may overlap the figure (the layout above already guarantees that).
4. **A faint watermark or logo is visible in the top-right corner** of each image. Check the image
   generator's terms: whether commercial or public use is allowed and whether the mark may be cropped.
   Record the tool, date and prompt in ATTRIBUTION.md as "AI-generated image, own prompt".
5. Style note: the images are far more detailed than the game can render. That is normal for loading
   art, but Part B aims to close the gap in mood (light, colour, fog), which is what the eye compares.

### A4. Suspense tips (rotating)

1. "The lock was not forced. Ask who had the key."
2. "A lamp that is still warm was lit by someone nearby."
3. "The deeper print carries the heavier load."
4. "A wrong turn at the Sun Wall costs minutes you do not have."
5. "Crouching makes no sound. Running does."
6. "A thrown pebble buys you six seconds."
7. "The red glint cannot be blocked. Step aside."
8. "Your flashlight can blind as well as reveal."
9. "People lie fastest about the things they love."
10. "Bells on a thread: someone expected to be followed."
11. "Time saved in one chapter is time earned for the next."
12. "Dawn does not wait."

## Part B — Art direction

### B1. Targets (from your reference images)

| Reference | What to take from it |
|---|---|
| Golden sunrise on a carved sandstone Odisha temple, mist, wet reflecting stones | Low warm key light, long shadows, ground mist, mirror-like paving |
| Blue moonlight against warm oil-lamp glow | Two-colour lighting: cool ambient, warm pools; never grey |
| Dense jungle, fog, vines, a lit temple far away | Depth in layers: dark foreground leaves, fogged mid-ground, glowing goal |
| Sanctum with light shafts, brass lamps, garlands, red cloth | Dust-filled shafts, brass highlights, saturated red and marigold accents |

### B2. Palette

| Role | Colour | Hex |
|---|---|---|
| Sandstone, lit | warm ochre | #C8935A |
| Sandstone, shadow | red-brown | #6B3F2A |
| Lamp flame / gold | amber | #FFB347 |
| Brass | muted gold | #B8892E |
| Moonlight | steel blue | #6F8FBF |
| Night shadow | deep indigo | #141B2E |
| Jungle green | dark moss | #23402B |
| Cloth accent | temple red | #A3231C |
| Marigold accent | orange-yellow | #F2A900 |
| Mist | pale warm grey | #D9CFC0 |

Rule: every frame has one warm and one cool source. UI gold stays #D4A64A.

### B3. Lighting recipe per map

| Map | Time | Key light | Fill / ambient | Practical lights | Fog | Mood |
|---|---|---|---|---|---|---|
| Courtyard (Ch 1) | 03:10, night | Moon, 0.4 intensity, #6F8FBF, 35° elevation, soft shadows | Trilight: sky #1B2540, equator #23202A, ground #0E0C0B | 8 oil lamps (point, range 6 m, #FFB347, flicker), 2 braziers | Exponential², density 0.012, #1A2238 | Uneasy calm |
| Storage Hall (Ch 2) | Night, interior | None at first | Ambient near black (#07070A) | Flashlight (spot 48°, soft shadows); five wall lamps add light as they are relit | Density 0.02, dust in the beam | Blind, then revealed |
| Outer Shrine (Ch 3) | 04:05, moonlit | Moon 0.6, harder shadows | Sky #22305A | 3 lamps, one broken; moon glints on tripwires | Ground mist cards, density 0.015 | Cold and precise |
| Sun Wall (Ch 4) | 04:30, interior | A shaft from a roof slot, cool, turning warm as the wall opens | Ambient #0C0E16 | Wall lamps; glowing symbol inlays (emissive) | Density 0.018, falling dust particles | Pressure |
| Jungle Ruins (Ch 5) | 05:05 → 05:45, pre-dawn | Moon fading; horizon glow rises during the chapter (sky lerp driven by the Dawn Clock) | Sky #1A2A3A → #5A4A52 | 4 braziers in the arena, robbers' lantern | Density 0.03, layered fog cards between trees | Hunted, then explosive |
| Courtyard (Ch 6) | 05:45, first light | Sun 1.3, #FFC98A, 6° elevation, very long shadows | Sky #8FA6C8, equator #E8B98A | Lamps still lit; the Surya Pradeepa flame | Density 0.02, warm mist #D9CFC0 | Release |

Shadow budget: one shadow-casting directional light, plus at most 2 shadowed additional lights in view
(flashlight + nearest brazier). All other practicals are unshadowed.

### B4. Materials (PBR, URP Lit)

| Material | Maps | Notes |
|---|---|---|
| Sandstone wall, carved | Albedo, normal, mask (metal/AO/detail/smooth) | Detail normal for grain; 2K, tiling with macro variation |
| Sandstone paving | Same, plus a wetness mask | See wet stone below |
| Weathered sandstone / laterite (ruins) | Same, moss blend by world-up | Vertex-colour or height blend for moss |
| Brass (lamps, vessel, bells) | Albedo, normal, metallic 1.0, smoothness 0.75 with a smudge map | Needs reflection probes to look right |
| Dark wood (doors, stands) | Albedo, normal, roughness | |
| Red silk / cotton cloth | Albedo, normal; double-sided | Slight sheen via smoothness 0.4 |
| Marigold garlands | Alpha-clipped cards or low-poly beads | Emission 0 |
| Palm leaf (manuscript) | Albedo, normal | Fine etched-line normal |
| Mud, gravel, forest floor | Terrain layers, 2K | Height-blended |
| Foliage (ferns, vines, palms, tall grass) | Alpha-clipped, two-sided, wind in the shader | SpeedTree-style or simple vertex sway |
| Water (ford, puddles) | Scrolling normals, depth fade | No planar reflection |

**Wet stone look:** darken albedo by about 25%, raise smoothness to 0.85 inside a puddle mask (noise
texture × upward-facing × vertex paint), flatten the normal inside puddles, and place **box-projected
reflection probes** (baked, 256 px) in each courtyard. Add screen-space reflections only if the frame
budget allows; baked probes are the default. Small ripple normal animation near lamps is optional.

### B5. Volumetric light and fog (URP-friendly)

URP has no built-in volumetric fog. Use cheap techniques that read well:
- **Light shafts:** additive, soft-edged mesh cones or quads with a scrolling noise texture and depth
  fade, placed at the hall's roof slot, the sanctum doorway and through the jungle canopy.
- **Ground mist:** large soft particle cards with depth fade, slow drift, about 20 per scene.
- **Fog:** built-in exponential-squared fog tinted per map (values in B3), plus a height-fog term in
  a simple full-screen pass if needed.
- **Dust:** a small GPU particle system inside the flashlight cone and the shafts.
- **Flame glow:** an additive billboard halo on each lamp, with bloom doing the rest.

### B6. Post-processing (URP Volume, one global profile plus one override per map)

| Effect | Setting |
|---|---|
| Tonemapping | ACES |
| Colour grading | Lift toward indigo in shadows, gain toward amber in highlights; contrast +12; saturation −5 at night, +8 at dawn. A 32-px LUT per map. |
| Bloom | Threshold 1.0, intensity 0.35 (0.5 at dawn), scatter 0.7, high-quality filtering off |
| SSAO | URP renderer feature: intensity 0.6, radius 0.35, half resolution, after opaque |
| Vignette | 0.22 (0.35 in stealth, pulsing with low health in the fight) |
| Film grain | Thin, intensity 0.15 |
| Depth of field | Bokeh **only** in dialogue close-ups and cinematics; off in gameplay |
| Motion blur | Off (or 0.1 in chases only) |
| Anti-aliasing | SMAA high, or MSAA 2× if SSAO cost allows; no TAA ghosting on thin tripwires |
| Chromatic aberration, lens distortion | Off, except a 0.3 s pulse when hit |

### B7. Props and vegetation

- **Temple:** carved pillars, lion guardians, stepped plinths, door frames with relief bands, hanging
  bells on chains, stone lamp towers, brass floor lamps, offering plates, garlands, red and saffron
  cloth hangings, flag on the spire, clay pots, a well with rope and bucket.
- **Storage Hall:** three cloth-lined stands, wooden chests, shelves, a key hook, rolled mats, wall lamps.
- **Outer Shrine:** threshold stones, tripwire posts with small bells, broken lamp, scattered petals.
- **Sun Wall:** four symbol discs, centre socket, hand-wheel crank, wall niche, gear housings, dust piles.
- **Jungle Ruins:** broken steps, fallen lintels, root-wrapped pillars, braziers, the rope and pole
  bundle, tall grass patches, banyan aerial roots, vines, ferns, palms, a stream and the ford.
- The shrine figure remains a background element in the Sanctum only: softly lit, never interactive.

### B8. Asset shopping list (free, to be licence-checked at download)

| Need | Candidate source | Licence as understood | Note |
|---|---|---|---|
| Stone, sandstone, mud, forest-floor PBR textures | Poly Haven; ambientCG | CC0 | 2K is enough; 4K only for hero walls |
| Night and dawn sky HDRIs | Poly Haven | CC0 | Used for ambient and reflections |
| Rocks, ruins pieces, some plants (photoscans) | Poly Haven models | CC0 | Decimate to game budgets |
| Foliage, trees, grass | Quaternius; Kenney nature kits; OpenGameArt CC0 | CC0 | More stylised; mix carefully or retexture |
| Extra photoscanned props | Sketchfab (filter: CC0 / CC-BY, downloadable) | Per model | CC-BY needs credit; avoid NC and "editorial" |
| High-end scans | Quixel Megascans via Fab | **Terms have changed; check before using.** Do not assume free. | Raw files may not be redistributable in a public repo |
| Temple-specific carvings, lamps, the three objects, blade, satchel | **Own Blender work** | Own work | Nothing suitable and respectful exists as CC0; these define the game's identity |
| Particle textures (smoke, embers, dust) | Kenney particle pack | CC0 | |
| Decals (cracks, moss, footprints, chalk) | ambientCG; own | CC0 / own | URP Decal renderer feature |

Rules: CC0, CC-BY or own work only; each asset listed in ATTRIBUTION.md with title, author, URL and
licence; your original Blender and Heritage files are never modified (new work goes in new files).

### B9. Performance plan (60 FPS, URP)

| Area | Budget / setting |
|---|---|
| Render scale | 1.0 at 1080p; fall back to 0.85 with FSR-style upscaling if below 55 FPS |
| Draw calls | Under 1,500 per frame; SRP Batcher on; static batching; GPU instancing for foliage and props |
| Triangles in view | Under 1.5 million; LOD groups (3 levels) on everything over 2,000 triangles |
| Lights | Forward+; 1 shadowed directional, up to 2 shadowed additional, the rest unshadowed |
| Shadows | 2 cascades, 2048 map, distance 45 m (30 m in the jungle); soft shadows medium |
| Baking | Baked lightmaps (Progressive GPU) plus light probes for static geometry; mixed lighting, Shadowmask |
| Reflection | Baked probes, 256 px, box projection; no real-time probes |
| Textures | 2K default, ASTC/BC7 compression, streaming mipmaps on |
| Particles | Under 2,000 alive; soft particles only on mist |
| Occlusion | Baked occlusion culling in the Hall, Sun Wall and jungle |
| Post | SSAO half-res; bloom low-quality filter; no real-time DoF in gameplay |

A frame-time overlay (dev builds only) reports FPS per map in Phase F; any map under 55 FPS loses
effects in this order: SSAO → film grain → shadowed additional lights → render scale.
