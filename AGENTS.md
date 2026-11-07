# Legacy of the Temple — persistent project instructions

- Never self-grade by checking that files exist. Run the real game (Editor Play mode and the built .app), take screenshots, look at them yourself, and judge them against my complaints. Save evidence in Docs/QA/.
- Create git tag backup-visual-1 before big changes. Never modify my original Blender / Heritage Mystery Project files (read-only). Builds/ stays in .gitignore. Never delete files outside this project.
- Ask me before installing software or downloading anything large. Use only assets with a clear license (CC0 / CC-BY / own work) and list each in ATTRIBUTION.md (title, author, URL, license).
- Zero compile errors and warnings. Loop: fix, rebuild, relaunch, re-screenshot, up to 10 rounds, then report honestly what still fails. Never claim "done" without evidence.
- EXPLAIN CLEARLY: after every task, write a plain-English note (what you changed, why, which files, how I see it in Unity or the game) for a beginner, append it to Docs/EXPLAINED.md and summarize it in the chat.
- Do NOT fake animation with generated .anim bobbing. Do NOT invent characters (no "Kala" or "Rudra" bosses).
- Cultural respect: the stolen items are FICTIONAL heritage objects (a brass ceremonial vessel, a carved sandstone rosette, a palm-leaf manuscript). Idol_Jagannath.fbx is only a non-interactable, respectful background shrine element; it is never stolen, carried or touched.

CAST: Investigator "Aryan" (hero), Pujari "Ananda", Meera (village girl in red and green saree), Robber 1 "Bhaskar" (red scarf, dark vest), Robber 2 "Kishore" (black hood, cross-strap). Use my ORIGINAL models from the Heritage Mystery Project.


## Story and levels

Implement as data-driven dialogue (ScriptableObjects or JSON), with portraits, typewriter text, subtitles, voice-ready hooks, camera framing for each speaker, NPC talk animations, and 2 or 3 choices in key conversations. Levels map to scenes: Level 1 = Courtyard + Sanctum + ShrineCrime, Level 2 = PuzzleWall, Level 3 = JunglePath. Use this script (polish the wording, keep the beats):
SCENE 1 COURTYARD (dawn). PUJARI: "Namaste. Three pieces from our storage hall are gone: a brass ceremonial vessel, a carved sandstone rosette, and a palm-leaf manuscript. Please find them before the festival." ARYAN: "Stay calm and touch nothing. Every small detail matters. Did anyone see something?" MEERA: "Before sunrise I saw two men near the storage door. One wore a red scarf, the other a black hood." Choices: ask Meera which way they ran / ask the Pujari why the pieces matter (answer: they carry the community's memory; label it fiction).
SCENE 2 SANCTUM / STORAGE HALL. ARYAN: "Three empty cloth-lined stands. Whoever came knew the hall." PUJARI: "Only temple staff know where the key is kept." Clue: broken lock, empty stands.
SCENE 3 SHRINE CRIME. ARYAN: "Muddy bootprints, two different sizes, and red silk caught on the door." PUJARI: "That cloth is not ours." Clues: footprints, red cloth, broken lamp. Aryan concludes two suspects.
SCENE 4 PUZZLE WALL. PUJARI: "My grandfather spoke of a wall that opens when four symbols face the rising sun." Scroll shows order: lotus, conch, wheel, kalash, then turn the centre wheel. ARYAN on success: "The passage is open. They went this way."
SCENE 5 JUNGLE PATH. BHASKAR: "Keep moving. The investigator is close." KISHORE: "I told you the lamp would leave marks." ARYAN: "It's over. Return what belongs to the community." BHASKAR: "Do you know what these fetch?" ARYAN: "Their value isn't money. It's memory." Non-violent surrender; objects recovered; Aryan returns them to the Pujari at the Courtyard at sunrise. ENDING TEXT: "Every temple holds a story. Every artifact carries a memory. Protecting heritage means protecting our history." then MISSION COMPLETED with evidence, score and time.


## Workspace
- Unity 6000.6.8f1, URP, macOS. Open `My project`; Assets, Packages, and ProjectSettings there are symlinks to this repository root. Store shared documentation here.
- Original `Heritage Mystery Project/` and Blender sources are read-only. Never save or modify them.
- Follow the user task order: Task 1 and asset shopping list, then remaining tasks. Honor explicit WAIT points for large downloads, uncertain investigator identity, and required external animation clips.
- Preserve pre-existing uncommitted work. Do not repeat old QA claims without fresh gameplay evidence.
