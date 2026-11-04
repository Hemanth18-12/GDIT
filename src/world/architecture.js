import * as THREE from 'three';
import { getSandstoneTexture, getRedSilkTexture } from './textures.js';

/**
 * Reusable material library to keep draw calls and memory low
 */
let materials = null;

export function getArchitectureMaterials() {
  if (materials) return materials;

  const sandstoneTex = getSandstoneTexture();
  sandstoneTex.repeat.set(2, 4);

  materials = {
    sandstone: new THREE.MeshStandardMaterial({
      map: sandstoneTex,
      roughness: 0.85,
      metalness: 0.1
    }),
    darkStone: new THREE.MeshStandardMaterial({
      color: 0x3d352e,
      roughness: 0.9,
      metalness: 0.05
    }),
    brass: new THREE.MeshStandardMaterial({
      color: 0xd4af37,
      roughness: 0.35,
      metalness: 0.85
    }),
    flame: new THREE.MeshBasicMaterial({
      color: 0xffaa22
    }),
    redFabric: new THREE.MeshStandardMaterial({
      map: getRedSilkTexture(),
      roughness: 0.7,
      metalness: 0.1,
      side: THREE.DoubleSide
    }),
    saffronFlag: new THREE.MeshStandardMaterial({
      color: 0xff6f00,
      roughness: 0.6,
      side: THREE.DoubleSide
    }),
    whiteJasmine: new THREE.MeshStandardMaterial({
      color: 0xf4f1ea,
      roughness: 0.5
    }),
    orangeMarigold: new THREE.MeshStandardMaterial({
      color: 0xf77f00,
      roughness: 0.6
    })
  };

  return materials;
}

/**
 * Builds a grand Odisha Kalinga-style Rekha Deula (tall curved sikhara spire)
 */
export function buildRekhaDeula({ width = 12, height = 36, depth = 12 } = {}) {
  const mats = getArchitectureMaterials();
  const group = new THREE.Group();
  group.name = 'RekhaDeula_Spire';

  // 1. Bada (Lower square sanctum base)
  const badaHeight = height * 0.3;
  const badaGeo = new THREE.BoxGeometry(width, badaHeight, depth);
  const bada = new THREE.Mesh(badaGeo, mats.sandstone);
  bada.position.y = badaHeight / 2;
  bada.castShadow = true;
  bada.receiveShadow = true;
  group.add(bada);

  // Decorative projecting pilasters (Rathas)
  const rathaGeo = new THREE.BoxGeometry(width * 0.45, badaHeight, depth + 0.6);
  const ratha = new THREE.Mesh(rathaGeo, mats.sandstone);
  ratha.position.y = badaHeight / 2;
  ratha.castShadow = true;
  group.add(ratha);

  // 2. Gandi (Curvilinear spire / Shikhara) built from stepped tiers
  const tiers = 14;
  const gandiHeight = height * 0.55;
  const tierHeight = gandiHeight / tiers;

  for (let i = 0; i < tiers; i++) {
    const t = i / tiers;
    // Inward curving profile (Rekha curvature)
    const curve = Math.pow(t, 1.6);
    const tierWidth = width * (1.0 - curve * 0.55);
    const tierDepth = depth * (1.0 - curve * 0.55);

    const tierGeo = new THREE.BoxGeometry(tierWidth, tierHeight * 0.95, tierDepth);
    const tierMesh = new THREE.Mesh(tierGeo, mats.sandstone);
    tierMesh.position.y = badaHeight + i * tierHeight + tierHeight / 2;
    tierMesh.castShadow = true;
    tierMesh.receiveShadow = true;
    group.add(tierMesh);

    // Bhumi-Amlas (miniature fluted corner stones on alternate tiers)
    if (i % 2 === 1 && i < tiers - 2) {
      const amlaCornerGeo = new THREE.CylinderGeometry(0.5, 0.5, tierHeight * 0.8, 8);
      const corners = [
        [tierWidth / 2 - 0.2, tierDepth / 2 - 0.2],
        [-tierWidth / 2 + 0.2, tierDepth / 2 - 0.2],
        [tierWidth / 2 - 0.2, -tierDepth / 2 + 0.2],
        [-tierWidth / 2 + 0.2, -tierDepth / 2 + 0.2]
      ];
      corners.forEach(([cx, cz]) => {
        const amlaMesh = new THREE.Mesh(amlaCornerGeo, mats.sandstone);
        amlaMesh.position.set(cx, badaHeight + i * tierHeight + tierHeight / 2, cz);
        group.add(amlaMesh);
      });
    }
  }

  // 3. Mastaka (Crowning elements)
  const mastakaBaseY = badaHeight + gandiHeight;

  // Beki (Cylindrical neck)
  const bekiGeo = new THREE.CylinderGeometry(width * 0.2, width * 0.22, 1.2, 16);
  const beki = new THREE.Mesh(bekiGeo, mats.sandstone);
  beki.position.y = mastakaBaseY + 0.6;
  group.add(beki);

  // Large fluted Amalaka (flattened ribbed disc)
  const amalakaGeo = new THREE.CylinderGeometry(width * 0.32, width * 0.32, 1.4, 24);
  const amalaka = new THREE.Mesh(amalakaGeo, mats.sandstone);
  amalaka.position.y = mastakaBaseY + 1.9;
  amalaka.castShadow = true;
  group.add(amalaka);

  // Khapuri (Dome cap)
  const khapuriGeo = new THREE.ConeGeometry(width * 0.25, 1.2, 16);
  const khapuri = new THREE.Mesh(khapuriGeo, mats.sandstone);
  khapuri.position.y = mastakaBaseY + 3.2;
  group.add(khapuri);

  // Sacred Kalasa (brass water-pot finial)
  const kalasaGeo = new THREE.SphereGeometry(width * 0.12, 12, 12);
  const kalasa = new THREE.Mesh(kalasaGeo, mats.brass);
  kalasa.position.y = mastakaBaseY + 4.2;
  group.add(kalasa);

  // Dhwaja (Sacred temple flag and mast)
  const mastGeo = new THREE.CylinderGeometry(0.08, 0.08, 3.5, 8);
  const mast = new THREE.Mesh(mastGeo, mats.brass);
  mast.position.y = mastakaBaseY + 5.5;
  group.add(mast);

  const flagShape = new THREE.Shape();
  flagShape.moveTo(0, 0);
  flagShape.lineTo(2.2, -0.6);
  flagShape.lineTo(0, -1.2);
  flagShape.closePath();

  const flagGeo = new THREE.ShapeGeometry(flagShape);
  const flag = new THREE.Mesh(flagGeo, mats.saffronFlag);
  flag.position.set(0.08, mastakaBaseY + 6.8, 0);
  flag.name = 'TempleFlag';
  group.add(flag);

  return group;
}

