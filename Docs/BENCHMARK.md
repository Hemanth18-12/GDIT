# Benchmark: Legacy of the Temple vs. professional third-person adventure / investigation games

Sources (public, design-level only; no assets or text copied):
- Simon Clavet, "Motion Matching and The Road to Next-Gen Animation", GDC 2016 (Ubisoft, *For Honor*).
- Naughty Dog GDC talks on *Uncharted* / *The Last of Us* animation and cameras (e.g. "Animation Bootcamp", GDC 2014–2017 animation summits).
- John Nesky, "50 Game Camera Mistakes", GDC 2014.
- Game Accessibility Guidelines (gameaccessibilityguidelines.com): text size, contrast, subtitles, remapping, hints.
- Microsoft Xbox Accessibility Guidelines (XAG 101 Text display, 102 Contrast, 104 Subtitles).
- WCAG 2.x contrast ratio formula (used in our AuditBot), AAA level 7:1.
- Public design writing on *Sherlock Holmes: Chapter One* (Frogwares), *Tomb Raider* (2013) and *Assassin's Creed* (Ubisoft) objective, hint and "eagle/survival instinct" guidance systems.

| Area | What a good version does | Our game BEFORE (Phase 0) | Our game NOW | Gap left |
|---|---|---|---|---|
| Locomotion | Idle/walk/run blend from speed; start/stop and turn-in-place clips; feet planted (motion matching or root-motion-driven); foot IK on stairs and slopes | Walk/run hips drifted 3.2 m off the collider; jump did nothing; no start/stop/turn | Root XZ no longer baked, so the body stays over the collider (verify in AUDIT_AFTER) | Start/stop, turn, strafe, fall/land, foot IK need Mixamo clips (list in STATUS_AUDIT §2) |
| Camera | Over-the-shoulder follow, collision pull-in, framing on the speaker in dialogue, never starts inside geometry | Start view inside a dark curtain; dialogue framed only the first NPC | Dialogue frames each line's speaker (over-the-shoulder two-shot) | Start-position camera still under investigation |
| Interaction prompts | One clear prompt near the object, with the key glyph, never overlapping other UI | Prompt overlapped dialogue box | Prompt hidden while talking; labels drawn with TMP and pushed apart | — |
| Objective guidance | Always-visible current objective; an optional "show me" (AC Eagle Vision, TR Survival Instinct, Sherlock's concentration mode) | Objective never changed from the first one | Data-driven objectives; HUD updates; Guide panel with direction + distance; "Show me the way" beacon, edge arrow and path line | Path uses NavMesh only if one is baked; else a ground-following line |
| Hint system | Progressive hints (general → specific → answer), opt-in, no penalty pressure | 3-tier IMGUI hints, tiny font, unrelated to state | 3 hints per objective from story data, revealed one by one; button pulses after 60 s idle | — |
| Tutorial pacing | Teach one verb at a time, in context, skippable | 12-step modal + a separate How-to-Play brief | Unchanged structure, but readable; Guide covers "what now" | Could fold tutorial into the first objectives |
| Dialogue | Speaker name + portrait per line, subtitles, typewriter skip, 2–3 choices that matter, camera on speaker | Name fixed per conversation, story hard-coded, most beats never triggered | JSON-driven, per-line speaker, choices (2–3) in 4 key scenes, voice hook `UIManager.OnSubtitleLine` | Portraits are initials, no voice audio |
| HUD readability | ≥ 28–30 px body at 1080p, high contrast, scalable text, consistent font family | 11–24 px IMGUI text in 14 scripts, overlaps | All player-facing text is TMP with Cinzel + Noto Sans, ≥ 30 px, 85% dark panels, Text Size + High Contrast | Dev HUD (Ctrl+F12) stays IMGUI, dev builds only |

## Priority gaps (fixed in Phase 2 in this order)
1. Hero walk/run drift, NPC talk clip sinking (A).
2. Story not wired / objectives frozen (B).
3. No way to get unstuck (C, Guide).
4. Unreadable text and overlaps (D).
5. Esc and number-key conflicts, New Game not resetting progress (E).
Remaining (needs the user): Mixamo clips for start/stop/turn/strafe/fall/talk and distinct robber gaits.
