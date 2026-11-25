# Pujari handovers — implementation notes

Scope: TempleRing's six experimental zones, per the requested brief. The current original Courtyard game route remains the main New Game route. The original Blender, Heritage Mystery files and shrine model are not edited.

- JSON supplies the six relic labels, historical lines, two choices and replies. Each collectible has an explicit stable save ID matching its previous treasure name.
- Pickup/zone completion remains independent of the lore delivery ledger. Carried/order/handed keys restore pending deliveries and prevent duplicate 500-point awards.
- K and both pause menus expose task skip; T and a Central Temple map entry return carried replicas to Ananda. Return is blocked during combat/dialogue and while occupying the Jeep.
- The existing priest prefab is copied into Resources/NPC because that resource path did not exist. The original prefab and model remain unchanged.
- VoicePlayer delegates lore subtitles to the existing single-owner VoiceManager. Its existing pause, face, animation and voice routing remain in use. Gameplay reaches UI through callbacks/events, preserving assembly dependencies.
- Kokoro generated 38 clips offline: the requested 32 plus six voiced second-choice answers. WAV validation is recorded in voice-validation.csv. Speech recognition was disabled as requested; automated pronunciation accuracy is not claimed.
- Historical references are listed in ATTRIBUTION.md. Recovered objects are fictional teaching replicas; shrine images remain outside inventory.

Final test/build results and image review are recorded separately after the verification run completes.
