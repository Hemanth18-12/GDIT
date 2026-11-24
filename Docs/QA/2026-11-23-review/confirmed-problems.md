# Inspection before editing — 23 November 2026

1. PlayerController.HandleMovement reverses ascent above startHeight + 1 m. A valid staircase or raised path higher than that is forcibly blocked. CharacterGrounding also limits steps to 0.30 m. Inspect the actual path before changing geometry.
2. LookTuner.Apply shifts all shadows toward indigo; outdoor fill is blue, saturation +10, contrast +10 and vignette 0.25–0.26. The supplied frame shows poor character/floor separation and a strong blue cast. Fresh captures are required to establish current severity.
3. Jeep.GetOut tests one exit point and a single ray. If that ray hits the jeep first it never searches the ground below; it does not test whether the character capsule fits between jeep and wall. GetIn moves the transform directly through obstacles.
4. EditorBuildSettings starts with an unrelated CarFootball scene from pre-existing uncommitted work. Preserve that work; use an explicit temple-only build scene list.
5. Existing voice assets and queued playback are present. Do not invent missing recordings or claim audio is absent without a fresh runtime check.

Existing backup-visual-1 tag retained. File copies and the initial working-tree diff are saved alongside this note. No original Heritage Mystery Project or Blender sources will be modified.
