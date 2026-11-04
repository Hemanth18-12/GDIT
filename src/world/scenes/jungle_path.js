import * as THREE from 'three';
import {
  getJungleGroundTexture,
  getFootprintTexture,
  getSandstoneTexture
} from '../textures.js';
import { buildRekhaDeula, getArchitectureMaterials } from '../architecture.js';

export async function createJunglePathScene({ group, collision, registerInteractable, lights }) {
  const mats = getArchitectureMaterials();

  // 1. Atmosphere: Dense Moonlit Jungle Mist
  const skyColor = new THREE.Color(0x071118);
  const fog = new THREE.FogExp2(0x09151e, 0.022);

  // 2. Lighting (Total: 1 Moon Directional + 1 Forest Ambient + 1 Distant Temple Glow + 1 Trail Torch = 4 lights)
  const ambLight = new THREE.AmbientLight(0x132330, 0.7);
  group.add(ambLight);
  lights.push(ambLight);

  const moonLight = new THREE.DirectionalLight(0x7fb6ed, 1.3);
  moonLight.position.set(-15, 25, 20);
  moonLight.castShadow = true;
  moonLight.shadow.mapSize.width = 1024;
  moonLight.shadow.mapSize.height = 1024;
  moonLight.shadow.camera.near = 1;
  moonLight.shadow.camera.far = 100;
  const d = 30;
  moonLight.shadow.camera.left = -d;
  moonLight.shadow.camera.right = d;
  moonLight.shadow.camera.top = d;
  moonLight.shadow.camera.bottom = -d;
  group.add(moonLight);
  lights.push(moonLight);

  // 3. Forest Terrain
  const groundTex = getJungleGroundTexture();
  groundTex.repeat.set(6, 8);
  const groundGeo = new THREE.PlaneGeometry(45, 65, 32, 32);
  const groundMat = new THREE.MeshStandardMaterial({
    map: groundTex,
    roughness: 0.9,
    metalness: 0.05
  });
  const ground = new THREE.Mesh(groundGeo, groundMat);
  ground.rotation.x = -Math.PI / 2;
  ground.receiveShadow = true;
  group.add(ground);

  // 4. Jungle Stream (winding across the path at Z = -2 to -6)
  const streamGeo = new THREE.PlaneGeometry(42, 6);
  const streamMat = new THREE.MeshStandardMaterial({
    color: 0x1a3848,
    roughness: 0.1,
    metalness: 0.8,
    transparent: true,
    opacity: 0.88
  });
  const stream = new THREE.Mesh(streamGeo, streamMat);
  stream.rotation.x = -Math.PI / 2;
  stream.position.set(0, 0.05, -4);
  stream.name = 'JungleStream';
  group.add(stream);

  // 5. Ruined Ancient Stone Steps over the stream bank
  const ruinedStoneMat = new THREE.MeshStandardMaterial({
    map: getSandstoneTexture(),
    color: 0x5a6350, // Moss-tinted stone
    roughness: 0.9
  });

  const stepCoords = [
    [0, 0.25, -2, 7, 0.5, 2],
    [0, 0.65, -4, 5.5, 0.5, 2],
    [0, 1.05, -6, 4.5, 0.5, 2]
  ];

  stepCoords.forEach(([sx, sy, sz, sw, sh, sd]) => {
    const step = new THREE.Mesh(new THREE.BoxGeometry(sw, sh, sd), ruinedStoneMat);
    step.position.set(sx, sy, sz);
    step.castShadow = true;
    step.receiveShadow = true;
    group.add(step);
    // Don't fully block, allow walking across stepping stones
  });

  registerInteractable({
    id: 'jungle_ruined_steps',
    object3d: group,
    radius: 4.5,
    prompt: 'Cross ancient ruined temple stepping stones'
  });

  // 6. Dense Jungle Trees & Hanging Vines flanking path
  const treeTrunkGeo = new THREE.CylinderGeometry(0.7, 1.1, 14, 8);
  const treeBarkMat = new THREE.MeshStandardMaterial({ color: 0x221a14, roughness: 0.95 });
  const foliageGeo = new THREE.DodecahedronGeometry(3.2);
  const foliageMat = new THREE.MeshStandardMaterial({ color: 0x162a1a, roughness: 0.85 });
  const vineGeo = new THREE.CylinderGeometry(0.04, 0.04, 6, 4);
  const vineMat = new THREE.MeshStandardMaterial({ color: 0x2b3d1f, roughness: 0.9 });

  const treePositions = [
    [-10, 0, 14],
    [10, 0, 14],
    [-11, 0, 5],
    [11, 0, 6],
    [-10, 0, -4],
    [12, 0, -3],
    [-9, 0, -14],
    [9, 0, -15],
    [-12, 0, -22],
    [11, 0, -23]
  ];

  treePositions.forEach(([tx, ty, tz]) => {
    const trunk = new THREE.Mesh(treeTrunkGeo, treeBarkMat);
    trunk.position.set(tx, 7, tz);
    trunk.castShadow = true;
    group.add(trunk);
    collision.addBox(tx, 7, tz, 2.2, 14, 2.2);

    const foliage = new THREE.Mesh(foliageGeo, foliageMat);
    foliage.position.set(tx, 13, tz);
    foliage.scale.set(1.4, 1.1, 1.4);
    foliage.castShadow = true;
    group.add(foliage);

    // Hanging vines
    for (let v = 0; v < 3; v++) {
      const vine = new THREE.Mesh(vineGeo, vineMat);
      vine.position.set(
        tx + (Math.random() - 0.5) * 2.5,
        10 - Math.random() * 2,
        tz + (Math.random() - 0.5) * 2.5
      );
      group.add(vine);
    }
  });

  // Perimeter collision
  collision.addPerimeter(18, 30, 12);

  // 7. Distant Glowing Temple Spire (Rekha Deula on a distant hill)
  const distantTemple = buildRekhaDeula({ width: 10, height: 26, depth: 10 });
  distantTemple.position.set(0, 6, -45);
  distantTemple.scale.set(0.7, 0.7, 0.7);
  group.add(distantTemple);

  // Warm golden spire glow shining through trees
  const templeGlowLight = new THREE.PointLight(0xffaa33, 2.2, 35, 1.5);
  templeGlowLight.position.set(0, 18, -42);
  group.add(templeGlowLight);
  lights.push(templeGlowLight);

  // 8. Footprints Trail leading toward river and distant temple
  const fpTrailGroup = new THREE.Group();
  fpTrailGroup.name = 'JungleFootprintTrail';

  const fpTex = getFootprintTexture();
  const fpMat = new THREE.MeshBasicMaterial({
    map: fpTex,
    transparent: true,
    opacity: 0.8,
    depthWrite: false
  });
  const fpGeo = new THREE.PlaneGeometry(0.5, 0.8);

  const trailCoords = [
    [0.2, 12, 0.1],
    [-0.4, 9, -0.15],
    [0.3, 5, 0.1],
    [-0.2, 1.5, 0.05],
    [0.4, -1.5, 0.2],
    [-0.3, -8, -0.1],
    [0.5, -12, 0.15]
  ];

  trailCoords.forEach(([fx, fz, fRot]) => {
    const fp = new THREE.Mesh(fpGeo, fpMat);
    fp.rotation.x = -Math.PI / 2;
    fp.rotation.z = fRot;
    fp.position.set(fx, 0.08, fz);
    fpTrailGroup.add(fp);
  });

  group.add(fpTrailGroup);

  registerInteractable({
    id: 'muddy_trail',
    object3d: fpTrailGroup,
    radius: 3.5,
    prompt: 'Inspect robber trail heading toward the distant temple'
  });

  // 9. Abandoned Robber Torch stuck in the ground near stream
  const torchLight = new THREE.PointLight(0xff7711, 1.5, 10, 2.0);
  torchLight.position.set(2.8, 1.5, 2.5);
  group.add(torchLight);
  lights.push(torchLight);

  const torchStick = new THREE.Mesh(
    new THREE.CylinderGeometry(0.06, 0.06, 1.8, 6),
    new THREE.MeshStandardMaterial({ color: 0x3d2b1f })
  );
  torchStick.position.set(2.8, 0.9, 2.5);
  torchStick.rotation.z = 0.2;
  group.add(torchStick);

  // 10. Low-lying Jungle Mist layer
  const mistMat = new THREE.MeshBasicMaterial({
    color: 0x88bbcc,
    transparent: true,
    opacity: 0.15,
    depthWrite: false
  });
  const mistGeo = new THREE.PlaneGeometry(36, 55);
  const mist = new THREE.Mesh(mistGeo, mistMat);
  mist.rotation.x = -Math.PI / 2;
  mist.position.y = 0.4;
  group.add(mist);

  // Spawn points for jungle path
  const spawns = {
    player_start: new THREE.Vector3(0, 0, 18),
    investigator: new THREE.Vector3(0, 0, 18),
    player: new THREE.Vector3(0, 0, 18),
    robber_spawn: new THREE.Vector3(2.5, 0, -14),
    robber1: new THREE.Vector3(2.5, 0, -14),
    robber2: new THREE.Vector3(-2.5, 0, -16),
    villager_spawn: new THREE.Vector3(-4, 0, 16),
    villager: new THREE.Vector3(-4, 0, 16),
    priest_spawn: new THREE.Vector3(3.5, 0, 15),
    priest: new THREE.Vector3(3.5, 0, 15),
    default: new THREE.Vector3(0, 0, 18)
  };

  return {
    fog,
    skyColor,
    spawns,
    update: (dt, time) => {
      // Shimmer on river stream
      stream.material.opacity = 0.82 + Math.sin(time * 2) * 0.08;
      // Torch flicker
      torchLight.intensity = 1.4 + Math.sin(time * 12) * 0.3;
      // Distant temple pulse
      templeGlowLight.intensity = 2.0 + Math.sin(time * 1.8) * 0.4;
      // Mist slow wave
      mist.position.x = Math.sin(time * 0.4) * 1.2;
    }
  };
}
