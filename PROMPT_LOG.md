# Prompt Log

## P0 - Project Scaffold & Core Contracts
- **Date**: 2026-11-04
- **Summary**: Initialized the "Legacy of Temples" Vite + Three.js project with modular ES module architecture, strict interface contracts, EventBus communication, and working functional stubs.
- **Files Created**:
  - `package.json`
  - `vite.config.js`
  - `index.html`
  - `.gitignore`
  - `README.md`
  - `PROMPT_LOG.md`
  - `public/models/.gitkeep`
  - `public/audio/.gitkeep`
  - `src/main.js`
  - `src/core/EventBus.js`
  - `src/core/constants.js`
  - `src/world/index.js`
  - `src/characters/index.js`
  - `src/gameplay/index.js`
  - `src/ui/index.js`
- **Verification**: Ran `npm install` and `npm run build` to confirm clean compilation and asset bundling without errors.

## P1-v1 - World Environments & Contracts Implementation
- **Date**: 2026-11-04
- **Summary**: Implemented 5 complete Odisha-style 3D temple environments (`courtyard`, `sanctum`, `puzzle_wall`, `shrine_crime`, `jungle_path`) loadable via `loadScene(id)`. Integrated procedural textures (Khondalite sandstone, wet flagstones, relief friezes, red silk, jungle ground, parchment), AABB collision resolution for walls and obstacles, quality manager targeting 60 FPS (with light counts <= 8), animated loading overlay, named spawn point resolution, and dev keys 1-5 for instant scene switching.
- **Files Created**:
  - `public/models/env/.gitkeep`
  - `src/world/textures.js`
  - `src/world/collision.js`
  - `src/world/quality.js`
  - `src/world/overlay.js`
  - `src/world/architecture.js`
  - `src/world/scenes/courtyard.js`
  - `src/world/scenes/sanctum.js`
  - `src/world/scenes/puzzle_wall.js`
  - `src/world/scenes/shrine_crime.js`
  - `src/world/scenes/jungle_path.js`
- **Files Modified**:
  - `src/world/index.js`
- **Known Issues**:
  - None. Procedural geometries and canvas textures load instantly with zero external network dependencies, while optionally supporting custom GLB models in `public/models/env/` when provided.


