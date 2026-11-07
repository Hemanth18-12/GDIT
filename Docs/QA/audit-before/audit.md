# Runtime audit (2026-11-07 15:03, Standalone, 1920x1080)

## UI: MainMenu

#### UI table: MainMenu @ requested 1280x720 (actual 1280x720); TMP texts=0; active IMGUI(OnGUI) scripts not measurable=1
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
Problems in this view: 0

#### UI table: MainMenu @ requested 1920x1080 (actual 1920x1080); TMP texts=0; active IMGUI(OnGUI) scripts not measurable=1
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
Problems in this view: 0

#### UI table: MainMenu @ requested 2560x1440 (actual 2560x1440); TMP texts=0; active IMGUI(OnGUI) scripts not measurable=1
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
Problems in this view: 0

## Scene: Courtyard
Animated roots: Characters x3; 
Visible priest roots: 0 (skinned renderers 0)

### Animator: Characters
controller=PriestAnimator, avatar=NONE, isHuman=False, avatarValid=False, applyRootMotion=False
clips: Priest_Rig|Priest_Rig|Priest_Rig|Priest_Idle, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Point
- idle: clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Idle 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.15..0.15m armsRaised=0% bones(L/R/hip/arm)=1111 => OK
- talk(IsTalking=true): clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.15..0.15m armsRaised=0% bones(L/R/hip/arm)=1111 => OK

### Animator: Characters (player)
controller=CharacterAnimator, avatar=InvestigatorAvatar, isHuman=True, avatarValid=True, applyRootMotion=False
clips: mixamo.com
- idle: clips[mixamo.com 100%, ] Speed=0.00 Grounded=T Crouch=F IsMoving=F bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.04m footAboveGround=0.10..0.10m armsRaised=0% bones(L/R/hip/arm)=1111 => OK
- walk: clips[mixamo.com 100%, ] Speed=4.50 Grounded=T Crouch=F IsMoving=T bodySpeed=0.00m/s footPlant10%L/R=1.26/0.93m/s hipDrift=3.19m footAboveGround=0.09..0.23m armsRaised=0% bones(L/R/hip/arm)=1111 => HIP-DRIFT
- run: clips[mixamo.com 100%, ] Speed=7.50 Grounded=T Crouch=F IsMoving=T bodySpeed=0.08m/s footPlant10%L/R=1.09/0.83m/s hipDrift=3.24m footAboveGround=0.09..0.22m armsRaised=0% bones(L/R/hip/arm)=1111 => HIP-DRIFT
- crouch: clips[mixamo.com 100%, ] Speed=2.50 Grounded=T Crouch=T IsMoving=T bodySpeed=0.04m/s footPlant10%L/R=0.03/0.02m/s hipDrift=0.05m footAboveGround=0.15..0.15m armsRaised=0% bones(L/R/hip/arm)=1111 => FLOATING
- jump: clips[mixamo.com 100%, ] Speed=0.00 Grounded=T Crouch=F IsMoving=F bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.04m footAboveGround=0.11..0.11m armsRaised=0% bones(L/R/hip/arm)=1111 => OK

