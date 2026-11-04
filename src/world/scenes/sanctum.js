import * as THREE from 'three';
import { getSandstoneTexture, getRedSilkTexture } from '../textures.js';
import {
  buildPillar,
  buildBrassLamp,
  buildTempleBell,
  buildGarland,
  getArchitectureMaterials
} from '../architecture.js';

export async function createSanctumScene({ group, collision, registerInteractable, lights }) {
  const mats = getArchitectureMaterials();

  // 1. Atmosphere: Dark Main Sanctum Hall
  const skyColor = new THREE.Color(0x0c0907);
  const fog = new THREE.FogExp2(0x0e0906, 0.032);

  // 2. Lighting (Total: 1 Ambient + 1 Soft Shaft + 4 Brass Lamps = 6 lights)
  const ambLight = new THREE.AmbientLight(0x221711, 0.6);
  group.add(ambLight);
  lights.push(ambLight);

  const windowLight = new THREE.DirectionalLight(0xffb070, 0.8);
  windowLight.position.set(12, 18, -4);
  windowLight.castShadow = true;
  group.add(windowLight);
  lights.push(windowLight);

  // 3. Hall Floor & High Corbelled Ceiling
  const floorGeo = new THREE.PlaneGeometry(26, 36);
  const floorMat = new THREE.MeshStandardMaterial({
    map: getSandstoneTexture(),
    roughness: 0.75,
    metalness: 0.1
  });
  const floor = new THREE.Mesh(floorGeo, floorMat);
  floor.rotation.x = -Math.PI / 2;
  floor.receiveShadow = true;
  group.add(floor);

  // High Ceiling
  const ceiling = new THREE.Mesh(new THREE.PlaneGeometry(26, 36), mats.darkStone);
  ceiling.position.y = 8.5;
  ceiling.rotation.x = Math.PI / 2;
  group.add(ceiling);

  // Hall Walls
  const wallMat = mats.sandstone;
  // West Wall
  const wWall = new THREE.Mesh(new THREE.BoxGeometry(1.5, 9, 36), wallMat);
  wWall.position.set(-12, 4.5, 0);
  wWall.receiveShadow = true;
  group.add(wWall);
  collision.addBox(-12, 4.5, 0, 2, 9, 36);

  // East Wall
  const eWall = new THREE.Mesh(new THREE.BoxGeometry(1.5, 9, 36), wallMat);
  eWall.position.set(12, 4.5, 0);
  eWall.receiveShadow = true;
  group.add(eWall);
  collision.addBox(12, 4.5, 0, 2, 9, 36);

  // North (Back) Wall
  const nWall = new THREE.Mesh(new THREE.BoxGeometry(26, 9, 1.5), wallMat);
  nWall.position.set(0, 4.5, -17.5);
  nWall.receiveShadow = true;
  group.add(nWall);
  collision.addBox(0, 4.5, -17.5, 26, 9, 2);

  // South (Entrance) Wall with portal
  const sWallL = new THREE.Mesh(new THREE.BoxGeometry(9.5, 9, 1.5), wallMat);
  sWallL.position.set(-8, 4.5, 17.5);
  group.add(sWallL);
  collision.addBox(-8, 4.5, 17.5, 10, 9, 2);

  const sWallR = new THREE.Mesh(new THREE.BoxGeometry(9.5, 9, 1.5), wallMat);
  sWallR.position.set(8, 4.5, 17.5);
  group.add(sWallR);
  collision.addBox(8, 4.5, 17.5, 10, 9, 2);

  collision.addPerimeter(13, 18, 9);

  // 4. Carved Monolithic Pillars lining central nave
  const pillarZ = [-10, -4, 2, 8];
  pillarZ.forEach((pz) => {
    // West row
    const pL = buildPillar({ height: 8.5, radius: 0.6 });
    pL.position.set(-6, 0, pz);
    group.add(pL);
    collision.addBox(-6, 4.25, pz, 1.6, 8.5, 1.6);

    // East row
    const pR = buildPillar({ height: 8.5, radius: 0.6 });
    pR.position.set(6, 0, pz);
    group.add(pR);
    collision.addBox(6, 4.25, pz, 1.6, 8.5, 1.6);

    // Hanging brass bells from ceiling
    const bell = buildTempleBell({ chainLength: 2.8 });
    bell.position.set((Math.random() > 0.5 ? -3.5 : 3.5), 8.5, pz);
    group.add(bell);
  });

  // 5. Flower Garlands draped across pillars
  for (let g = 0; g < pillarZ.length - 1; g++) {
    const garlandL = buildGarland({ length: 6, sag: 0.9 });
    garlandL.position.set(-6, 6.2, (pillarZ[g] + pillarZ[g + 1]) / 2);
    garlandL.rotation.y = Math.PI / 2;
    group.add(garlandL);

    const garlandR = buildGarland({ length: 6, sag: 0.9 });
    garlandR.position.set(6, 6.2, (pillarZ[g] + pillarZ[g + 1]) / 2);
    garlandR.rotation.y = Math.PI / 2;
    group.add(garlandR);
  }

  // 6. Red Silk Curtains flanking sacred sanctum altar
  const curtainMat = mats.redFabric;
  const curtainGeo = new THREE.PlaneGeometry(3.5, 7, 8, 8);
  // Add gentle wave to curtain
  const posAttr = curtainGeo.attributes.position;
  for (let i = 0; i < posAttr.count; i++) {
    const x = posAttr.getX(i);
    posAttr.setZ(i, Math.sin(x * 3) * 0.25);
  }
  curtainGeo.computeVertexNormals();

  const curtainL = new THREE.Mesh(curtainGeo, curtainMat);
  curtainL.position.set(-4.5, 4.0, -12);
  curtainL.castShadow = true;
  group.add(curtainL);

  const curtainR = new THREE.Mesh(curtainGeo, curtainMat);
  curtainR.position.set(4.5, 4.0, -12);
  curtainR.castShadow = true;
  group.add(curtainR);

  // 7. Central Altar (Empty Idol Pedestal)
  const altarGroup = new THREE.Group();
  altarGroup.name = 'SanctumAltar';
  altarGroup.position.set(0, 0, -12);

  // Stepped black granite base
  const aBase1 = new THREE.Mesh(new THREE.BoxGeometry(4.2, 0.6, 3.8), mats.darkStone);
  aBase1.position.y = 0.3;
  aBase1.castShadow = true;
  aBase1.receiveShadow = true;
  altarGroup.add(aBase1);

  const aBase2 = new THREE.Mesh(new THREE.BoxGeometry(3.2, 0.5, 2.8), mats.sandstone);
  aBase2.position.y = 0.85;
  aBase2.castShadow = true;
  altarGroup.add(aBase2);

  // Carved lotus throne plinth
  const lotusBase = new THREE.Mesh(new THREE.CylinderGeometry(1.4, 1.2, 0.4, 16), mats.sandstone);
  lotusBase.position.y = 1.3;
  altarGroup.add(lotusBase);

  // Red velvet pedestal cloth
  const altarCloth = new THREE.Mesh(new THREE.BoxGeometry(2.2, 0.1, 1.8), mats.redFabric);
  altarCloth.position.y = 1.55;
  altarGroup.add(altarCloth);

  // Conspicuous EMPTY pedestal indentation / broken mounting pegs
  const pegGeo = new THREE.CylinderGeometry(0.08, 0.08, 0.2, 8);
  const brokenPeg1 = new THREE.Mesh(pegGeo, mats.brass);
  brokenPeg1.position.set(-0.5, 1.65, 0);
  altarGroup.add(brokenPeg1);

  const brokenPeg2 = new THREE.Mesh(pegGeo, mats.brass);
  brokenPeg2.position.set(0.5, 1.65, 0);
  altarGroup.add(brokenPeg2);

  group.add(altarGroup);
  collision.addBox(0, 0.9, -12, 4.4, 2.0, 4.0);

  registerInteractable({
    id: 'sanctum_empty_altar',
    object3d: altarGroup,
    radius: 3.8,
    prompt: 'Examine desecrated altar: sacred idol is missing!'
  });

  // Also register with generic altar id for gameplay contract compatibility
  registerInteractable({
    id: 'interactable_sanctum_core',
    object3d: altarGroup,
    radius: 3.8,
    prompt: 'Examine ancient sanctum altar'
  });

  // 8. Brass Lamps with flickering warm point lights (4 lamps)
  const lampPositions = [
    [-2.2, 0, -10],
    [2.2, 0, -10],
    [-2.5, 0, 4],
    [2.5, 0, 4]
  ];
  const activeLamps = [];
  lampPositions.forEach(([lx, ly, lz]) => {
    const lamp = buildBrassLamp({ height: 2.2, hasLight: true, lightColor: 0xff851b });
    lamp.position.set(lx, ly, lz);
    group.add(lamp);
    activeLamps.push(lamp);
    if (lamp.userData.light) lights.push(lamp.userData.light);
  });

  // 9. Diagonal Light Shaft from clerestory window
  const shaftGeo = new THREE.CylinderGeometry(0.8, 3.2, 16, 12, 1, true);
  const shaftMat = new THREE.MeshBasicMaterial({
    color: 0xffcf99,
    transparent: true,
    opacity: 0.14,
    side: THREE.DoubleSide,
    depthWrite: false
  });
  const shaft = new THREE.Mesh(shaftGeo, shaftMat);
  shaft.position.set(4, 5.5, -6);
  shaft.rotation.z = 0.55;
  shaft.rotation.x = -0.3;
  group.add(shaft);

  // Spawn points for sanctum
  const spawns = {
    player_start: new THREE.Vector3(0, 0, 14),
    investigator: new THREE.Vector3(0, 0, 14),
    player: new THREE.Vector3(0, 0, 14),
    priest_spawn: new THREE.Vector3(2.8, 0, -5),
    priest: new THREE.Vector3(2.8, 0, -5),
    villager_spawn: new THREE.Vector3(-3.5, 0, 10),
    villager: new THREE.Vector3(-3.5, 0, 10),
    robber_spawn: new THREE.Vector3(-5, 0, -14),
    robber1: new THREE.Vector3(-5, 0, -14),
    robber2: new THREE.Vector3(5, 0, -14),
    default: new THREE.Vector3(0, 0, 14)
  };

  return {
    fog,
    skyColor,
    spawns,
    update: (dt, time) => {
      // Dynamic flickering of sanctum oil lamps
      activeLamps.forEach((l, idx) => {
        if (l.userData.light) {
          l.userData.light.intensity = 1.4 + Math.sin(time * 10 + idx * 2.1) * 0.35;
        }
      });
      // Soft drift on light shaft
      shaft.material.opacity = 0.12 + Math.sin(time * 1.5) * 0.04;
    }
  };
}
