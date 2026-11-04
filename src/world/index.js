import * as THREE from 'three';
import { SCENE_IDS, EVENTS } from '../core/constants.js';

/**
 * Initializes the 3D world environment, scene loading, colliders, and interactables.
 *
 * CONTRACT:
 * export async function initWorld({scene, camera, renderer, bus}) ->
 *   { update(dt), loadScene(id), getSpawn(name), colliders:[], registerInteractable({id,object3d,radius,prompt}), interactables:[] }
 */
export async function initWorld({ scene, camera, renderer, bus }) {
  const colliders = [];
  const interactables = [];
  let currentSceneId = null;

  // Scene root container to swap environment contents cleanly
  const worldGroup = new THREE.Group();
  worldGroup.name = 'WorldGroup';
  scene.add(worldGroup);

  // Setup ambient and directional lighting
  const ambientLight = new THREE.AmbientLight(0xfff1dc, 0.7);
  ambientLight.name = 'AmbientLight';
  scene.add(ambientLight);

  const sunLight = new THREE.DirectionalLight(0xffecc2, 1.2);
  sunLight.name = 'SunLight';
  sunLight.position.set(25, 40, 20);
  sunLight.castShadow = true;
  sunLight.shadow.mapSize.width = 2048;
  sunLight.shadow.mapSize.height = 2048;
  sunLight.shadow.camera.near = 0.5;
  sunLight.shadow.camera.far = 150;
  const shadowDist = 30;
  sunLight.shadow.camera.left = -shadowDist;
  sunLight.shadow.camera.right = shadowDist;
  sunLight.shadow.camera.top = shadowDist;
  sunLight.shadow.camera.bottom = -shadowDist;
  scene.add(sunLight);

  // Scene spawn points
  const spawnPoints = {
    courtyard: {
      player: new THREE.Vector3(0, 0, 8),
      investigator: new THREE.Vector3(0, 0, 8),
      priest: new THREE.Vector3(0, 0, -4),
      villager: new THREE.Vector3(-6, 0, 2),
      default: new THREE.Vector3(0, 0, 0)
    },
    sanctum: {
      player: new THREE.Vector3(0, 0, 10),
      investigator: new THREE.Vector3(0, 0, 10),
      priest: new THREE.Vector3(0, 0, -2),
      robber1: new THREE.Vector3(-3, 0, -5),
      robber2: new THREE.Vector3(3, 0, -5),
      default: new THREE.Vector3(0, 0, 0)
    },
    puzzle_wall: {
      player: new THREE.Vector3(0, 0, 5),
      investigator: new THREE.Vector3(0, 0, 5),
      default: new THREE.Vector3(0, 0, 0)
    },
    shrine_crime: {
      player: new THREE.Vector3(0, 0, 7),
      investigator: new THREE.Vector3(0, 0, 7),
      villager: new THREE.Vector3(4, 0, 3),
      default: new THREE.Vector3(0, 0, 0)
    },
    jungle_path: {
      player: new THREE.Vector3(0, 0, 15),
      investigator: new THREE.Vector3(0, 0, 15),
      robber1: new THREE.Vector3(-2, 0, -10),
      robber2: new THREE.Vector3(2, 0, -12),
      default: new THREE.Vector3(0, 0, 0)
    }
  };

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
   * Get designated spawn position by name for current scene.
   */
  function getSpawn(name = 'default') {
    const currentSceneSpawns = (currentSceneId && spawnPoints[currentSceneId]) || spawnPoints.courtyard;
    if (currentSceneSpawns && currentSceneSpawns[name]) {
      return currentSceneSpawns[name].clone();
    }
    return new THREE.Vector3(0, 0, 0);
  }

  /**
   * Load or transition into a scene id.
   */
  async function loadScene(id) {
    if (!Object.values(SCENE_IDS).includes(id)) {
      console.warn(`[World] Unknown scene ID: "${id}". Defaulting to courtyard.`);
      id = SCENE_IDS.COURTYARD;
    }

    currentSceneId = id;

    // Clear previous world group items
    while (worldGroup.children.length > 0) {
      const child = worldGroup.children[0];
      worldGroup.remove(child);
      if (child.geometry) child.geometry.dispose();
      if (child.material) {
        if (Array.isArray(child.material)) {
          child.material.forEach((m) => m.dispose());
        } else {
          child.material.dispose();
        }
      }
    }

    // Reset colliders and interactables lists
    colliders.length = 0;
    interactables.length = 0;

    // Ground platform
    const floorGeo = new THREE.PlaneGeometry(60, 60);
    const floorMat = new THREE.MeshStandardMaterial({
      color: id === SCENE_IDS.JUNGLE_PATH ? 0x2d3a24 : 0x4a433a,
      roughness: 0.85,
      metalness: 0.1
    });
    const floorMesh = new THREE.Mesh(floorGeo, floorMat);
    floorMesh.rotation.x = -Math.PI / 2;
    floorMesh.receiveShadow = true;
    floorMesh.name = `Floor_${id}`;
    worldGroup.add(floorMesh);
    colliders.push(floorMesh);

    // Decorative scene pillars / architectural monuments
    const pillarGeo = new THREE.CylinderGeometry(0.8, 1.0, 7, 16);
    const pillarMat = new THREE.MeshStandardMaterial({
      color: 0x7c7263,
      roughness: 0.7
    });

    const pillarCoords = [
      [-8, 3.5, -8],
      [8, 3.5, -8],
      [-8, 3.5, 8],
      [8, 3.5, 8]
    ];

    pillarCoords.forEach(([px, py, pz], index) => {
      const pillar = new THREE.Mesh(pillarGeo, pillarMat);
      pillar.position.set(px, py, pz);
      pillar.castShadow = true;
      pillar.receiveShadow = true;
      pillar.name = `Pillar_${index}`;
      worldGroup.add(pillar);
      colliders.push(pillar);
    });

    // Central altar or focal interactable
    const altarGeo = new THREE.BoxGeometry(2.5, 1.2, 2.5);
    const altarMat = new THREE.MeshStandardMaterial({
      color: 0xb59458,
      roughness: 0.4,
      metalness: 0.2
    });
    const altar = new THREE.Mesh(altarGeo, altarMat);
    altar.position.set(0, 0.6, 0);
    altar.castShadow = true;
    altar.receiveShadow = true;
    altar.name = `Altar_${id}`;
    worldGroup.add(altar);
    colliders.push(altar);

    registerInteractable({
      id: `interactable_${id}_core`,
      object3d: altar,
      radius: 3.5,
      prompt: `Inspect ancient altar in ${id}`
    });

    // Grid helper for spatial orientation
    const grid = new THREE.GridHelper(60, 30, 0xd4af37, 0x333333);
    grid.position.y = 0.01;
    worldGroup.add(grid);

    console.log(`[World] Loaded scene "${id}".`);
    bus.emit(EVENTS.SCENE_CHANGED, { sceneId: id });
  }

  /**
   * Per-frame world update
   */
  function update(dt) {
    // Environmental animation or dynamic updates
    const altar = worldGroup.getObjectByName(`Altar_${currentSceneId}`);
    if (altar) {
      altar.rotation.y += 0.2 * dt;
    }
  }

  // Initial load
  await loadScene(SCENE_IDS.COURTYARD);

  return {
    update,
    loadScene,
    getSpawn,
    colliders,
    registerInteractable,
    interactables
  };
}

export default initWorld;

