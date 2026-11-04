import * as THREE from 'three';

export const QUALITY_LEVELS = {
  HIGH: 'high',
  MEDIUM: 'medium',
  LOW: 'low'
};

class QualityManager {
  constructor() {
    this.level =
      typeof localStorage !== 'undefined' && localStorage.getItem('lot_quality')
        ? localStorage.getItem('lot_quality')
        : QUALITY_LEVELS.HIGH;
    this.renderer = null;
  }

  init(renderer) {
    this.renderer = renderer;
    this.apply();

    // Dev/Player shortcut: Press 'Q' to cycle quality
    if (typeof window !== 'undefined') {
      window.addEventListener('keydown', (e) => {
        if (e.key === 'q' || e.key === 'Q') {
          this.cycle();
        }
      });
    }
  }

  cycle() {
    if (this.level === QUALITY_LEVELS.HIGH) {
      this.set(QUALITY_LEVELS.MEDIUM);
    } else if (this.level === QUALITY_LEVELS.MEDIUM) {
      this.set(QUALITY_LEVELS.LOW);
    } else {
      this.set(QUALITY_LEVELS.HIGH);
    }
  }

  set(level) {
    this.level = level;
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('lot_quality', level);
    }
    this.apply();
    this.showToast(`Graphics Quality: ${level.toUpperCase()} (Target 60 FPS)`);
  }

  apply() {
    if (!this.renderer) return;

    const dpr = typeof window !== 'undefined' ? window.devicePixelRatio : 1;

    if (this.level === QUALITY_LEVELS.LOW) {
      this.renderer.shadowMap.enabled = false;
      this.renderer.setPixelRatio(1.0);
    } else if (this.level === QUALITY_LEVELS.MEDIUM) {
      this.renderer.shadowMap.enabled = true;
      this.renderer.shadowMap.type = THREE.BasicShadowMap;
      this.renderer.setPixelRatio(Math.min(dpr, 1.25));
    } else {
      // HIGH
      this.renderer.shadowMap.enabled = true;
      this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
      this.renderer.setPixelRatio(Math.min(dpr, 2.0));
    }
  }

  showToast(message) {
    if (typeof document === 'undefined') return;

    let toast = document.getElementById('quality-toast');
    if (!toast) {
      toast = document.createElement('div');
      toast.id = 'quality-toast';
      toast.style.position = 'fixed';
      toast.style.bottom = '20px';
      toast.style.right = '20px';
      toast.style.background = 'rgba(20, 24, 32, 0.9)';
      toast.style.border = '1px solid #d4af37';
      toast.style.color = '#f5c542';
      toast.style.padding = '8px 16px';
      toast.style.borderRadius = '6px';
      toast.style.fontFamily = 'monospace';
      toast.style.fontSize = '12px';
      toast.style.zIndex = '9999';
      toast.style.pointerEvents = 'none';
      toast.style.transition = 'opacity 0.4s ease';
      document.body.appendChild(toast);
    }
    toast.textContent = message;
    toast.style.opacity = '1';
    clearTimeout(this._toastTimeout);
    this._toastTimeout = setTimeout(() => {
      if (toast) toast.style.opacity = '0';
    }, 2500);
  }
}

export const quality = new QualityManager();
export default quality;
