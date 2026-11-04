import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { SCENE_IDS, EVENTS } from '../core/constants.js';
import CollisionSystem from './collision.js';
import quality from './quality.js';
import overlay from './overlay.js';
import { createCourtyardScene } from './scenes/courtyard.js';
import { createSanctumScene } from './scenes/sanctum.js';
import { createPuzzleWallScene } from './scenes/puzzle_wall.js';
import { createShrineCrimeScene } from './scenes/shrine_crime.js';
import { createJunglePathScene } from './scenes/jungle_path.js';

const SCENE_BUILDERS = {
  [SCENE_IDS.COURTYARD]: { title: 'Temple Courtyard (Sunrise)', build: createCourtyardScene },
  [SCENE_IDS.SANCTUM]: { title: 'Inner Sanctum (Garbhagriha)', build: createSanctumScene },
  [SCENE_IDS.PUZZLE_WALL]: { title: 'Ancient Puzzle Wall', build: createPuzzleWallScene },
  [SCENE_IDS.SHRINE_CRIME]: { title: 'Ransacked Shrine (Crime Scene)', build: createShrineCrimeScene },
  [SCENE_IDS.JUNGLE_PATH]: { title: 'Moonlit Jungle Path', build: createJunglePathScene }
};

/**
 * Initializes the 3D world environment, scene loading, colliders, and interactables.
 *
 * CONTRACT:
 * export async function initWorld({scene, camera, renderer, bus}) ->
 *   { update(dt), loadScene(id), getSpawn(name), colliders:[], registerInteractable({id,object3d,radius,prompt}), interactables:[] }
 */
