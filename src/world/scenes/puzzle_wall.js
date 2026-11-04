import * as THREE from 'three';
import { getReliefTexture, getParchmentTexture, getSandstoneTexture } from '../textures.js';
import { buildPillar, getArchitectureMaterials } from '../architecture.js';

export async function createPuzzleWallScene({ group, collision, registerInteractable, lights }) {
  const mats = getArchitectureMaterials();

  // 1. Atmosphere: Mystic Stone Vault
  const skyColor = new THREE.Color(0x10151c);
  const fog = new THREE.FogExp2(0x121720, 0.025);

  // 2. Lighting (Total: 1 Ambient + 1 Directional Accent + 2 Torches = 4 lights)
  const ambLight = new THREE.AmbientLight(0x232d3b, 0.7);
  group.add(ambLight);
  lights.push(ambLight);

  const keyLight = new THREE.DirectionalLight(0xffdfba, 1.0);
  keyLight.position.set(0, 10, 12);
  keyLight.target.position.set(0, 3, -4);
  keyLight.castShadow = true;
  group.add(keyLight);
  group.add(keyLight.target);
  lights.push(keyLight);

  // 3. Chamber Floor and Walls
  const floorGeo = new THREE.PlaneGeometry(24, 24);
  const floorMat = new THREE.MeshStandardMaterial({
    map: getSandstoneTexture(),
    roughness: 0.8,
    metalness: 0.1
  });
  const floor = new THREE.Mesh(floorGeo, floorMat);
  floor.rotation.x = -Math.PI / 2;
  floor.receiveShadow = true;
  group.add(floor);

  // 4. Grand Carved Puzzle Wall Structure (Z = -4)
  const wallGroup = new THREE.Group();
  wallGroup.name = 'PuzzleWallStructure';
  wallGroup.position.set(0, 0, -4);

  const mainWallGeo = new THREE.BoxGeometry(18, 9, 2);
  const mainWall = new THREE.Mesh(mainWallGeo, mats.sandstone);
  mainWall.position.y = 4.5;
  mainWall.castShadow = true;
  mainWall.receiveShadow = true;
  wallGroup.add(mainWall);
  collision.addBox(0, 4.5, -4, 18, 9, 2.2);

  // Perimeter collision for room
  collision.addPerimeter(10, 10, 8);

  // Side flanking pillars
  const pilL = buildPillar({ height: 8.5, radius: 0.55 });
  pilL.position.set(-8, 0, 0);
  wallGroup.add(pilL);

  const pilR = buildPillar({ height: 8.5, radius: 0.55 });
  pilR.position.set(8, 0, 0);
  wallGroup.add(pilR);

  // 5. Central Rotating Lotus / Chakra Wheel
  const wheelGroup = new THREE.Group();
  wheelGroup.name = 'LotusChakraWheel';
  wheelGroup.position.set(0, 4.6, 1.05);

  // Outer hub rim
  const rimGeo = new THREE.TorusGeometry(1.6, 0.16, 16, 32);
  const rim = new THREE.Mesh(rimGeo, mats.brass);
  rim.castShadow = true;
  wheelGroup.add(rim);

  // Inner carved disc
  const discGeo = new THREE.CylinderGeometry(1.4, 1.4, 0.14, 24);
  const discMat = new THREE.MeshStandardMaterial({
    map: getReliefTexture('wheel'),
    roughness: 0.5,
    metalness: 0.4
  });
  const disc = new THREE.Mesh(discGeo, discMat);
  disc.rotation.x = Math.PI / 2;
  disc.castShadow = true;
  wheelGroup.add(disc);

  // Center lotus boss / handle
  const centerBossGeo = new THREE.SphereGeometry(0.35, 12, 12);
  const centerBoss = new THREE.Mesh(centerBossGeo, mats.brass);
  centerBoss.position.z = 0.12;
  wheelGroup.add(centerBoss);

  wallGroup.add(wheelGroup);

  // Register Wheel Center Interactable
  registerInteractable({
    id: 'wheel_center',
    object3d: wheelGroup,
    radius: 3.2,
    prompt: 'Rotate the ancient Chakra Wheel'
  });

  // 6. Relief Panels (Lotus, Conch, Wheel, Kalash)
  const panelGeo = new THREE.BoxGeometry(2.2, 2.2, 0.2);
  const panelConfig = [
    { id: 'panel_lotus', type: 'lotus', x: -4.5, y: 5.8, prompt: 'Inspect Lotus relief panel' },
    { id: 'panel_conch', type: 'conch', x: -4.5, y: 3.0, prompt: 'Inspect Conch relief panel' },
    { id: 'panel_wheel', type: 'wheel', x: 4.5, y: 5.8, prompt: 'Inspect Wheel relief panel' },
    { id: 'panel_kalash', type: 'kalash', x: 4.5, y: 3.0, prompt: 'Inspect Kalash relief panel' }
  ];

  panelConfig.forEach(({ id, type, x, y, prompt }) => {
    const mat = new THREE.MeshStandardMaterial({
      map: getReliefTexture(type),
      roughness: 0.6,
      metalness: 0.2
    });
    const panel = new THREE.Mesh(panelGeo, mat);
    panel.position.set(x, y, 1.05);
    panel.castShadow = true;
    panel.receiveShadow = true;
    panel.name = `Relief_${id}`;
    wallGroup.add(panel);

    registerInteractable({
      id,
      object3d: panel,
      radius: 3.0,
      prompt
    });
  });

  // 7. Stone Ledge with Ancient Scroll
  const ledgeGeo = new THREE.BoxGeometry(3.6, 0.35, 1.2);
  const ledge = new THREE.Mesh(ledgeGeo, mats.sandstone);
  ledge.position.set(0, 1.8, 0.8);
  ledge.castShadow = true;
  ledge.receiveShadow = true;
  wallGroup.add(ledge);
  collision.addBox(0, 1.8, -3.2, 3.6, 0.4, 1.2);

  // Scroll mesh
  const scrollGroup = new THREE.Group();
  scrollGroup.name = 'ParchmentScroll';
  scrollGroup.position.set(0, 2.05, 0.8);

  const scrollRollGeo = new THREE.CylinderGeometry(0.12, 0.12, 1.8, 12);
  const scrollMat = new THREE.MeshStandardMaterial({
    map: getParchmentTexture(),
    roughness: 0.85
  });
  const scrollRoll1 = new THREE.Mesh(scrollRollGeo, scrollMat);
  scrollRoll1.rotation.z = Math.PI / 2;
  scrollRoll1.position.z = -0.2;
  scrollGroup.add(scrollRoll1);

  const scrollUnrolled = new THREE.Mesh(new THREE.PlaneGeometry(1.6, 0.5), scrollMat);
  scrollUnrolled.rotation.x = -Math.PI / 2;
  scrollGroup.add(scrollUnrolled);

  const scrollRoll2 = new THREE.Mesh(scrollRollGeo, scrollMat);
  scrollRoll2.rotation.z = Math.PI / 2;
  scrollRoll2.position.z = 0.2;
  scrollGroup.add(scrollRoll2);

  wallGroup.add(scrollGroup);

  registerInteractable({
    id: 'scroll',
    object3d: scrollGroup,
    radius: 3.0,
    prompt: 'Read ancient temple cipher scroll'
  });

  group.add(wallGroup);

  // 8. Warm Torch Sconces flanking puzzle wall
  const torchL = new THREE.PointLight(0xff9922, 1.6, 12, 1.8);
  torchL.position.set(-6.5, 4.5, -2.5);
  torchL.castShadow = true;
  group.add(torchL);
  lights.push(torchL);

  const torchR = new THREE.PointLight(0xff9922, 1.6, 12, 1.8);
  torchR.position.set(6.5, 4.5, -2.5);
  torchR.castShadow = true;
  group.add(torchR);
  lights.push(torchR);

  // Spawn points for puzzle wall
  const spawns = {
    player_start: new THREE.Vector3(0, 0, 5),
    investigator: new THREE.Vector3(0, 0, 5),
    player: new THREE.Vector3(0, 0, 5),
    priest_spawn: new THREE.Vector3(-4, 0, 4),
    priest: new THREE.Vector3(-4, 0, 4),
    robber_spawn: new THREE.Vector3(4, 0, 1),
    robber1: new THREE.Vector3(4, 0, 1),
    robber2: new THREE.Vector3(5, 0, 2),
    villager_spawn: new THREE.Vector3(0, 0, 8),
    villager: new THREE.Vector3(0, 0, 8),
    default: new THREE.Vector3(0, 0, 5)
  };

  let wheelRotationSpeed = 0.15;

  return {
    fog,
    skyColor,
    spawns,
    update: (dt, time) => {
      // Gentle mystical rotation of chakra wheel
      wheelGroup.rotation.z += wheelRotationSpeed * dt;

      // Torch flickers
      torchL.intensity = 1.5 + Math.sin(time * 11) * 0.25;
      torchR.intensity = 1.5 + Math.cos(time * 13) * 0.25;
    }
  };
}
