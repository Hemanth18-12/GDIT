import * as THREE from 'three';

const textureCache = new Map();

/**
 * Creates a procedural 2D canvas texture and returns a THREE.CanvasTexture.
 */
function getOrCreateTexture(key, drawFn, width = 512, height = 512) {
  if (textureCache.has(key)) {
    return textureCache.get(key);
  }

  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext('2d');

  drawFn(ctx, width, height);

  const texture = new THREE.CanvasTexture(canvas);
  texture.wrapS = THREE.RepeatWrapping;
  texture.wrapT = THREE.RepeatWrapping;
  textureCache.set(key, texture);
  return texture;
}

/**
 * Weathered Odisha sandstone (Khondalite stone)
 */
export function getSandstoneTexture() {
  return getOrCreateTexture('sandstone', (ctx, w, h) => {
    // Base warm ochre stone color
    const grad = ctx.createLinearGradient(0, 0, w, h);
    grad.addColorStop(0, '#8c5e3d');
    grad.addColorStop(0.5, '#784d30');
    grad.addColorStop(1, '#653e26');
    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, w, h);

    // Weathering grain & sediment noise
    for (let i = 0; i < 4000; i++) {
      const x = Math.random() * w;
      const y = Math.random() * h;
      const radius = Math.random() * 2 + 0.5;
      const shade = Math.floor(Math.random() * 60 - 30);
      ctx.fillStyle = `rgba(${120 + shade}, ${80 + shade}, ${50 + shade}, 0.25)`;
      ctx.beginPath();
      ctx.arc(x, y, radius, 0, Math.PI * 2);
      ctx.fill();
    }

    // Carved horizontal stratification bands
    ctx.strokeStyle = 'rgba(40, 25, 15, 0.35)';
    ctx.lineWidth = 2;
    for (let y = 16; y < h; y += 32) {
      ctx.beginPath();
      ctx.moveTo(0, y + (Math.random() * 4 - 2));
      for (let x = 0; x < w; x += 20) {
        ctx.lineTo(x, y + (Math.random() * 4 - 2));
      }
      ctx.stroke();
    }
  });
}

/**
 * Wet polished stone courtyard plaza with puddles
 */
export function getWetPlazaTexture() {
  return getOrCreateTexture('wet_plaza', (ctx, w, h) => {
    ctx.fillStyle = '#22201e';
    ctx.fillRect(0, 0, w, h);

    // Flagstone tile grid
    const tileSize = 64;
    ctx.lineWidth = 3;
    for (let y = 0; y < h; y += tileSize) {
      for (let x = 0; x < w; x += tileSize) {
        // Individual stone tone variation
        const tone = Math.floor(Math.random() * 30 - 15);
        ctx.fillStyle = `rgb(${45 + tone}, ${42 + tone}, ${40 + tone})`;
        ctx.fillRect(x + 2, y + 2, tileSize - 4, tileSize - 4);

        // Grout line
        ctx.strokeStyle = '#121110';
        ctx.strokeRect(x, y, tileSize, tileSize);
      }
    }

    // Wet glossy puddles sheen
    for (let i = 0; i < 12; i++) {
      const px = Math.random() * w;
      const py = Math.random() * h;
      const pr = Math.random() * 40 + 20;
      const pGrad = ctx.createRadialGradient(px, py, 5, px, py, pr);
      pGrad.addColorStop(0, 'rgba(80, 85, 95, 0.5)');
      pGrad.addColorStop(0.7, 'rgba(35, 38, 42, 0.3)');
      pGrad.addColorStop(1, 'rgba(0, 0, 0, 0)');
      ctx.fillStyle = pGrad;
      ctx.beginPath();
      ctx.arc(px, py, pr, 0, Math.PI * 2);
      ctx.fill();
    }
  });
}

/**
 * Relief carving panel textures for puzzle wall
 */
