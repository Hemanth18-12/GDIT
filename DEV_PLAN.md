# DEVELOPMENT PLAN: LEGACY OF THE TEMPLE
**Cinematic Third-Person 3D Heritage Mystery-Investigation Game**

---

## 1. Level-to-Scene Architecture

The storyline spans 3 primary investigative levels mapped onto seamless, high-fidelity Unity scenes:

| Level | Title | Unity Scene(s) | Key Objectives & Story Progression |
| :--- | :--- | :--- | :--- |
| **Menu** | Main Menu & Cinematic Prologue | `Assets/_Project/Scenes/Dev/MainMenu.unity` | Atmospheric temple backdrop, slow cinematic pan, title "LEGACY OF THE TEMPLE", menu options (New Game, Continue, Level Select, Settings, Controls, Help, Credits, Exit). Skippable opening cinematic establishing the temple robbery. |
| **Level 1** | **"The Missing Heritage"** | `Assets/_Project/Scenes/Environments/Courtyard.unity`<br>`Assets/_Project/Scenes/Environments/Sanctum.unity` | **Arrival & Investigation**: Detective Aryan arrives at dawn at the grand Jagannath temple. Meets Priest Ananda and Villager Meera. Inspects the violated sanctum and temple storage room. Discovers the broken brass lock, muddy bootprints, and torn fabric. Questions the Priest to complete the initial incident report and deduces the presence of two thieves. Unlocks Level 2. |
| **Level 2** | **"The Trail of the Robbers"** | `Assets/_Project/Scenes/Environments/ShrineCrime.unity`<br>`Assets/_Project/Scenes/Environments/PuzzleWall.unity` | **Tracking & Decryption**: Follows the muddy footprints outside the sanctum past the outer shrine. Examines the overturned brass lamp and dropped robber cloth. In the hidden stone passage, solves the ancient rotating lotus chakra wheel puzzle and relief panels (Lotus, Conch, Wheel, Kalash) to open the secret exit tunnel leading to the jungle. Unlocks Level 3. |
| **Level 3** | **"The Final Truth"** | `Assets/_Project/Scenes/Environments/JunglePath.unity` | **Confrontation & Recovery**: Traverses the misty forest ravine and ascends the ancient moss-covered stone stairs. Confronts the two thieves—Kala (Lead Thief) and Rudra (Accomplice) cornered at the ruined jungle altar. Uses collected evidence in dialogue to expose their scheme, secures the 3 fictional stolen heritage relics, and returns them to Priest Ananda. Triggers the cinematic ending and mission completion report. |

---

## 2. Fictional Cultural Artifacts (Cultural Respect Rules)
In accordance with cultural guidelines, the sacred Lord Jagannath 3D idol (`Idol_Jagannath.fbx`) is strictly a **respectful non-interactable shrine element** in the background. The three stolen items are 100% fictional heritage relics:
1. **The Sacred Pradip Vessel**: An ornate 16th-century ceremonial brass oil lamp engraved with floral motifs.
2. **The Carved Sandstone Rosette**: An architectural stone fragment dating back to the Eastern Ganga dynasty.
3. **The Ancient Tala Patra Manuscript**: A sacred palm-leaf scroll detailing ancient temple astronomical records.

---

## 3. Character Rigging & Animation Pipeline

| Character | Source Mesh Status | Rigging Plan | Animations |
| :--- | :--- | :--- | :--- |
| **Investigator (Aryan)** | Rigged (121 bones, `noir_ARM`) | Humanoid avatar configured with bone remapping for Unity Humanoid retargeting. | `Investigator_Idle`, `Investigator_Walk`, `Investigator_Run`, `Investigator_Crouch`, `Investigator_Interact`, plus investigation poses (kneel, flashlight). |
| **Priest (Ananda)** | High-poly static sculpt (997k polys) | Mesh copy decimated to ~40k polys in Blender; skinned to standard Humanoid armature with white dhoti weights. | Idle prayer stance, talking/gesturing, pointing to storage room. |
| **Villager (Meera)** | Rigged Mixamo Humanoid (73 bones) | Already rigged with Humanoid avatar. Saree physics and walking path. | `AvatarRoot\|mixamo.com\|Layer0` (Catwalk/Saree Idle), walking patrol. |
| **Robber 1 (Kala)** | High-poly static sculpt (1.99M polys) | Mesh copy decimated to ~45k polys; skinned to standard Humanoid armature. | Menacing idle, dialogue argument, fleeing sprint. |
| **Robber 2 (Rudra)** | High-poly static sculpt (1.96M polys) | Mesh copy decimated to ~45k polys; skinned to standard Humanoid armature. | Nervous crouch, stealth idle, cornered surrender. |

---

## 4. Input & Control Scheme (Input System)

- **WASD / Left Stick**: Move Investigator
- **Mouse Delta / Right Stick**: Orbit Camera / Mouse Look
- **Left Shift / Right Trigger**: Sprint / Run
- **Space / Button South**: Jump
- **Left Ctrl / C / Button East**: Crouch / Sneak
- **E / Button West**: Interact / Talk / Inspect
- **F / Left Shoulder**: Investigate Clue (Zoom & Rotate)
- **G / D-Pad Up**: Toggle Flashlight
- **I / Select Button**: Toggle Inventory
- **J / D-Pad Right**: Toggle Investigation Journal
- **M / D-Pad Down**: Toggle Temple Map
- **H / D-Pad Left**: Contextual Help & Progressive Hints
- **Esc / Start Button**: Pause Menu & Settings

---

## 5. Technical Risks & Mitigations

1. **High Poly Sculpt Rigs**:
   - *Risk*: Priest and Robber meshes have ~1M - 2M triangles. Skinning 2M triangles directly can cause severe frame drops.
   - *Mitigation*: Decimate duplicate copies to optimized game-ready LODs (~35k-45k tris) preserving normal maps and PBR textures before rigging.
2. **Camera Collision / Wall Clipping**:
   - *Risk*: Tight temple corridors and colonnades causing camera to clip through stone walls.
   - *Mitigation*: Implement spherical raycast obstacle avoidance in `OrbitCamera.cs` that dynamically pulls the camera forward when an obstacle is detected.
3. **Batch-Mode Verification**:
   - *Risk*: Unsaved scene changes or broken references during automated batch builds.
   - *Mitigation*: Automated Editor test runners execute compile checks and asset validation methods after each phase, saving dirty scenes and updating build settings.

---

## 6. Manual User Actions (Optional Enhancements)
- **Mixamo Downloads**: If the user desires additional custom motion clips from Mixamo, download them as **FBX ("Without Skin")** and place them in `Assets/_Project/Animations/Clips/`. The system automatically ingests, re-targets, and links them.

