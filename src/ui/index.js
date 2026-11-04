import { EVENTS } from '../core/constants.js';

/**
 * Initializes the DOM-based UI overlay and binds EventBus events.
 */
export async function initUI({ bus } = {}) {
  const statusEl = document.getElementById('hud-status');
  const promptEl = document.getElementById('hud-prompt');
  const dialogueEl = document.getElementById('hud-dialogue');
  const speakerEl = document.getElementById('dialogue-speaker');
  const messageEl = document.getElementById('dialogue-message');

  function showPrompt(text) {
    if (promptEl) {
      promptEl.textContent = text;
      promptEl.classList.remove('hidden');
    }
  }

  function hidePrompt() {
    if (promptEl) {
      promptEl.classList.add('hidden');
    }
  }

  function showDialogue(speaker, message) {
    if (dialogueEl && speakerEl && messageEl) {
      speakerEl.textContent = speaker;
      messageEl.textContent = message;
      dialogueEl.classList.remove('hidden');
    }
  }

  function hideDialogue() {
    if (dialogueEl) {
      dialogueEl.classList.add('hidden');
    }
  }

  function updateStatus(text) {
    if (statusEl) {
      statusEl.textContent = text;
    }
  }

  if (bus) {
    bus.on(EVENTS.GAME_STATE, (data) => {
      if (data?.message) updateStatus(data.message);
    });

    bus.on(EVENTS.SCENE_CHANGED, ({ sceneId }) => {
      updateStatus(`Entered scene: ${sceneId.replace('_', ' ').toUpperCase()}`);
    });

    bus.on(EVENTS.CLUE_FOUND, ({ id, description }) => {
      updateStatus(`Clue Found: ${description}`);
    });

    bus.on(EVENTS.DIALOGUE_START, ({ speaker, text }) => {
      showDialogue(speaker, text);
    });

    bus.on(EVENTS.DIALOGUE_END, () => {
      hideDialogue();
    });

    bus.on('ui:prompt:show', ({ text }) => {
      showPrompt(text);
    });

    bus.on('ui:prompt:hide', () => {
      hidePrompt();
    });
  }

  function update(dt) {
    // Optional UI tick updates
  }

  return {
    update,
    showPrompt,
    hidePrompt,
    showDialogue,
    hideDialogue,
    updateStatus
  };
}

export default initUI;