### Animator: Characters
controller=VillagerAnimator, avatar=VillagerAvatar, isHuman=True, avatarValid=True, applyRootMotion=True
clips: AvatarRoot|AvatarRoot|mixamo.com|Layer0, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Point
- idle: clips[AvatarRoot|AvatarRoot|mixamo.com|Layer0 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.02m footAboveGround=0.16..0.16m armsRaised=0% bones(L/R/hip/arm)=1111 => FLOATING
- talk(IsTalking=true): clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.02m footAboveGround=0.16..0.16m armsRaised=0% bones(L/R/hip/arm)=1111 => FLOATING

### Function test (Courtyard, injected key presses)

#### UI table: Journal @ requested 1920x1080 (actual 1920x1080); TMP texts=0; active IMGUI(OnGUI) scripts not measurable=13
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
Problems in this view: 0
- Journal [J]: open False->True, closes on 2nd press=True => WORKS

#### UI table: Map @ requested 1920x1080 (actual 1920x1080); TMP texts=0; active IMGUI(OnGUI) scripts not measurable=13
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
Problems in this view: 0
- Map [M]: open False->True, closes on 2nd press=True => WORKS

#### UI table: Help @ requested 1920x1080 (actual 1920x1080); TMP texts=3; active IMGUI(OnGUI) scripts not measurable=13
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| CardBody: INVESTIGATION MISSION GOAL: • Goal 1: Th… | 31 | 30 | 7.8 |  |
| ContinuePrompt: Press ENTER or Space to Begin Your Inves… | 36 | 34 | 5.4 | LOW-CONTRAST, OVERFLOW(text>rect) |
| CardTitle: HOW TO PLAY & INVESTIGATION BRIEF | 49 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
Problems in this view: 2
- Help [H]: open False->True, closes on 2nd press=True => WORKS
- H also toggles GameIntroOverlay instructions: False

#### UI table: Photo mode @ requested 1920x1080 (actual 1920x1080); TMP texts=1; active IMGUI(OnGUI) scripts not measurable=13
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| ToastText: PHOTO MODE: UI Hidden. Press [P] to rest… | 26 | 30 | 9.8 | SMALL |
Problems in this view: 1
- Photo mode [P]: open False->True, closes on 2nd press=True => WORKS

#### UI table: Pause @ requested 1920x1080 (actual 1920x1080); TMP texts=17; active IMGUI(OnGUI) scripts not measurable=13
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
| Text: Temple Map [M] | 36 | 30 | 12.3 |  |
| Label: Master Volume | 34 | 30 | 12.0 |  |
| PauseTitle: PAUSED | 83 | 64 | 5.5 | LOW-CONTRAST, OVERFLOW(text>rect) |
| Label: Music Volume | 34 | 30 | 12.0 |  |
| Text: Skip Tutorial | 36 | 30 | 12.3 |  |
| BtnText: Toggle Mode | 31 | 30 | 10.2 |  |
| Label: SFX Volume | 34 | 30 | 12.0 |  |
| Text: Investigation Journal [J] | 36 | 30 | 12.3 |  |
| ObjectiveTitle: Speak with Head Priest Ananda | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| Text: Replay Tutorial | 36 | 30 | 12.3 |  |
| ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5 | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Head Priest Ananda stands at the courtya… | 36 | 34 | 7.8 | OVERFLOW(text>rect), OVERLAPS 'ObjectiveTitle: Speak with Head Priest Ananda', OVERLAPS 'ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5' |
| HighContrastLabel: High Contrast: OFF | 36 | 30 | 12.0 | OVERFLOW(text>rect) |
| TextScaleLabel: Text Size: Extra Large (130%) | 36 | 30 | 12.0 | OVERFLOW(text>rect) |
| BtnText: Change Size | 31 | 30 | 10.2 |  |
| Text: Resume Game | 36 | 30 | 12.3 |  |
Problems in this view: 7
- Pause [Esc]: timeScale 1->0 closes=True => WORKS

#### UI table: Dialogue Priest @ requested 1920x1080 (actual 1920x1080); TMP texts=9; active IMGUI(OnGUI) scripts not measurable=13
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| DialoguePrompt: [Space / Enter / E to advance] | 34 | 36 | 5.4 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| DialogueBody: Namaste. Three pieces from our storage h… | 42 | 36 | 7.8 | OVERLAPS 'SpeakerName: Pujari Ananda' |
| KeyText: E | 42 | 34 | 8.0 |  |
| PromptAction: Talk to Pujari Ananda | 36 | 34 | 7.8 |  |
| ObjectiveTitle: Speak with Head Priest Ananda | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| PortraitInitial: P | 83 | 36 | 8.6 |  |
| ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5 | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Head Priest Ananda stands at the courtya… | 36 | 34 | 7.8 | OVERFLOW(text>rect), OVERLAPS 'ObjectiveTitle: Speak with Head Priest Ananda', OVERLAPS 'ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5' |
| SpeakerName: Pujari Ananda | 47 | 36 | 5.5 | LOW-CONTRAST, OVERFLOW(text>rect) |
Problems in this view: 6
- Talk [E] to Priest: prompt='Talk to Pujari Ananda', dialogueOpened=True, finished=True after 1 advance rounds => WORKS
- Clues in Courtyard: 0
- Save/Load game progress: no save system found in code (only settings + tutorial flag in PlayerPrefs) => MISSING
- Guide button: no GuideButton component => MISSING

### UI: Courtyard HUD

#### UI table: Courtyard HUD @ requested 1280x720 (actual 1280x720); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=12
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| ObjectiveTitle: CURRENT OBJECTIVE | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics Recovered: 0/3  /  Clues F… | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Speak with Head Priest Ananda at the cou… | 36 | 34 | 7.8 | OVERLAPS 'ObjectiveTitle: CURRENT OBJECTIVE' |
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
Problems in this view: 4

#### UI table: Courtyard HUD @ requested 1920x1080 (actual 1920x1080); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=12
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| ObjectiveTitle: CURRENT OBJECTIVE | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics Recovered: 0/3  /  Clues F… | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Speak with Head Priest Ananda at the cou… | 36 | 34 | 7.8 | OVERLAPS 'ObjectiveTitle: CURRENT OBJECTIVE' |
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
Problems in this view: 4

#### UI table: Courtyard HUD @ requested 2560x1440 (actual 2560x1440); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=12
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| ObjectiveTitle: CURRENT OBJECTIVE | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics Recovered: 0/3  /  Clues F… | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Speak with Head Priest Ananda at the cou… | 36 | 34 | 7.8 | OVERLAPS 'ObjectiveTitle: CURRENT OBJECTIVE' |
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
Problems in this view: 4

## Scene: Sanctum
Animated roots: Characters x3; 
Visible priest roots: 0 (skinned renderers 0)

### Animator: Characters
controller=PriestAnimator, avatar=NONE, isHuman=False, avatarValid=False, applyRootMotion=False
clips: Priest_Rig|Priest_Rig|Priest_Rig|Priest_Idle, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Point
- idle: clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Idle 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.15..0.15m armsRaised=0% bones(L/R/hip/arm)=1111 => OK
- talk(IsTalking=true): clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.15..0.15m armsRaised=0% bones(L/R/hip/arm)=1111 => OK

### Animator: Characters (player)
controller=CharacterAnimator, avatar=InvestigatorAvatar, isHuman=True, avatarValid=True, applyRootMotion=False
clips: mixamo.com

### Animator: Characters
controller=VillagerAnimator, avatar=VillagerAvatar, isHuman=True, avatarValid=True, applyRootMotion=True
clips: AvatarRoot|AvatarRoot|mixamo.com|Layer0, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Point
- idle: clips[AvatarRoot|AvatarRoot|mixamo.com|Layer0 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.01m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.02m footAboveGround=0.16..0.16m armsRaised=0% bones(L/R/hip/arm)=1111 => FLOATING
- talk(IsTalking=true): clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.05m footAboveGround=-0.60..-0.60m armsRaised=0% bones(L/R/hip/arm)=1111 => SINKING

### UI: Sanctum HUD

#### UI table: Sanctum HUD @ requested 1280x720 (actual 1280x720); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=12
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| ObjectiveCounter: Stolen Relics Recovered: 0/3  /  Clues F… | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveTitle: CURRENT OBJECTIVE | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
| ObjectiveBody: Speak with Head Priest Ananda at the cou… | 36 | 34 | 7.8 | OVERLAPS 'ObjectiveTitle: CURRENT OBJECTIVE' |
Problems in this view: 4

#### UI table: Sanctum HUD @ requested 1920x1080 (actual 1920x1080); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=12
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| ObjectiveCounter: Stolen Relics Recovered: 0/3  /  Clues F… | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveTitle: CURRENT OBJECTIVE | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
| ObjectiveBody: Speak with Head Priest Ananda at the cou… | 36 | 34 | 7.8 | OVERLAPS 'ObjectiveTitle: CURRENT OBJECTIVE' |
Problems in this view: 4

#### UI table: Sanctum HUD @ requested 2560x1440 (actual 2560x1440); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=12
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| ObjectiveCounter: Stolen Relics Recovered: 0/3  /  Clues F… | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveTitle: CURRENT OBJECTIVE | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
| ObjectiveBody: Speak with Head Priest Ananda at the cou… | 36 | 34 | 7.8 | OVERLAPS 'ObjectiveTitle: CURRENT OBJECTIVE' |
Problems in this view: 4

## Scene: ShrineCrime
Animated roots: Characters x3; 
Visible priest roots: 0 (skinned renderers 0)

### Animator: Characters
controller=VillagerAnimator, avatar=VillagerAvatar, isHuman=True, avatarValid=True, applyRootMotion=True
clips: AvatarRoot|AvatarRoot|mixamo.com|Layer0, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Point
- idle: clips[AvatarRoot|AvatarRoot|mixamo.com|Layer0 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.01m/s footPlant10%L/R=0.01/0.01m/s hipDrift=0.02m footAboveGround=0.16..0.16m armsRaised=0% bones(L/R/hip/arm)=1111 => FLOATING
- talk(IsTalking=true): clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.05m footAboveGround=-0.66..-0.66m armsRaised=0% bones(L/R/hip/arm)=1111 => SINKING

### Animator: Characters (player)
controller=CharacterAnimator, avatar=InvestigatorAvatar, isHuman=True, avatarValid=True, applyRootMotion=False
clips: mixamo.com

### Animator: Characters
controller=PriestAnimator, avatar=NONE, isHuman=False, avatarValid=False, applyRootMotion=False
clips: Priest_Rig|Priest_Rig|Priest_Rig|Priest_Idle, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Point
- idle: clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Idle 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.15..0.15m armsRaised=0% bones(L/R/hip/arm)=1111 => OK
- talk(IsTalking=true): clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.15..0.15m armsRaised=0% bones(L/R/hip/arm)=1111 => OK

### UI: ShrineCrime HUD

#### UI table: ShrineCrime HUD @ requested 1280x720 (actual 1280x720); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=12
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| ObjectiveTitle: CURRENT OBJECTIVE | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics Recovered: 0/3  /  Clues F… | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
| ObjectiveBody: Speak with Head Priest Ananda at the cou… | 36 | 34 | 7.8 | OVERLAPS 'ObjectiveTitle: CURRENT OBJECTIVE' |
Problems in this view: 4

#### UI table: ShrineCrime HUD @ requested 1920x1080 (actual 1920x1080); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=12
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| ObjectiveTitle: CURRENT OBJECTIVE | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics Recovered: 0/3  /  Clues F… | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
| ObjectiveBody: Speak with Head Priest Ananda at the cou… | 36 | 34 | 7.8 | OVERLAPS 'ObjectiveTitle: CURRENT OBJECTIVE' |
Problems in this view: 4

#### UI table: ShrineCrime HUD @ requested 2560x1440 (actual 2560x1440); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=12
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| ObjectiveTitle: CURRENT OBJECTIVE | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics Recovered: 0/3  /  Clues F… | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
| ObjectiveBody: Speak with Head Priest Ananda at the cou… | 36 | 34 | 7.8 | OVERLAPS 'ObjectiveTitle: CURRENT OBJECTIVE' |
Problems in this view: 4

## Scene: PuzzleWall
Animated roots: Characters x2; 
Visible priest roots: 0 (skinned renderers 0)

### Animator: Characters
controller=PriestAnimator, avatar=NONE, isHuman=False, avatarValid=False, applyRootMotion=False
clips: Priest_Rig|Priest_Rig|Priest_Rig|Priest_Idle, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk, Priest_Rig|Priest_Rig|Priest_Rig|Priest_Point
- idle: clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.15..0.15m armsRaised=0% bones(L/R/hip/arm)=1111 => OK
- talk(IsTalking=true): clips[Priest_Rig|Priest_Rig|Priest_Rig|Priest_Talk 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.15..0.15m armsRaised=0% bones(L/R/hip/arm)=1111 => OK

### Animator: Characters (player)
controller=CharacterAnimator, avatar=InvestigatorAvatar, isHuman=True, avatarValid=True, applyRootMotion=False
clips: mixamo.com

### UI: PuzzleWall HUD

#### UI table: PuzzleWall HUD @ requested 1280x720 (actual 1280x720); TMP texts=9; active IMGUI(OnGUI) scripts not measurable=11
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| PromptAction: Talk to Pujari Ananda | 36 | 34 | 7.8 |  |
| DialogueBody: My grandfather spoke of a wall that open… | 42 | 36 | 7.8 | OVERLAPS 'SpeakerName: Pujari Ananda' |
| ObjectiveTitle: Speak with Head Priest Ananda | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5 | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Head Priest Ananda stands at the courtya… | 36 | 34 | 7.8 | OVERFLOW(text>rect), OVERLAPS 'ObjectiveTitle: Speak with Head Priest Ananda', OVERLAPS 'ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5' |
| DialoguePrompt: [Space / Enter / E to advance] | 34 | 36 | 5.4 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| KeyText: E | 42 | 34 | 8.0 |  |
| SpeakerName: Pujari Ananda | 47 | 36 | 5.5 | LOW-CONTRAST, OVERFLOW(text>rect) |
| PortraitInitial: P | 83 | 36 | 8.6 |  |
Problems in this view: 6

#### UI table: PuzzleWall HUD @ requested 1920x1080 (actual 1920x1080); TMP texts=9; active IMGUI(OnGUI) scripts not measurable=11
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| PromptAction: Talk to Pujari Ananda | 36 | 34 | 7.8 |  |
| DialogueBody: My grandfather spoke of a wall that open… | 42 | 36 | 7.8 | OVERLAPS 'SpeakerName: Pujari Ananda' |
| ObjectiveTitle: Speak with Head Priest Ananda | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5 | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Head Priest Ananda stands at the courtya… | 36 | 34 | 7.8 | OVERFLOW(text>rect), OVERLAPS 'ObjectiveTitle: Speak with Head Priest Ananda', OVERLAPS 'ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5' |
| DialoguePrompt: [Space / Enter / E to advance] | 34 | 36 | 5.4 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| KeyText: E | 42 | 34 | 8.0 |  |
| SpeakerName: Pujari Ananda | 47 | 36 | 5.5 | LOW-CONTRAST, OVERFLOW(text>rect) |
| PortraitInitial: P | 83 | 36 | 8.6 |  |
Problems in this view: 6

#### UI table: PuzzleWall HUD @ requested 2560x1440 (actual 2560x1440); TMP texts=9; active IMGUI(OnGUI) scripts not measurable=11
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| PromptAction: Talk to Pujari Ananda | 36 | 34 | 7.8 |  |
| DialogueBody: My grandfather spoke of a wall that open… | 42 | 36 | 7.8 | OVERLAPS 'SpeakerName: Pujari Ananda' |
| ObjectiveTitle: Speak with Head Priest Ananda | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5 | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Head Priest Ananda stands at the courtya… | 36 | 34 | 7.8 | OVERFLOW(text>rect), OVERLAPS 'ObjectiveTitle: Speak with Head Priest Ananda', OVERLAPS 'ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5' |
| DialoguePrompt: [Space / Enter / E to advance] | 34 | 36 | 5.4 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| KeyText: E | 42 | 34 | 8.0 |  |
| SpeakerName: Pujari Ananda | 47 | 36 | 5.5 | LOW-CONTRAST, OVERFLOW(text>rect) |
| PortraitInitial: P | 83 | 36 | 8.6 |  |
Problems in this view: 6

## Scene: JunglePath
Animated roots: Characters x3; 
Visible priest roots: 0 (skinned renderers 0)

### Animator: Characters
controller=Robber1Animator, avatar=NONE, isHuman=False, avatarValid=False, applyRootMotion=False
clips: Robber1_Rig|Robber1_Idle, Robber1_Rig|Robber1_Walk, Robber1_Rig|Robber1_Threaten
- idle: clips[Robber1_Rig|Robber1_Idle 100%, ] Speed=- Grounded=- Crouch=- IsMoving=F bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.06..0.06m armsRaised=0% bones(L/R/hip/arm)=1111 => OK
talk: NO IsTalking parameter
- move(IsMoving=true): clips[Robber1_Rig|Robber1_Walk 100%, ] Speed=- Grounded=- Crouch=- IsMoving=T bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.06..0.06m armsRaised=0% bones(L/R/hip/arm)=1111 => OK

### Animator: Characters (player)
controller=CharacterAnimator, avatar=InvestigatorAvatar, isHuman=True, avatarValid=True, applyRootMotion=False
clips: mixamo.com

### Animator: Characters
controller=Robber2Animator, avatar=NONE, isHuman=False, avatarValid=False, applyRootMotion=False
clips: Robber2_Rig|Robber2_Idle, Robber2_Rig|Robber2_Stealth, Robber2_Rig|Robber2_Cornered
- idle: clips[Robber2_Rig|Robber2_Idle 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.08..0.08m armsRaised=0% bones(L/R/hip/arm)=1111 => OK
talk: NO IsTalking parameter
- move(IsStealth=true): clips[Robber2_Rig|Robber2_Stealth 100%, ] Speed=- Grounded=- Crouch=- IsMoving=- bodySpeed=0.00m/s footPlant10%L/R=0.00/0.00m/s hipDrift=0.00m footAboveGround=0.08..0.08m armsRaised=0% bones(L/R/hip/arm)=1111 => OK

### UI: JunglePath HUD

#### UI table: JunglePath HUD @ requested 1280x720 (actual 1280x720); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=15
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
| ObjectiveTitle: Speak with Head Priest Ananda | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5 | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Head Priest Ananda stands at the courtya… | 36 | 34 | 7.8 | OVERFLOW(text>rect), OVERLAPS 'ObjectiveTitle: Speak with Head Priest Ananda', OVERLAPS 'ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5' |
Problems in this view: 4

#### UI table: JunglePath HUD @ requested 1920x1080 (actual 1920x1080); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=15
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
| ObjectiveTitle: Speak with Head Priest Ananda | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5 | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Head Priest Ananda stands at the courtya… | 36 | 34 | 7.8 | OVERFLOW(text>rect), OVERLAPS 'ObjectiveTitle: Speak with Head Priest Ananda', OVERLAPS 'ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5' |
Problems in this view: 4

#### UI table: JunglePath HUD @ requested 2560x1440 (actual 2560x1440); TMP texts=4; active IMGUI(OnGUI) scripts not measurable=15
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| HintText: [W A S D] Move  •  [Shift] Sprint  •  [S… | 25 | 32 | 7.8 | SMALL |
| ObjectiveTitle: Speak with Head Priest Ananda | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW(text>rect) |
| ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5 | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| ObjectiveBody: Head Priest Ananda stands at the courtya… | 36 | 34 | 7.8 | OVERFLOW(text>rect), OVERLAPS 'ObjectiveTitle: Speak with Head Priest Ananda', OVERLAPS 'ObjectiveCounter: Stolen Relics: 0/3  /  Clues: 0/5' |
Problems in this view: 4
