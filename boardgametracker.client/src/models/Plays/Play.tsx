import { PlayFlag } from '../Games/PlayFlag';
import { PlayPlayer } from '../';

export interface Play {
  id: number;
  comment: string;
  ended: boolean;
  gameId: number;
  start: Date;
  minutes: number;
  players: PlayPlayer[];
  locationId: number;
  playFlags: PlayFlag[];
}
