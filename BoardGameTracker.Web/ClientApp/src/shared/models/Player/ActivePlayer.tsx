export interface ActivePlayer {
  uiId: number;
  id: number;
  won: boolean;
  firstPlay: boolean;
  color?: string;
  score?: number;
  team?: string;
  characterName?: string;
}