/**
 * Cinematic loading overlay for scene transitions
 */
class LoadingOverlay {
  constructor() {
    this.container = null;
    this.textEl = null;
    this.init();
  }

  init() {
    if (typeof document === 'undefined') return;

    if (document.getElementById('world-loading-overlay')) {
      this.container = document.getElementById('world-loading-overlay');
      this.textEl = document.getElementById('world-loading-text');
      return;
    }

    const overlay = document.createElement('div');
    overlay.id = 'world-loading-overlay';
    overlay.style.position = 'fixed';
    overlay.style.top = '0';
    overlay.style.left = '0';
    overlay.style.width = '100vw';
    overlay.style.height = '100vh';
    overlay.style.background = '#090c10';
    overlay.style.zIndex = '9998';
    overlay.style.display = 'flex';
    overlay.style.flexDirection = 'column';
    overlay.style.alignItems = 'center';
    overlay.style.justifyContent = 'center';
    overlay.style.color = '#e6edf3';
    overlay.style.fontFamily = 'serif';
    overlay.style.letterSpacing = '0.12em';
    overlay.style.transition = 'opacity 0.45s cubic-bezier(0.4, 0, 0.2, 1)';
    overlay.style.opacity = '1';

    // Spinner Chakra / Temple Wheel
    const spinner = document.createElement('div');
    spinner.style.width = '60px';
    spinner.style.height = '60px';
    spinner.style.border = '3px solid rgba(218, 165, 32, 0.25)';
    spinner.style.borderTop = '3px solid #f5c542';
    spinner.style.borderRadius = '50%';
    spinner.style.animation = 'lot-spin 1.2s linear infinite';
    spinner.style.marginBottom = '20px';

    // Inject CSS keyframe for spinner
    const style = document.createElement('style');
    style.textContent = `
      @keyframes lot-spin {
        0% { transform: rotate(0deg); }
        100% { transform: rotate(360deg); }
      }
    `;
    document.head.appendChild(style);

    const title = document.createElement('h2');
    title.textContent = 'LEGACY OF TEMPLES';
    title.style.color = '#f5c542';
    title.style.fontSize = '1.2rem';
    title.style.marginBottom = '8px';
    title.style.fontWeight = '500';

    const text = document.createElement('p');
    text.id = 'world-loading-text';
    text.textContent = 'Uncovering ancient sanctum...';
    text.style.fontSize = '0.9rem';
    text.style.color = '#8b949e';

    overlay.appendChild(spinner);
    overlay.appendChild(title);
    overlay.appendChild(text);
    document.body.appendChild(overlay);

    this.container = overlay;
    this.textEl = text;
  }

  show(sceneTitle = 'Loading...') {
    if (typeof document === 'undefined') return;
    if (!this.container) this.init();
    if (this.textEl) this.textEl.textContent = sceneTitle;
    if (this.container) {
      this.container.style.pointerEvents = 'auto';
      this.container.style.opacity = '1';
    }
  }

  hide() {
    if (!this.container) return;
    this.container.style.opacity = '0';
    this.container.style.pointerEvents = 'none';
  }
}

export const overlay = new LoadingOverlay();
export default overlay;
