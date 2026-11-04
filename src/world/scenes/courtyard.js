import * as THREE from 'three';
import { getWetPlazaTexture, getSandstoneTexture } from '../textures.js';
import {
  buildRekhaDeula,
  buildPillar,
  buildLionStatue,
  buildBrassLamp,
  getArchitectureMaterials
} from '../architecture.js';

export async function createCourtyardScene({ group, collision, registerInteractable, lights }) {
  const mats = getArchitectureMaterials();

  // 1. Atmosphere: Sunrise Sky & Fog
  const skyColor = new THREE.Color(0xf69d76); // Sunrise orange/pink
  const groundFogColor = new THREE.Color(0xd27860);
  const fog = new THREE.FogExp2(groundFogColor, 0.012);

  // 2. Lighting (Strict budget: 1 Sun + 1 Ambient + 2 Brass Lamps = 4 lights)
  const sunLight = new THREE.DirectionalLight(0xffecd0, 1.3);
  sunLight.position.set(30, 25, 20);
  sunLight.castShadow = true;
  sunLight.shadow.mapSize.width = 1024;
  sunLight.shadow.mapSize.height = 1024;
  sunLight.shadow.camera.near = 1;
  sunLight.shadow.camera.far = 120;
  const d = 32;
  sunLight.shadow.camera.left = -d;
  sunLight.shadow.camera.right = d;
  sunLight.shadow.camera.top = d;
  sunLight.shadow.camera.bottom = -d;
  group.add(sunLight);
  lights.push(sunLight);

  const ambLight = new THREE.AmbientLight(0x7a505b, 0.7); // Sunrise purple-pink ambient bounce
  group.add(ambLight);
  lights.push(ambLight);

  // 3. Sunrise clouds in sky
  const cloudGroup = new THREE.Group();
  cloudGroup.name = 'SunriseClouds';
  const cloudMat = new THREE.MeshBasicMaterial({
    color: 0xffb59b,
    transparent: true,
    opacity: 0.35,
    depthWrite: false
  });
  const cloudGeo = new THREE.SphereGeometry(18, 7, 7);
  for (let c = 0; c < 6; c++) {
    const cloud = new THREE.Mesh(cloudGeo, cloudMat);
    cloud.scale.set(2.5, 0.3, 1.4);
    cloud.position.set((c - 2.5) * 24, 28 + (c % 2) * 4, -35 - (c % 3) * 10);
    cloudGroup.add(cloud);
  }
  group.add(cloudGroup);

  // 4. Wet Reflective Plaza Floor
  const wetTex = getWetPlazaTexture();
  wetTex.repeat.set(8, 10);
  const floorGeo = new THREE.PlaneGeometry(50, 60);
  const floorMat = new THREE.MeshStandardMaterial({
    map: wetTex,
    roughness: 0.35,
    metalness: 0.2
  });
  const floor = new THREE.Mesh(floorGeo, floorMat);
  floor.rotation.x = -Math.PI / 2;
  floor.receiveShadow = true;
  group.add(floor);

  // 5. Tall Carved Odisha Rekha Deula Temple (Main Spire)
  const templeSpire = buildRekhaDeula({ width: 14, height: 38, depth: 14 });
  templeSpire.position.set(0, 0, -22);
  group.add(templeSpire);
  collision.addBox(0, 10, -22, 15, 20, 15);

  // Grand temple entrance steps & Jagamohana porch
  const stepsGeo = new THREE.BoxGeometry(10, 1.6, 6);
  const steps = new THREE.Mesh(stepsGeo, mats.sandstone);
  steps.position.set(0, 0.8, -13);
  steps.receiveShadow = true;
  steps.castShadow = true;
  group.add(steps);
  collision.addBox(0, 0.8, -13, 10, 1.6, 6);

  // 6. Carved Lion Statues (Gajasimha guardians flanking steps)
  const leftLion = buildLionStatue();
  leftLion.position.set(-6, 0, -11);
  leftLion.rotation.y = 0.35;
  group.add(leftLion);
  collision.addBox(-6, 1.5, -11, 2.0, 3.0, 2.5);

  const rightLion = buildLionStatue();
  rightLion.position.set(6, 0, -11);
  rightLion.rotation.y = -0.35;
  group.add(rightLion);
  collision.addBox(6, 1.5, -11, 2.0, 3.0, 2.5);

  registerInteractable({
    id: 'courtyard_lion_left',
    object3d: leftLion,
    radius: 3.5,
    prompt: 'Inspect carved stone Gajasimha lion'
  });

  // 7. Carved Boundary Walls and Columns
  const wallMat = mats.sandstone;
  const sideWallGeo = new THREE.BoxGeometry(1.5, 4.5, 58);
  const westWall = new THREE.Mesh(sideWallGeo, wallMat);
  westWall.position.set(-24, 2.25, -2);
  westWall.castShadow = true;
  group.add(westWall);
  collision.addBox(-24, 2.25, -2, 2.0, 5.0, 60);

  const eastWall = new THREE.Mesh(sideWallGeo, wallMat);
  eastWall.position.set(24, 2.25, -2);
  eastWall.castShadow = true;
  group.add(eastWall);
  collision.addBox(24, 2.25, -2, 2.0, 5.0, 60);

  // Front courtyard wall with Torana arch entrance
  const frontWallL = new THREE.Mesh(new THREE.BoxGeometry(17, 4.5, 1.5), wallMat);
  frontWallL.position.set(-15.5, 2.25, 27);
  group.add(frontWallL);
  collision.addBox(-15.5, 2.25, 27, 18, 5, 2);

  const frontWallR = new THREE.Mesh(new THREE.BoxGeometry(17, 4.5, 1.5), wallMat);
  frontWallR.position.set(15.5, 2.25, 27);
  group.add(frontWallR);
  collision.addBox(15.5, 2.25, 27, 18, 5, 2);

  // Back boundary wall behind sikhara
  collision.addBox(0, 5, -30, 50, 10, 2);

  // Perimeter containment
  collision.addPerimeter(25, 28, 8);

  // 8. Courtyard Pillars along walkways
  const pillarCoords = [
    [-12, 0, -5],
    [12, 0, -5],
    [-12, 0, 5],
    [12, 0, 5]
  ];
  pillarCoords.forEach(([px, py, pz], i) => {
    const pil = buildPillar({ height: 5.5, radius: 0.55 });
    pil.position.set(px, py, pz);
    group.add(pil);
    collision.addBox(px, 2.75, pz, 1.6, 5.5, 1.6);
  });

  // 9. Brass Oil Lamps with warm glow (2 with point lights)
  const lampL = buildBrassLamp({ height: 2.5, hasLight: true, lightColor: 0xff9020 });
  lampL.position.set(-4, 0, 8);
  group.add(lampL);
  if (lampL.userData.light) lights.push(lampL.userData.light);

  const lampR = buildBrassLamp({ height: 2.5, hasLight: true, lightColor: 0xff9020 });
  lampR.position.set(4, 0, 8);
  group.add(lampR);
  if (lampR.userData.light) lights.push(lampR.userData.light);

  registerInteractable({
    id: 'courtyard_lamp',
    object3d: lampL,
    radius: 3.0,
    prompt: 'Examine burning brass temple diya'
  });

  // Sanctum doorway interactable
  const sanctumEntranceGeo = new THREE.BoxGeometry(3.5, 5, 1);
  const sanctumEntranceMat = new THREE.MeshBasicMaterial({ color: 0x110c08 });
  const sanctumDoor = new THREE.Mesh(sanctumEntranceGeo, sanctumEntranceMat);
  sanctumDoor.position.set(0, 3.5, -14.8);
  group.add(sanctumDoor);

  registerInteractable({
    id: 'courtyard_sanctum_entrance',
    object3d: sanctumDoor,
    radius: 4.0,
    prompt: 'Enter the Sacred Sanctum'
  });

  // 10. Low-lying Morning Mist Layer
  const mistMat = new THREE.MeshBasicMaterial({
    color: 0xffe2d0,
    transparent: true,
    opacity: 0.18,
    depthWrite: false
  });
  const mistGeo = new THREE.PlaneGeometry(48, 56);
  const mist = new THREE.Mesh(mistGeo, mistMat);
  mist.rotation.x = -Math.PI / 2;
  mist.position.y = 0.35;
  mist.name = 'GroundMist';
  group.add(mist);

  // Spawn points for courtyard
  const spawns = {
    player_start: new THREE.Vector3(0, 0, 18),
    investigator: new THREE.Vector3(0, 0, 18),
    player: new THREE.Vector3(0, 0, 18),
    priest_spawn: new THREE.Vector3(3.5, 0, 2),
    priest: new THREE.Vector3(3.5, 0, 2),
    villager_spawn: new THREE.Vector3(-8, 0, 12),
    villager: new THREE.Vector3(-8, 0, 12),
    robber_spawn: new THREE.Vector3(14, 0, -12),
    robber1: new THREE.Vector3(14, 0, -12),
    robber2: new THREE.Vector3(16, 0, -14),
    default: new THREE.Vector3(0, 0, 18)
  };

  return {
    fog,
    skyColor,
    spawns,
    update: (dt, time) => {
      // Gentle flag flutter
      const flags = group.getObjectsByProperty('name', 'TempleFlag');
      flags.forEach((f) => {
        f.rotation.y = Math.sin(time * 3) * 0.15;
      });

      // Drifting mist
      mist.position.z = Math.sin(time * 0.5) * 1.5;

      // Lamp light flicker
      if (lampL.userData.light) {
        lampL.userData.light.intensity = 1.6 + Math.sin(time * 12) * 0.25;
      }
      if (lampR.userData.light) {
        lampR.userData.light.intensity = 1.6 + Math.cos(time * 15) * 0.25;
      }
    }
  };
}
