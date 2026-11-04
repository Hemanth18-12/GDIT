/**
 * Core constants for Legacy of Temples
 */

export const SCENE_IDS = {
  COURTYARD: 'courtyard',
  SANCTUM: 'sanctum',
  PUZZLE_WALL: 'puzzle_wall',
  SHRINE_CRIME: 'shrine_crime',
  JUNGLE_PATH: 'jungle_path'
};

export const CHARACTER_IDS = {
  INVESTIGATOR: 'investigator',
  ROBBER1: 'robber1',
  ROBBER2: 'robber2',
  PRIEST: 'priest',
  VILLAGER: 'villager'
};

export const EVENTS = {
  SCENE_CHANGED: 'scene:changed',
  PLAYER_INTERACT: 'player:interact',
  CLUE_FOUND: 'clue:found',
  PUZZLE_SOLVED: 'puzzle:solved',
  DIALOGUE_START: 'dialogue:start',
  DIALOGUE_END: 'dialogue:end',
  IDOL_RECOVERED: 'idol:recovered',
  GAME_STATE: 'game:state'
};

export const DEFAULT_SCENE = SCENE_IDS.COURTYARD;
export const DEFAULT_PLAYER = CHARACTER_IDS.INVESTIGATOR;

