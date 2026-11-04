import * as THREE from 'three';
import { CHARACTER_IDS, EVENTS } from '../core/constants.js';

/**
 * Character palette & attributes
 */
const CHARACTER_META = {
  [CHARACTER_IDS.INVESTIGATOR]: { name: 'Investigator', color: 0x2b4c7e, accent: 0xf5d061, height: 1.8 },
  [CHARACTER_IDS.ROBBER1]: { name: 'Robber 1', color: 0x6e2626, accent: 0x111111, height: 1.85 },
  [CHARACTER_IDS.ROBBER2]: { name: 'Robber 2', color: 0x4a1e2a, accent: 0x222222, height: 1.75 },
  [CHARACTER_IDS.PRIEST]: { name: 'Priest', color: 0xd97724, accent: 0xf3ede2, height: 1.7 },
  [CHARACTER_IDS.VILLAGER]: { name: 'Villager', color: 0x2d6a4f, accent: 0xd8f3dc, height: 1.65 }
};

/**
 * Initializes the characters system (player and NPCs).
 *
 * CONTRACT:
 * export async function initCharacters({scene, bus, world}) ->
 *   { player, npcs:{}, update(dt), spawn(id, position) }
 */
export async function initCharacters({ scene, bus, world }) {
  const npcs = {};
  const charactersGroup = new THREE.Group();
  charactersGroup.name = 'CharactersGroup';
  scene.add(charactersGroup);

  // Keyboard input state for third-person player control
  const keys = {
    KeyW: false,
    KeyA: false,
    KeyS: false,
    KeyD: false,
    ArrowUp: false,
    ArrowLeft: false,
    ArrowDown: false,
    ArrowRight: false,
    KeyE: false
  };

  window.addEventListener('keydown', (e) => {
    if (e.code in keys) {
      keys[e.code] = true;
    }
    if (e.code === 'KeyE') {
      bus.emit(EVENTS.PLAYER_INTERACT, {
        position: player.position.clone()
      });
    }
  });

  window.addEventListener('keyup', (e) => {
    if (e.code in keys) {
      keys[e.code] = false;
    }
  });

  /**
   * Builds a simple stylized character mesh
   */
  function createCharacterMesh(id) {
    const meta = CHARACTER_META[id] || { name: id, color: 0x888888, accent: 0xffffff, height: 1.8 };
    const group = new THREE.Group();
    group.name = `Char_${id}`;

    // Body
    const bodyGeo = new THREE.CapsuleGeometry(0.4, meta.height - 0.8, 8, 16);
    const bodyMat = new THREE.MeshStandardMaterial({
      color: meta.color,
      roughness: 0.6,
      metalness: 0.1
    });
    const body = new THREE.Mesh(bodyGeo, bodyMat);
    body.position.y = meta.height / 2;
    body.castShadow = true;
    body.receiveShadow = true;
    group.add(body);

    // Direction visor / eyes indicator
    const visorGeo = new THREE.BoxGeometry(0.45, 0.15, 0.3);
    const visorMat = new THREE.MeshStandardMaterial({
      color: meta.accent,
      emissive: meta.accent,
      emissiveIntensity: 0.3
    });
    const visor = new THREE.Mesh(visorGeo, visorMat);
    visor.position.set(0, meta.height * 0.75, 0.3);
    group.add(visor);

    return group;
  }

  /**
   * Spawns a character by ID at the given position.
   */
  function spawn(id, position) {
    const targetPos = position
      ? (position.isVector3 ? position.clone() : new THREE.Vector3(position.x || 0, position.y || 0, position.z || 0))
      : world.getSpawn(id);

    // If existing, remove previous mesh
    if (npcs[id] && npcs[id].mesh) {
      charactersGroup.remove(npcs[id].mesh);
    }

    const mesh = createCharacterMesh(id);
    mesh.position.copy(targetPos);
    charactersGroup.add(mesh);

    const charEntry = {
      id,
      mesh,
      position: mesh.position,
      rotation: mesh.rotation,
      meta: CHARACTER_META[id] || { name: id }
    };

    if (id !== CHARACTER_IDS.INVESTIGATOR) {
      npcs[id] = charEntry;
    }

    return charEntry;
  }

  // Spawn player (Investigator)
  const playerSpawnPos = world.getSpawn(CHARACTER_IDS.INVESTIGATOR);
  const playerMesh = createCharacterMesh(CHARACTER_IDS.INVESTIGATOR);
  playerMesh.position.copy(playerSpawnPos);
  charactersGroup.add(playerMesh);

  const player = {
    id: CHARACTER_IDS.INVESTIGATOR,
    mesh: playerMesh,
    position: playerMesh.position,
    rotation: playerMesh.rotation,
    speed: 6.0,
    velocity: new THREE.Vector3(),
    meta: CHARACTER_META[CHARACTER_IDS.INVESTIGATOR]
  };

  // Spawn initial NPCs in courtyard
  spawn(CHARACTER_IDS.PRIEST, world.getSpawn(CHARACTER_IDS.PRIEST));
  spawn(CHARACTER_IDS.VILLAGER, world.getSpawn(CHARACTER_IDS.VILLAGER));

  /**
   * Update character transforms and player input
   */
  function update(dt) {
    // Player movement
    const moveZ = (keys.KeyS || keys.ArrowDown ? 1 : 0) - (keys.KeyW || keys.ArrowUp ? 1 : 0);
    const moveX = (keys.KeyD || keys.ArrowRight ? 1 : 0) - (keys.KeyA || keys.ArrowLeft ? 1 : 0);

    const inputDir = new THREE.Vector3(moveX, 0, moveZ);
    if (inputDir.lengthSq() > 0) {
      inputDir.normalize();
      const moveDistance = player.speed * dt;
      player.position.x += inputDir.x * moveDistance;
      player.position.z += inputDir.z * moveDistance;

      // Rotate player smoothly towards movement direction
      const targetAngle = Math.atan2(inputDir.x, inputDir.z);
      player.mesh.rotation.y = targetAngle;
    }

    // NPC idle subtle breathing / animations
    const time = performance.now() * 0.002;
    Object.values(npcs).forEach((npc, index) => {
      if (npc.mesh) {
        npc.mesh.position.y = Math.sin(time + index) * 0.05;
      }
    });
  }

  // Listen to scene changes to re-position characters according to scene spawns
  bus.on(EVENTS.SCENE_CHANGED, ({ sceneId }) => {
    const newPos = world.getSpawn(CHARACTER_IDS.INVESTIGATOR);
    player.position.copy(newPos);
  });

  return {
    player,
    npcs,
    update,
    spawn
  };
}

export default initCharacters;

