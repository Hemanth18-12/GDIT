# Legacy of Temples

A third-person 3D web game set across ancient Indian heritage temples, powered by **Vite** and **Three.js** (plain JavaScript ES modules).

---

## 🏛️ Project Architecture

The engine is built around a decoupled architecture where systems communicate via an asynchronous/synchronous `EventBus` and adhere to strict module contracts.

```
src/
├── core/
│   ├── constants.js       # Scene IDs, Character IDs, Bus events
│   └── EventBus.js        # Event publish/subscribe bus (on, off, emit)
├── world/
│   └── index.js           # World environment, scene loader, spawns, interactables
├── characters/
│   └── index.js           # Player and NPC management, movement, spawn controller
├── gameplay/
│   └── index.js           # Game progression, interaction resolution, state machine
├── ui/
│   └── index.js           # HUD, prompts, dialogue overlay, status bar
└── main.js                # Bootstrap: Three.js renderer/camera/scene/clock + update loop
```

---

## 📜 Module Contracts

### 1. World Module (`src/world/index.js`)
```javascript
export async function initWorld({ scene, camera, renderer, bus }) -> {
  update(dt),
  loadScene(id),
  getSpawn(name),
  colliders: [],
  registerInteractable({ id, object3d, radius, prompt }),
  interactables: []
}
```

### 2. Characters Module (`src/characters/index.js`)
```javascript
export async function initCharacters({ scene, bus, world }) -> {
  player,
  npcs: {},
  update(dt),
  spawn(id, position)
}
```

### 3. Gameplay Module (`src/gameplay/index.js`)
```javascript
export async function initGameplay({ scene, bus, world, characters }) -> {
  update(dt),
  start()
}
```

---

## 🔑 Identifiers & Events

### Scene IDs
- `courtyard` - Ancient temple entrance and courtyards
- `sanctum` - Inner sanctum (Garbhagriha) where the relic was held
- `puzzle_wall` - Inscribed stone puzzle barrier
- `shrine_crime` - Crime scene showing marks of relic theft
- `jungle_path` - Dense forest escape route used by perpetrators

### Character IDs
- `investigator` - Player character investigating the theft
- `robber1` - Primary suspect fleeing with temple treasures
- `robber2` - Accomplice assisting the getaway
- `priest` - Temple guardian providing crucial testimony
- `villager` - Eyewitness from the surrounding settlement

### Bus Events
- `scene:changed` - Emitted when world transitions between scenes
- `player:interact` - Emitted when player presses interaction key [E]
- `clue:found` - Emitted upon discovering temple evidence or artifacts
- `puzzle:solved` - Emitted when temple architectural puzzles are unlocked
- `dialogue:start` - Emitted to open dialogues with NPCs or inspectables
- `dialogue:end` - Emitted when conversations terminate
- `idol:recovered` - Emitted when the sacred stolen idol is retrieved
- `game:state` - Emitted on overarching gameplay phase transitions

---

## 🎮 Controls

- **W / A / S / D** or **Arrow Keys**: Move investigator
- **E**: Interact / Talk / Inspect relics / Advance dialogue

---

## 🚀 Getting Started

### Prerequisites
- Node.js (v18+)
- npm (v9+)

### Installation
```bash
npm install
```

### Development Server
```bash
npm run dev
```

### Production Build
```bash
npm run build
```

### Preview Production Build
```bash
npm run preview
```