export async function initWorld({ scene, camera, renderer, bus }) {
  const collision = new CollisionSystem();
  const interactables = [];
  const activeSceneLights = [];

  let currentSceneId = null;
  let currentSceneData = null;
  let elapsedTime = 0;

  // Root container for swapping environment contents cleanly
  const worldGroup = new THREE.Group();
  worldGroup.name = 'WorldGroup';
  scene.add(worldGroup);

  // Initialize quality settings for 60 FPS target
  quality.init(renderer);

  const gltfLoader = new GLTFLoader();

  /**
   * Handle window resize
   */
  function handleResize() {
    if (camera && renderer) {
      camera.aspect = window.innerWidth / window.innerHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
      quality.apply();
    }
  }
  window.addEventListener('resize', handleResize);

  /**
   * Register an interactable item in the world.
   */
  function registerInteractable({ id, object3d, radius = 2.5, prompt = 'Press [E] to Interact' }) {
    const existingIndex = interactables.findIndex((item) => item.id === id);
    const item = { id, object3d, radius, prompt };
    if (existingIndex >= 0) {
      interactables[existingIndex] = item;
    } else {
      interactables.push(item);
    }
    return item;
  }

  /**
   * Check if a custom GLB model exists in public/models/env/
   */
  async function tryLoadCustomGLB(sceneId) {
    const glbUrl = `/models/env/${sceneId}.glb`;
    try {
      const res = await fetch(glbUrl, { method: 'HEAD' });
      const contentType = res.headers.get('content-type') || '';
      if (res.ok && !contentType.includes('text/html')) {
        return new Promise((resolve) => {
          gltfLoader.load(
            glbUrl,
            (gltf) => {
              console.log(`[World] Loaded external GLB model for ${sceneId}`);
              resolve(gltf.scene);
            },
            undefined,
            () => resolve(null)
          );
        });
      }
    } catch {
      // Fallback cleanly to procedural architecture
    }
    return null;
  }

  /**
   * Deep cleanup of 3D objects, geometries, and materials
   */
  function disposeHierarchy(obj) {
    if (!obj) return;
    while (obj.children && obj.children.length > 0) {
      const child = obj.children[0];
      obj.remove(child);
      disposeHierarchy(child);
    }
    if (obj.geometry) {
      obj.geometry.dispose();
    }
    if (obj.material) {
      if (Array.isArray(obj.material)) {
        obj.material.forEach((m) => m && m.dispose && m.dispose());
      } else if (obj.material.dispose) {
        obj.material.dispose();
      }
    }
  }

  /**
   * Unload previous environment and load new scene
   */
  async function loadScene(id) {
    if (!SCENE_BUILDERS[id]) {
      console.warn(`[World] Unknown scene ID "${id}". Defaulting to "${SCENE_IDS.COURTYARD}".`);
      id = SCENE_IDS.COURTYARD;
    }

    const sceneMeta = SCENE_BUILDERS[id];
    overlay.show(`Entering ${sceneMeta.title}...`);

    try {
      // 1. Unload previous scene objects
      disposeHierarchy(worldGroup);

      // Clean up previous scene lights
      activeSceneLights.forEach((light) => {
        if (light.parent) light.parent.remove(light);
        if (light.dispose) light.dispose();
      });
      activeSceneLights.length = 0;

      // Reset colliders and interactables
      collision.clear();
      interactables.length = 0;

      currentSceneId = id;

      // 2. Try loading custom GLB from public/models/env/
      const customModel = await tryLoadCustomGLB(id);
      if (customModel) {
        worldGroup.add(customModel);
        collision.add(customModel);
      }

      // 3. Build scene environment
      const sceneResult = await sceneMeta.build({
        group: worldGroup,
        collision,
        registerInteractable,
        lights: activeSceneLights
      });

      currentSceneData = sceneResult;

      // Apply atmospheric fog and background
      if (sceneResult.skyColor) {
        scene.background = sceneResult.skyColor;
      }
      if (sceneResult.fog) {
        scene.fog = sceneResult.fog;
      }

      console.log(`[World] Successfully loaded scene: "${id}" with ${activeSceneLights.length} lights.`);

      // 4. Emit scene:changed event
      bus.emit(EVENTS.SCENE_CHANGED, { sceneId: id });
    } catch (err) {
      console.error(`[World] Failed to load scene "${id}":`, err);
    } finally {
      // Hide loading overlay smoothly
      setTimeout(() => {
        overlay.hide();
      }, 350);
    }
  }

  /**
   * Named spawn point resolution supporting contract & character IDs:
   * "player_start" | "robber_spawn" | "priest_spawn" | "villager_spawn"
   */
  function getSpawn(name = 'player_start') {
    const defaultSpawns = {
      player_start: new THREE.Vector3(0, 0, 8),
      robber_spawn: new THREE.Vector3(0, 0, -8),
      priest_spawn: new THREE.Vector3(2, 0, 0),
      villager_spawn: new THREE.Vector3(-4, 0, 4),
      default: new THREE.Vector3(0, 0, 0)
    };

    const currentSpawns = (currentSceneData && currentSceneData.spawns) || defaultSpawns;

    // Normalization mapping for backward compatibility
    const aliasMap = {
      investigator: 'player_start',
      player: 'player_start',
      robber1: 'robber_spawn',
      robber2: 'robber_spawn',
      priest: 'priest_spawn',
      villager: 'villager_spawn'
    };

    const normalizedName = aliasMap[name] || name;

    if (currentSpawns[normalizedName]) {
      return currentSpawns[normalizedName].clone();
    }
    if (currentSpawns[name]) {
      return currentSpawns[name].clone();
    }
    if (currentSpawns.player_start) {
      return currentSpawns.player_start.clone();
    }
    return defaultSpawns.default.clone();
  }

  /**
   * Per-frame world update loop
   */
  function update(dt) {
    elapsedTime += dt;

    // Update scene animations
    if (currentSceneData && currentSceneData.update) {
      currentSceneData.update(dt, elapsedTime);
    }

    // Resolve player collisions against walls and obstacles
    const playerObj = scene.getObjectByName('Char_investigator');
    if (playerObj && playerObj.position) {
      collision.resolve(playerObj.position);
    }
  }

  /**
   * Dev-only keys 1-5 to switch scenes
   */
  window.addEventListener('keydown', (e) => {
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;

    const devKeyMap = {
      '1': SCENE_IDS.COURTYARD,
      '2': SCENE_IDS.SANCTUM,
      '3': SCENE_IDS.PUZZLE_WALL,
      '4': SCENE_IDS.SHRINE_CRIME,
      '5': SCENE_IDS.JUNGLE_PATH
    };

    if (devKeyMap[e.key]) {
      console.log(`[Dev] Switching scene via key "${e.key}" to "${devKeyMap[e.key]}"`);
      loadScene(devKeyMap[e.key]);
    }
  });

  // Initial load: courtyard
  await loadScene(SCENE_IDS.COURTYARD);

  return {
    update,
    loadScene,
    getSpawn,
    colliders: collision.colliders,
    registerInteractable,
    interactables
  };
}

export default initWorld;
