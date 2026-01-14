export interface Expansion {
  id: number;
  title: string;
  bggId: number;
  gameId: number | null;
}

export interface ExpansionUpdate {
  gameId: number;
  expansionBggIds: number[];
}
