import * as THREE from 'three';

/**
 * Collision manager for wall and obstacle avoidance
 */
export class CollisionSystem {
  constructor() {
    this.colliders = [];
    this.boxes = [];
    this.playerRadius = 0.45;
  }

  clear() {
    this.colliders.length = 0;
    this.boxes.length = 0;
  }

  /**
   * Add a mesh, Box3, or bounding geometry as a solid obstacle.
   */
  add(item) {
    let box = null;
    if (item.isBox3) {
      box = item.clone();
      this.colliders.push(item);
    } else if (item.geometry) {
      item.updateMatrixWorld(true);
      box = new THREE.Box3().setFromObject(item);
      this.colliders.push(item);
    } else if (item.box && item.box.isBox3) {
      box = item.box.clone();
      this.colliders.push(item);
    }

    if (box) {
      this.boxes.push(box);
    }
  }

  /**
   * Add a box obstacle defined by center position and dimensions.
   */
  addBox(x, y, z, width, height, depth) {
    const min = new THREE.Vector3(x - width / 2, y - height / 2, z - depth / 2);
    const max = new THREE.Vector3(x + width / 2, y + height / 2, z + depth / 2);
    const box = new THREE.Box3(min, max);
    this.boxes.push(box);
    this.colliders.push(box);
    return box;
  }

  /**
   * Add scene boundary walls so player stays inside play area.
   */
  addPerimeter(halfWidth, halfDepth, height = 10) {
    const thickness = 2.0;
    // North wall (+Z)
    this.addBox(0, height / 2, halfDepth + thickness / 2, halfWidth * 2 + thickness * 2, height, thickness);
    // South wall (-Z)
    this.addBox(0, height / 2, -halfDepth - thickness / 2, halfWidth * 2 + thickness * 2, height, thickness);
    // East wall (+X)
    this.addBox(halfWidth + thickness / 2, height / 2, 0, thickness, height, halfDepth * 2 + thickness * 2);
    // West wall (-X)
    this.addBox(-halfWidth - thickness / 2, height / 2, 0, thickness, height, halfDepth * 2 + thickness * 2);
  }

  /**
   * Resolve player penetration against all active obstacle boxes.
   * Modifies playerPos in place.
   */
  resolve(playerPos) {
    if (!playerPos) return;

    const r = this.playerRadius;
    const pMinY = playerPos.y;
    const pMaxY = playerPos.y + 1.8;

    for (let i = 0; i < this.boxes.length; i++) {
      const box = this.boxes[i];

      // Check vertical overlap
      if (pMaxY < box.min.y || pMinY > box.max.y) {
        continue;
      }

      // Check horizontal AABB intersection with player radius
      const closestX = Math.max(box.min.x, Math.min(playerPos.x, box.max.x));
      const closestZ = Math.max(box.min.z, Math.min(playerPos.z, box.max.z));

      const dx = playerPos.x - closestX;
      const dz = playerPos.z - closestZ;
      const distSq = dx * dx + dz * dz;

      if (distSq < r * r) {
        const dist = Math.sqrt(distSq);

        if (dist > 0.0001) {
          // Push player away from nearest point on box surface
          const push = r - dist;
          playerPos.x += (dx / dist) * push;
          playerPos.z += (dz / dist) * push;
        } else {
          // Player is strictly inside the box: push along minimal penetration axis
          const overlapLeft = playerPos.x - box.min.x;
          const overlapRight = box.max.x - playerPos.x;
          const overlapBottom = playerPos.z - box.min.z;
          const overlapTop = box.max.z - playerPos.z;

          const minOverlap = Math.min(overlapLeft, overlapRight, overlapBottom, overlapTop);

          if (minOverlap === overlapLeft) playerPos.x = box.min.x - r;
          else if (minOverlap === overlapRight) playerPos.x = box.max.x + r;
          else if (minOverlap === overlapBottom) playerPos.z = box.min.z - r;
          else playerPos.z = box.max.z + r;
        }
      }
    }
  }
}

export default CollisionSystem;
