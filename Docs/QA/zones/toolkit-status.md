# Physical toolkit review — 24 November 2026

Reused: RotatingDisc, PuzzleLever / WaterValve, PressurePlate, PushableBlock, LightBeamEmitter / LightMirror / LightSocket, ProximityMatch, WaterBasin, SequenceLock / SequenceKey, Carryable / PlacementSocket, PuzzleGroup, PuzzleSave.

Added: GearTrain (correctly seated gear sizes transmit opposite rotation at tooth ratios), RopeSwing (arc crossing with obstructed-landing recovery), DestructibleWall (release prerequisite, reversible intact/rubble geometry).

Corrected: runtime-created elements restore only after the builder assigns their persistent ID. Sockets now save the actual placed item and reconstruct its physical placement after loading, rather than leaving an invisible solved flag.

**Verified in Editor Play Mode:** `toolkit-round1.xml`: 15 passed, 0 failed (new cases plus existing puzzle framework). Tests deliberately exercise wrong gear placement, removed/recreated sockets, reset, and visible geometry after releasing a wall brace. RopeSwing has source review only; its traversal check is pending and is not covered by those 15 tests.