/**
 * Builds a carved Odisha temple pillar with base, shaft, and pot capital
 */
export function buildPillar({ height = 7, radius = 0.65 } = {}) {
  const mats = getArchitectureMaterials();
  const group = new THREE.Group();
  group.name = 'CarvedPillar';

  // Base plinth
  const baseGeo = new THREE.BoxGeometry(radius * 2.8, 0.8, radius * 2.8);
  const base = new THREE.Mesh(baseGeo, mats.sandstone);
  base.position.y = 0.4;
  base.castShadow = true;
  base.receiveShadow = true;
  group.add(base);

  // Stepped lower moulding
  const mouldGeo = new THREE.CylinderGeometry(radius * 1.3, radius * 1.4, 0.6, 12);
  const mould = new THREE.Mesh(mouldGeo, mats.sandstone);
  mould.position.y = 1.1;
  group.add(mould);

  // Main octagonal carved shaft
  const shaftHeight = height - 2.8;
  const shaftGeo = new THREE.CylinderGeometry(radius, radius * 1.05, shaftHeight, 8);
  const shaft = new THREE.Mesh(shaftGeo, mats.sandstone);
  shaft.position.y = 1.4 + shaftHeight / 2;
  shaft.castShadow = true;
  shaft.receiveShadow = true;
  group.add(shaft);

  // Capital: Purna-ghata (vase of abundance)
  const potGeo = new THREE.SphereGeometry(radius * 1.25, 12, 10);
  const pot = new THREE.Mesh(potGeo, mats.sandstone);
  pot.position.y = 1.4 + shaftHeight + radius * 0.7;
  group.add(pot);

  // Top abacus bracket
  const topGeo = new THREE.BoxGeometry(radius * 2.6, 0.6, radius * 2.6);
  const top = new THREE.Mesh(topGeo, mats.sandstone);
  top.position.y = height - 0.3;
  top.castShadow = true;
  group.add(top);

  return group;
}

/**
 * Builds a traditional Gajasimha statue (Lion guardian upon an elephant)
 */