export function getReliefTexture(type = 'lotus') {
  return getOrCreateTexture(`relief_${type}`, (ctx, w, h) => {
    // Stone background
    ctx.fillStyle = '#6e513b';
    ctx.fillRect(0, 0, w, h);

    // Border frame
    ctx.lineWidth = 14;
    ctx.strokeStyle = '#432f22';
    ctx.strokeRect(7, 7, w - 14, h - 14);

    ctx.lineWidth = 4;
    ctx.strokeStyle = '#9c7756';
    ctx.strokeRect(18, 18, w - 36, h - 36);

    const cx = w / 2;
    const cy = h / 2;

    ctx.fillStyle = '#b08a68';
    ctx.strokeStyle = '#362419';
    ctx.lineWidth = 6;

    if (type === 'lotus') {
      // 8-Petal Lotus flower
      const petals = 8;
      for (let i = 0; i < petals; i++) {
        const angle = (i * Math.PI * 2) / petals;
        ctx.save();
        ctx.translate(cx, cy);
        ctx.rotate(angle);
        ctx.beginPath();
        ctx.ellipse(0, -90, 36, 75, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();
        ctx.restore();
      }
      // Inner circle / pericarp
      ctx.beginPath();
      ctx.arc(cx, cy, 45, 0, Math.PI * 2);
      ctx.fillStyle = '#c79d77';
      ctx.fill();
      ctx.stroke();
    } else if (type === 'conch') {
      // Sacred Shankha (conch)
      ctx.save();
      ctx.translate(cx, cy);
      ctx.beginPath();
      ctx.moveTo(0, -110);
      ctx.bezierCurveTo(80, -90, 95, 30, 20, 110);
      ctx.bezierCurveTo(-30, 120, -50, 70, -40, 20);
      ctx.bezierCurveTo(-70, -20, -60, -90, 0, -110);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();

      // Spiral whorls
      ctx.beginPath();
      ctx.arc(10, 0, 35, 0, Math.PI);
      ctx.stroke();
      ctx.beginPath();
      ctx.arc(5, -35, 20, 0, Math.PI);
      ctx.stroke();
      ctx.restore();
    } else if (type === 'wheel') {
      // Sacred Sudarshana Chakra wheel
      ctx.beginPath();
      ctx.arc(cx, cy, 115, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();

      ctx.beginPath();
      ctx.arc(cx, cy, 80, 0, Math.PI * 2);
      ctx.fillStyle = '#6e513b';
      ctx.fill();
      ctx.stroke();

      // Spokes
      const spokes = 12;
      ctx.fillStyle = '#b08a68';
      for (let i = 0; i < spokes; i++) {
        const angle = (i * Math.PI * 2) / spokes;
        ctx.save();
        ctx.translate(cx, cy);
        ctx.rotate(angle);
        ctx.fillRect(-6, 25, 12, 65);
        ctx.strokeRect(-6, 25, 12, 65);
        ctx.restore();
      }

      ctx.beginPath();
      ctx.arc(cx, cy, 32, 0, Math.PI * 2);
      ctx.fillStyle = '#c79d77';
      ctx.fill();
      ctx.stroke();
    } else if (type === 'kalash') {
      // Sacred Kalash pot with mango leaves and coconut
      ctx.save();
      ctx.translate(cx, cy + 20);

      // Pot body
      ctx.beginPath();
      ctx.arc(0, 10, 80, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();

      // Pot base
      ctx.fillRect(-45, 75, 90, 25);
      ctx.strokeRect(-45, 75, 90, 25);

      // Neck & Rim
      ctx.fillRect(-40, -85, 80, 20);
      ctx.strokeRect(-40, -85, 80, 20);

      // Coconut atop
      ctx.beginPath();
      ctx.arc(0, -115, 35, 0, Math.PI * 2);
      ctx.fill();
      ctx.stroke();

      // Leaves
      ctx.beginPath();
      ctx.moveTo(-50, -80);
      ctx.lineTo(-85, -125);
      ctx.lineTo(-30, -95);
      ctx.fill();
      ctx.stroke();

      ctx.beginPath();
      ctx.moveTo(50, -80);
      ctx.lineTo(85, -125);
      ctx.lineTo(30, -95);
      ctx.fill();
      ctx.stroke();

      ctx.restore();
    }
  });
}

/**
 * Crimson silk cloth with golden lace border
 */
export function getRedSilkTexture() {
  return getOrCreateTexture('red_silk', (ctx, w, h) => {
    // Crimson base
    ctx.fillStyle = '#7a101c';
    ctx.fillRect(0, 0, w, h);

    // Fine gold zari / brocade threads
    ctx.strokeStyle = 'rgba(218, 165, 32, 0.4)';
    ctx.lineWidth = 1.5;
    for (let x = 0; x < w; x += 16) {
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, h);
      ctx.stroke();
    }
    for (let y = 0; y < h; y += 16) {
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(w, y);
      ctx.stroke();
    }

    // Rich gold border trim
    ctx.fillStyle = '#d4af37';
    ctx.fillRect(0, 0, w, 24);
    ctx.fillRect(0, h - 24, w, 24);
  });
}

/**
 * Jungle moss and damp earth texture
 */
export function getJungleGroundTexture() {
  return getOrCreateTexture('jungle_ground', (ctx, w, h) => {
    ctx.fillStyle = '#1e2417';
    ctx.fillRect(0, 0, w, h);

    // Patches of vibrant moss and loam
    for (let i = 0; i < 50; i++) {
      const x = Math.random() * w;
      const y = Math.random() * h;
      const r = Math.random() * 60 + 20;
      const grad = ctx.createRadialGradient(x, y, 4, x, y, r);
      grad.addColorStop(0, '#3a4f23');
      grad.addColorStop(0.6, '#283818');
      grad.addColorStop(1, 'rgba(30, 36, 23, 0)');
      ctx.fillStyle = grad;
      ctx.beginPath();
      ctx.arc(x, y, r, 0, Math.PI * 2);
      ctx.fill();
    }
  });
}

/**
 * Muddy footprint decal texture
 */
export function getFootprintTexture() {
  return getOrCreateTexture('footprint', (ctx, w, h) => {
    ctx.clearRect(0, 0, w, h);
    const cx = w / 2;
    const cy = h / 2;

    ctx.fillStyle = 'rgba(40, 24, 12, 0.85)';

    // Heel
    ctx.beginPath();
    ctx.ellipse(cx, cy + 50, 28, 38, 0, 0, Math.PI * 2);
    ctx.fill();

    // Sole
    ctx.beginPath();
    ctx.ellipse(cx, cy - 20, 36, 50, -0.05, 0, Math.PI * 2);
    ctx.fill();

    // Toes
    for (let i = -2; i <= 2; i++) {
      const tx = cx + i * 14;
      const ty = cy - 80 - Math.abs(i) * 4;
      const tr = 8 - Math.abs(i);
      ctx.beginPath();
      ctx.arc(tx, ty, tr, 0, Math.PI * 2);
      ctx.fill();
    }
  }, 256, 256);
}

/**
 * Parchment / palm-leaf scroll texture
 */
export function getParchmentTexture() {
  return getOrCreateTexture('parchment', (ctx, w, h) => {
    ctx.fillStyle = '#dfc28d';
    ctx.fillRect(0, 0, w, h);

    // Weathering stains
    for (let i = 0; i < 8; i++) {
      const x = Math.random() * w;
      const y = Math.random() * h;
      const r = Math.random() * 70 + 30;
      const g = ctx.createRadialGradient(x, y, 5, x, y, r);
      g.addColorStop(0, 'rgba(120, 85, 45, 0.35)');
      g.addColorStop(1, 'rgba(223, 194, 141, 0)');
      ctx.fillStyle = g;
      ctx.beginPath();
      ctx.arc(x, y, r, 0, Math.PI * 2);
      ctx.fill();
    }

    // Inscription text lines (Brahmi/Sanskrit feel)
    ctx.strokeStyle = 'rgba(50, 30, 15, 0.65)';
    ctx.lineWidth = 2.5;
    for (let y = 40; y < h - 40; y += 28) {
      ctx.beginPath();
      ctx.moveTo(35, y);
      ctx.lineTo(w - 35, y);
      ctx.stroke();
    }
  });
}
