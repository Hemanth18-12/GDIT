import * as THREE from 'three';
import {
  getFootprintTexture,
  getRedSilkTexture,
  getSandstoneTexture
} from '../textures.js';
import {
  buildPillar,
  buildBrassLamp,
  getArchitectureMaterials
} from '../architecture.js';

export async function createShrineCrimeScene({ group, collision, registerInteractable, lights }) {
  const mats = getArchitectureMaterials();

  // 1. Atmosphere: Eerie Night Crime Scene with Moonlight
  const skyColor = new THREE.Color(0x0a1018);
  const fog = new THREE.FogExp2(0x0d1420, 0.028);

  // 2. Lighting: Cool blue moonlight beam against dying warm lamp
  const ambLight = new THREE.AmbientLight(0x1a2638, 0.65);
  group.add(ambLight);
  lights.push(ambLight);

  // Directional moonlight streaming through broken doorway
  const moonBeam = new THREE.DirectionalLight(0x8bc0f8, 1.4);
  moonBeam.position.set(4, 12, 16);
  moonBeam.target.position.set(0, 0, 0);
  moonBeam.castShadow = true;
  group.add(moonBeam);
  group.add(moonBeam.target);
  lights.push(moonBeam);

  // 3. Stone Floor with dust and debris
  const floorGeo = new THREE.PlaneGeometry(24, 26);
  const floorMat = new THREE.MeshStandardMaterial({
    map: getSandstoneTexture(),
    roughness: 0.85,
    metalness: 0.05
  });
  const floor = new THREE.Mesh(floorGeo, floorMat);
  floor.rotation.x = -Math.PI / 2;
  floor.receiveShadow = true;
  group.add(floor);

  // 4. Shrine Walls and Ruined Doorway
  const wallMat = mats.sandstone;

  // West Wall
  const wWall = new THREE.Mesh(new THREE.BoxGeometry(1.5, 7, 26), wallMat);
  wWall.position.set(-11, 3.5, 0);
  wWall.castShadow = true;
  wWall.receiveShadow = true;
  group.add(wWall);
  collision.addBox(-11, 3.5, 0, 2, 7, 26);

  // East Wall
  const eWall = new THREE.Mesh(new THREE.BoxGeometry(1.5, 7, 26), wallMat);
  eWall.position.set(11, 3.5, 0);
  eWall.castShadow = true;
  eWall.receiveShadow = true;
  group.add(eWall);
  collision.addBox(11, 3.5, 0, 2, 7, 26);

  // North (Back) Wall
  const nWall = new THREE.Mesh(new THREE.BoxGeometry(24, 7, 1.5), wallMat);
  nWall.position.set(0, 3.5, -12);
  nWall.castShadow = true;
  group.add(nWall);
  collision.addBox(0, 3.5, -12, 24, 7, 2);

  // South Wall with Broken Door Gap
  const sWallL = new THREE.Mesh(new THREE.BoxGeometry(8, 7, 1.5), wallMat);
  sWallL.position.set(-7, 3.5, 12);
  group.add(sWallL);
  collision.addBox(-7, 3.5, 12, 8, 7, 2);

  const sWallR = new THREE.Mesh(new THREE.BoxGeometry(8, 7, 1.5), wallMat);
  sWallR.position.set(7, 3.5, 12);
  group.add(sWallR);
  collision.addBox(7, 3.5, 12, 8, 7, 2);

  collision.addPerimeter(12, 13, 8);

  // 5. Broken Door hanging off hinges
  const doorGeo = new THREE.BoxGeometry(2.4, 5.0, 0.25);
  const doorMat = new THREE.MeshStandardMaterial({
    color: 0x422c1b,
    roughness: 0.9
  });
  const brokenDoor = new THREE.Mesh(doorGeo, doorMat);
  brokenDoor.position.set(-1.2, 2.2, 11.8);
  brokenDoor.rotation.set(0.15, 0.45, -0.2);
  brokenDoor.castShadow = true;
  group.add(brokenDoor);
  collision.addBox(-1.2, 2.2, 11.8, 2.6, 5.0, 1.0);

  // Volumetric moonlight cone through broken door
  const beamConeGeo = new THREE.CylinderGeometry(0.8, 3.5, 14, 12, 1, true);
  const beamConeMat = new THREE.MeshBasicMaterial({
    color: 0xa4d4ff,
    transparent: true,
    opacity: 0.16,
    side: THREE.DoubleSide,
    depthWrite: false
  });
  const beamCone = new THREE.Mesh(beamConeGeo, beamConeMat);
  beamCone.position.set(1.5, 3.5, 5);
  beamCone.rotation.x = 0.55;
  beamCone.rotation.z = -0.15;
  group.add(beamCone);

  // 6. Plundered Subsidiary Altar
  const shrineAltar = new THREE.Mesh(new THREE.BoxGeometry(3.5, 1.2, 2.5), mats.darkStone);
  shrineAltar.position.set(0, 0.6, -7);
  shrineAltar.castShadow = true;
  shrineAltar.receiveShadow = true;
  group.add(shrineAltar);
  collision.addBox(0, 0.6, -7, 3.8, 1.4, 2.8);

  // 7. INTERACTABLE: Fallen / Broken Brass Lamp (broken_lamp)
  const brokenLampGroup = new THREE.Group();
  brokenLampGroup.name = 'BrokenLampGroup';
  brokenLampGroup.position.set(1.8, 0.2, -3.5);

  const fallenLamp = buildBrassLamp({ height: 2.2, hasLight: false });
  fallenLamp.rotation.z = Math.PI / 2 - 0.15;
  fallenLamp.rotation.y = 0.4;
  fallenLamp.position.set(0, 0.15, 0);
  brokenLampGroup.add(fallenLamp);

  // Dark oil spill stain on floor
  const oilSpillGeo = new THREE.PlaneGeometry(1.8, 1.4);
  const oilSpillMat = new THREE.MeshStandardMaterial({
    color: 0x110e08,
    roughness: 0.15,
    metalness: 0.4
  });
  const oilSpill = new THREE.Mesh(oilSpillGeo, oilSpillMat);
  oilSpill.rotation.x = -Math.PI / 2;
  oilSpill.position.set(0.4, 0.02, 0);
  brokenLampGroup.add(oilSpill);

  // Weak dying ember light from broken lamp
  const emberLight = new THREE.PointLight(0xff5511, 1.2, 7, 2.0);
  emberLight.position.set(0.5, 0.3, 0);
  brokenLampGroup.add(emberLight);
  lights.push(emberLight);

  group.add(brokenLampGroup);

  registerInteractable({
    id: 'broken_lamp',
    object3d: brokenLampGroup,
    radius: 2.8,
    prompt: 'Examine overturned brass diya and fresh oil pool'
  });

  // 8. INTERACTABLE: Scattered Footprints (footprints)
  const footprintGroup = new THREE.Group();
  footprintGroup.name = 'MuddyFootprints';
  footprintGroup.position.set(-0.8, 0.02, 0);

  const fpTex = getFootprintTexture();
  const fpMat = new THREE.MeshBasicMaterial({
    map: fpTex,
    transparent: true,
    opacity: 0.85,
    depthWrite: false
  });
  const fpGeo = new THREE.PlaneGeometry(0.5, 0.9);

  // Trail of 5 distinct footprints heading toward broken door
  const fpOffsets = [
    [-0.2, -4, 0.1],
    [0.3, -2.5, -0.1],
    [-0.1, -1.0, 0.15],
    [0.4, 1.2, 0.05],
    [0.2, 3.5, 0.2]
  ];

  fpOffsets.forEach(([fx, fz, fRot]) => {
    const fp = new THREE.Mesh(fpGeo, fpMat);
    fp.rotation.x = -Math.PI / 2;
    fp.rotation.z = fRot;
    fp.position.set(fx, 0, fz);
    footprintGroup.add(fp);
  });

  group.add(footprintGroup);

  registerInteractable({
    id: 'footprints',
    object3d: footprintGroup,
    radius: 3.2,
    prompt: 'Inspect muddy robber footprints leading toward doorway'
  });

  // 9. INTERACTABLE: Torn Red Cloth (red_cloth)
  const clothGroup = new THREE.Group();
  clothGroup.name = 'TornRedCloth';
  clothGroup.position.set(-2.5, 1.1, 11.2); // Snagged on door frame / pillar

  const clothGeo = new THREE.PlaneGeometry(0.9, 1.5, 6, 6);
  const clothPos = clothGeo.attributes.position;
  for (let i = 0; i < clothPos.count; i++) {
    clothPos.setZ(i, Math.sin(clothPos.getY(i) * 4) * 0.15);
  }
  clothGeo.computeVertexNormals();

  const clothMat = new THREE.MeshStandardMaterial({
    map: getRedSilkTexture(),
    roughness: 0.65,
    side: THREE.DoubleSide
  });
  const tornCloth = new THREE.Mesh(clothGeo, clothMat);
  tornCloth.rotation.y = 0.4;
  tornCloth.castShadow = true;
  clothGroup.add(tornCloth);

  group.add(clothGroup);

  registerInteractable({
    id: 'red_cloth',
    object3d: clothGroup,
    radius: 2.8,
    prompt: 'Inspect torn scrap of thief silk cloth'
  });

  // 10. Scattered Flowers on floor
  const flowerGeo = new THREE.SphereGeometry(0.08, 6, 5);
  for (let fl = 0; fl < 25; fl++) {
    const petal = new THREE.Mesh(
      flowerGeo,
      fl % 2 === 0 ? mats.orangeMarigold : mats.whiteJasmine
    );
    petal.position.set((Math.random() - 0.5) * 6, 0.04, -5 + (Math.random() - 0.5) * 5);
    petal.rotation.set(Math.random(), Math.random(), Math.random());
    group.add(petal);
  }

  // Spawn points for crime shrine
  const spawns = {
    player_start: new THREE.Vector3(0, 0, 8),
    investigator: new THREE.Vector3(0, 0, 8),
    player: new THREE.Vector3(0, 0, 8),
    villager_spawn: new THREE.Vector3(3.2, 0, 9.5),
    villager: new THREE.Vector3(3.2, 0, 9.5),
    robber_spawn: new THREE.Vector3(-4, 0, -8),
    robber1: new THREE.Vector3(-4, 0, -8),
    robber2: new THREE.Vector3(-6, 0, -9),
    priest_spawn: new THREE.Vector3(3.0, 0, 0),
    priest: new THREE.Vector3(3.0, 0, 0),
    default: new THREE.Vector3(0, 0, 8)
  };

  return {
    fog,
    skyColor,
    spawns,
    update: (dt, time) => {
      // Ember flicker
      emberLight.intensity = 1.0 + Math.sin(time * 14) * 0.3;
      // Gentle flutter of torn cloth caught in night draft
      tornCloth.rotation.y = 0.4 + Math.sin(time * 2.5) * 0.12;
    }
  };
}