export function buildLionStatue() {
  const mats = getArchitectureMaterials();
  const group = new THREE.Group();
  group.name = 'Gajasimha_Lion';

  // Pedestal
  const pedGeo = new THREE.BoxGeometry(1.6, 0.8, 2.2);
  const ped = new THREE.Mesh(pedGeo, mats.darkStone);
  ped.position.y = 0.4;
  ped.castShadow = true;
  ped.receiveShadow = true;
  group.add(ped);

  // Elephant crouching underneath
  const elephantGeo = new THREE.BoxGeometry(1.2, 0.7, 1.6);
  const elephant = new THREE.Mesh(elephantGeo, mats.sandstone);
  elephant.position.set(0, 1.1, 0);
  group.add(elephant);

  // Rampant lion torso
  const torsoGeo = new THREE.BoxGeometry(1.0, 1.6, 1.2);
  const torso = new THREE.Mesh(torsoGeo, mats.sandstone);
  torso.position.set(0, 2.2, 0);
  torso.rotation.x = -0.2;
  torso.castShadow = true;
  group.add(torso);

  // Fierce Lion head with mane
  const headGeo = new THREE.SphereGeometry(0.55, 10, 10);
  const head = new THREE.Mesh(headGeo, mats.sandstone);
  head.position.set(0, 3.2, 0.4);
  head.castShadow = true;
  group.add(head);

  // Wide open jaw / snout
  const jawGeo = new THREE.BoxGeometry(0.5, 0.3, 0.45);
  const jaw = new THREE.Mesh(jawGeo, mats.sandstone);
  jaw.position.set(0, 2.95, 0.7);
  group.add(jaw);

  return group;
}

/**
 * Builds an authentic tall Brass Deepastambha (oil lamp stand) with warm light
 */
export function buildBrassLamp({ height = 2.4, hasLight = false, lightColor = 0xff9933 } = {}) {
  const mats = getArchitectureMaterials();
  const group = new THREE.Group();
  group.name = 'BrassLamp';

  // Base
  const baseGeo = new THREE.CylinderGeometry(0.35, 0.5, 0.3, 16);
  const base = new THREE.Mesh(baseGeo, mats.brass);
  base.position.y = 0.15;
  base.castShadow = true;
  group.add(base);

  // Slender central rod
  const rodGeo = new THREE.CylinderGeometry(0.06, 0.09, height - 0.7, 10);
  const rod = new THREE.Mesh(rodGeo, mats.brass);
  rod.position.y = 0.3 + (height - 0.7) / 2;
  group.add(rod);

  // Tiered oil disc (Thali)
  const dishGeo = new THREE.CylinderGeometry(0.42, 0.25, 0.12, 16);
  const dish = new THREE.Mesh(dishGeo, mats.brass);
  dish.position.y = height - 0.35;
  group.add(dish);

  // Central flame
  const flameGeo = new THREE.ConeGeometry(0.08, 0.22, 8);
  const flame = new THREE.Mesh(flameGeo, mats.flame);
  flame.position.y = height - 0.2;
  flame.name = 'LampFlame';
  group.add(flame);

  // Optional point light (flickering in update)
  if (hasLight) {
    const lampLight = new THREE.PointLight(lightColor, 1.8, 10, 2.0);
    lampLight.position.set(0, height - 0.1, 0);
    lampLight.castShadow = true;
    lampLight.name = 'LampPointLight';
    group.add(lampLight);
    group.userData.light = lampLight;
  }

  return group;
}

/**
 * Builds a hanging temple bell with chain
 */
export function buildTempleBell({ chainLength = 3.0 } = {}) {
  const mats = getArchitectureMaterials();
  const group = new THREE.Group();
  group.name = 'TempleBell';

  // Hanging chain
  const chainGeo = new THREE.CylinderGeometry(0.03, 0.03, chainLength, 6);
  const chain = new THREE.Mesh(chainGeo, mats.brass);
  chain.position.y = -chainLength / 2;
  group.add(chain);

  // Bell body
  const bellGeo = new THREE.CylinderGeometry(0.2, 0.55, 0.7, 16, 1, true);
  const bell = new THREE.Mesh(bellGeo, mats.brass);
  bell.position.y = -chainLength - 0.35;
  bell.castShadow = true;
  group.add(bell);

  // Bell crown
  const domeGeo = new THREE.SphereGeometry(0.24, 12, 8, 0, Math.PI * 2, 0, Math.PI / 2);
  const dome = new THREE.Mesh(domeGeo, mats.brass);
  dome.position.y = -chainLength;
  group.add(dome);

  // Clapper
  const clapperGeo = new THREE.SphereGeometry(0.1, 8, 8);
  const clapper = new THREE.Mesh(clapperGeo, mats.brass);
  clapper.position.y = -chainLength - 0.65;
  group.add(clapper);

  return group;
}

/**
 * Builds flower garland curve (orange marigold and jasmine)
 */
export function buildGarland({ length = 4, sag = 0.8 } = {}) {
  const mats = getArchitectureMaterials();
  const group = new THREE.Group();
  group.name = 'FlowerGarland';

  const segments = 16;
  for (let i = 0; i <= segments; i++) {
    const t = i / segments;
    const x = (t - 0.5) * length;
    const y = -Math.sin(t * Math.PI) * sag;

    const geo = new THREE.SphereGeometry(0.12, 6, 6);
    const mat = i % 2 === 0 ? mats.orangeMarigold : mats.whiteJasmine;
    const bead = new THREE.Mesh(geo, mat);
    bead.position.set(x, y, 0);
    group.add(bead);
  }

  return group;
}
