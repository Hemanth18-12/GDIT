# Voice audit (2026-11-23)

What the project had before the voice-over work. Everything here was read from the code and files on this date.

## What exists
| Part | Where | What it does today |
|---|---|---|
| Story lines | `Resources/Story/story.json` | 25 one-off **barks** (V3) and 2 short **conversations** (4 lines). A bark is triggered by a story event (`start:<objective>`, `done:<objective>`, `v3:<event>`). |
| Bark queue | `StoryDialogueSystem.Say()` / `UpdateBarks()` | Up to 4 barks queued; one shown at a time as a subtitle box ("bark" panel, bottom centre) for a time based on text length. Fires `OnBark`. |
| Voice playback | `UI/VoicePlayer.cs` | One 2D `AudioSource`. Plays a clip for every bark (`OnBark`) and every dialogue line (`UIManager.OnSubtitleLine`). Looks up `Resources/Voice/<speaker>/<line id>.wav`, then `<id>_tts.wav`, then a text hash, then generated "talk blips". Volume: `Settings_VoiceVol`. |
| Existing clips | `Resources/Voice/<speaker>/*.wav` | 146 WAV files: 29 V3 lines plus older V2 lines, all made with macOS `say` (`Tools/audio/build_v3_voices.py`). Kept unchanged. |
| Intro cutscene voices | `Gameplay/IntroCinematic.cs` `Say()` | 4 lines in the New Game arrival (Pujari, Aryan, Meera, Aryan), played on its own `AudioSource` with its own subtitle. |
| Mixer | `Audio/Resources/Audio/LotMixer.mixer`, `UI/AudioDirector.cs` | Groups Music, Ambience, SFX, UI, **VoiceAryan**, **VoiceNPC**. Aryan's chain changes with effort tier (calm/firm/shout). Per-scene reverb. **Ducking:** music -6 dB and ambience about -4 dB while `VoicePlayer` plays. |
| Lip sync | `Characters/FaceAnimator.cs`, `UI/FaceVoiceBridge.cs` | Reads the playing clip's loudness and spectrum: visemes on Meera, jaw bone on Aryan, a small nod on characters without a face rig; blinking. The bridge only finds Aryan, the Pujari and Meera. |
| Subtitles | bark panel, dialogue UI, intro subtitle | Setting `Settings_Subtitles` exists (main menu) but the bark panel does not check it. |
| Settings | `HeritagePauseMenu`, `MainMenuController` | Volume, Voice volume, subtitles (menu only), difficulty, graphics. No voice frequency. |

## What is missing
- No voice at all for gameplay moments: walking, running, jumping, the jeep, exploring, puzzle steps, hints, fights, items, the Dawn Clock, NPC ambience.
- No priority, no cooldown, no "don't repeat" logic: two voices can overlap (intro source and `VoicePlayer`, or a bark and a dialogue line).
- No voice frequency setting; bark subtitles ignore the subtitle setting.
- Lip sync does not reach Madhav, Bhaskar, Kishore, the robbers or villagers; NPC voices are 2D (no position).
- The existing voices are macOS `say` placeholders with mixed quality (Aman and Tara are natural Siri voices; Rishi, Rocko, Reed and Grandpa are robotic).

## Events that can trigger voice
| Source | How it can be read |
|---|---|
| Player movement, run, jump, landing | `PlayerController.IsMoving`, `IsRunning`, `JumpCount`, `CurrentSpeed`, `CharacterController.isGrounded` (watched every frame) |
| Area entered | `ZoneAtmosphere.Instance.Current.zone` (Bazaar, Courtyard, Vaults, Stepwell, SunWall, Jungle) |
| Jeep | `Jeep.Instance`: `Occupied`, `Car.CurrentSpeed`, `Car.Driven`, `DamageState` (0-3), `LastImpact`, `TotalDamage` |
| Puzzles | `PuzzleGroup.All`: `Solved`, `OnSolved`, `HintsUsed`, `conditions`; every part's `PuzzleElement.Changed` |
| Pot and lantern (storeroom) | `BreakInPuzzle`: `Sockets[i].State`, `PotWhole`, `LampSpot.State`, `LockPlate.State`, `Flagstone.State` |
| Clues and treasures | `EvidenceBoard.Found` / `Pieces`; `TreasurePickup.Taken` |
| Fights | `Brawl.Active`: `Health`, `DamageTaken`, `MaxCombo`, `Finishers`, `Won`, `Enemies`; a dodge counter must be added |
| Stealth tail | `BazaarTail`: `Now` (phase), `Spotted` |
| Jeep chase | `JungleChase.Now` (Waiting, Chase, Court, Wave1, Wave2, Boss, Done) |
| Dawn Clock | `DawnClock.Instance.Minutes` of `DawnMinute` (155) |
| Weather | `WeatherManager.Instance.Intensity` (rain at the stepwell) |
| NPCs | `NPCDialogueTrigger` (Pujari `npc_priest`, Meera `npc_villager`), `MadhavCameos.Madhav`, `VillageLife.Npcs` |
| Story | `StoryDialogueSystem.OnBark`, `OnObjectiveChanged`, `OnNewGame`; `EventBus.OnGameState` (Ending); `UIManager.OnSubtitleLine` |
| Cutscene and pause | `IntroCinematic.Playing`, `InputGate.CinematicActive`, `Time.timeScale` 0 in the pause menu |

## Plan (decided)
One new **VoiceManager** owns every voice: story barks and dialogue lines from `VoicePlayer`, the intro lines, and all new event lines. It uses one queue with priorities, so voices never overlap. Gameplay is read by a **VoiceWatchers** component instead of editing many scripts; a tiny `VoiceEvents` hook in Core covers the few moments that cannot be watched (hint asked, dodge). Lines live in `Docs/VOICE_LINES.csv` and are built into a `VoiceDatabase` ScriptableObject. Details and reasons are in `Docs/DECISIONS.md` (D-41 onward).
