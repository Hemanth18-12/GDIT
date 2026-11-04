import * as THREE from 'three';
import { EventBus } from './core/EventBus.js';
import { initWorld } from './world/index.js';
import { initCharacters } from './characters/index.js';
import { initGameplay } from './gameplay/index.js';
import { initUI } from './ui/index.js';

async function bootstrap() {
  const canvas = document.getElementById('game-canvas');

  // 1. Create renderer, scene, camera, clock
  const renderer = new THREE.WebGLRenderer({
    canvas,
    antialias: true,
    powerPreference: 'high-performance'
  });
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;

  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0e131b);
  scene.fog = new THREE.FogExp2(0x0e131b, 0.015);

  const camera = new THREE.PerspectiveCamera(
    60,
    window.innerWidth / window.innerHeight,
    0.1,
    1000
  );
  camera.position.set(0, 8, 16);

  const clock = new THREE.Clock();

  // 2. Create the EventBus
  const bus = new EventBus();

  // 3. Initialize UI layer
  const ui = await initUI({ bus });

  // 4. Initialize modules IN THAT ORDER: initWorld -> initCharacters -> initGameplay
  const world = await initWorld({ scene, camera, renderer, bus });
  const characters = await initCharacters({ scene, bus, world });
  const gameplay = await initGameplay({ scene, bus, world, characters });

  // Start gameplay
  gameplay.start();

  // Resize handler
  window.addEventListener('resize', () => {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  });

  // Third-person camera offset
  const cameraOffset = new THREE.Vector3(0, 6, 11);
  const cameraTarget = new THREE.Vector3();

  // 5. Update loop calling each module's update(dt)
  function animate() {
    requestAnimationFrame(animate);

    const dt = Math.min(clock.getDelta(), 0.1);

    // Call each module's update(dt)
    world.update(dt);
    characters.update(dt);
    gameplay.update(dt);
    if (ui && ui.update) ui.update(dt);

    // Third-person camera tracking
    if (characters.player && characters.player.position) {
      const pPos = characters.player.position;
      const desiredPos = pPos.clone().add(cameraOffset);
      camera.position.lerp(desiredPos, 0.08);

      cameraTarget.lerp(new THREE.Vector3(pPos.x, pPos.y + 1.2, pPos.z), 0.1);
      camera.lookAt(cameraTarget);
    }

    renderer.render(scene, camera);
  }

  animate();
}

bootstrap().catch((err) => {
  console.error('[Legacy of Temples] Failed to initialize game:', err);
});

