import { EVENTS, SCENE_IDS, CHARACTER_IDS } from '../core/constants.js';

/**
 * Initializes gameplay loop, state machine, quests, and event handling.
 *
 * CONTRACT:
 * export async function initGameplay({scene, bus, world, characters}) ->
 *   { update(dt), start() }
 */
export async function initGameplay({ scene, bus, world, characters }) {
  const state = {
    phase: 'idle', // idle, investigating, puzzle_active, idol_secured, complete
    clues: new Set(),
    activeInteractable: null,
    dialogueActive: false
  };

  /**
   * Start the main gameplay progression
   */
  function start() {
    state.phase = 'investigating';
    bus.emit(EVENTS.GAME_STATE, {
      phase: state.phase,
      cluesCount: state.clues.size,
      message: 'Investigate the sacred temple courtyard. Speak with the Priest or examine relics.'
    });
    console.log('[Gameplay] Started: Phase =', state.phase);
  }

  // Handle player interactions
  bus.on(EVENTS.PLAYER_INTERACT, () => {
    if (state.dialogueActive) {
      state.dialogueActive = false;
      bus.emit(EVENTS.DIALOGUE_END, {});
      return;
    }

    if (!characters.player) return;
    const playerPos = characters.player.position;

    // Check interaction with registered world interactables
    for (const item of world.interactables) {
      if (!item.object3d) continue;
      const dist = playerPos.distanceTo(item.object3d.position);
      if (dist <= item.radius) {
        handleInteractable(item);
        return;
      }
    }

    // Check interaction with NPCs
    for (const [id, npc] of Object.entries(characters.npcs)) {
      if (!npc.position) continue;
      const dist = playerPos.distanceTo(npc.position);
      if (dist <= 3.0) {
        handleNPCInteraction(id);
        return;
      }
    }
  });

  function handleInteractable(item) {
    if (item.id.includes('altar') || item.id.includes('core')) {
      const clueId = `ancient_inscription_${world.colliders.length}`;
      if (!state.clues.has(clueId)) {
        state.clues.add(clueId);
        bus.emit(EVENTS.CLUE_FOUND, {
          id: clueId,
          description: 'Discovered ancient Brahmi inscriptions detailing the temple sanctum layout.'
        });
      }

      bus.emit(EVENTS.DIALOGUE_START, {
        speaker: 'Ancient Altar',
        text: 'The pedestal is empty. The sacred golden idol was removed in haste under the moon.'
      });
      state.dialogueActive = true;
    }
  }

  function handleNPCInteraction(npcId) {
    state.dialogueActive = true;
    switch (npcId) {
      case CHARACTER_IDS.PRIEST:
        bus.emit(EVENTS.DIALOGUE_START, {
          speaker: 'Temple Priest',
          text: 'Investigator! The sanctum was desecrated last night! Shadows fled towards the jungle path.'
        });
        break;
      case CHARACTER_IDS.VILLAGER:
        bus.emit(EVENTS.DIALOGUE_START, {
          speaker: 'Villager',
          text: 'I saw two suspicious figures carrying heavy sacks past the courtyard gates!'
        });
        break;
      default:
        bus.emit(EVENTS.DIALOGUE_START, {
          speaker: npcId,
          text: '...'
        });
        break;
    }
  }

  // Event handlers for core bus events
  bus.on(EVENTS.CLUE_FOUND, ({ id, description }) => {
    console.log(`[Gameplay] Clue discovered: ${id} - ${description}`);
  });

  bus.on(EVENTS.PUZZLE_SOLVED, ({ puzzleId }) => {
    console.log(`[Gameplay] Puzzle solved: ${puzzleId}`);
  });

  bus.on(EVENTS.IDOL_RECOVERED, () => {
    state.phase = 'idol_secured';
    console.log('[Gameplay] Sacred idol recovered!');
    bus.emit(EVENTS.GAME_STATE, {
      phase: state.phase,
      cluesCount: state.clues.size,
      message: 'The sacred idol is secured! Return it to the sanctum.'
    });
  });

  bus.on(EVENTS.DIALOGUE_END, () => {
    state.dialogueActive = false;
  });

  /**
   * Per-frame gameplay update
   */
  function update(dt) {
    if (!characters.player) return;
    const playerPos = characters.player.position;

    // Detect closest interactable for UI prompt
    let nearest = null;
    let minDistance = Infinity;

    for (const item of world.interactables) {
      if (!item.object3d) continue;
      const d = playerPos.distanceTo(item.object3d.position);
      if (d <= item.radius && d < minDistance) {
        minDistance = d;
        nearest = item;
      }
    }

    if (!nearest) {
      for (const [id, npc] of Object.entries(characters.npcs)) {
        if (!npc.position) continue;
        const d = playerPos.distanceTo(npc.position);
        if (d <= 3.0 && d < minDistance) {
          minDistance = d;
          nearest = {
            id: `npc_${id}`,
            prompt: `Press [E] to talk to ${npc.meta?.name || id}`
          };
        }
      }
    }

    if (nearest !== state.activeInteractable) {
      state.activeInteractable = nearest;
      if (nearest) {
        bus.emit('ui:prompt:show', { text: nearest.prompt });
      } else {
        bus.emit('ui:prompt:hide');
      }
    }
  }

  return {
    update,
    start
  };
}

export default initGameplay;

