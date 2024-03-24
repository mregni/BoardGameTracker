export interface PlayPlayer {
  id: number;
  playerId: number;
  won: boolean;
  firstPlay: boolean;
  color?: string;
  score?: number;
  team?: string;
  characterName?: string;
}
