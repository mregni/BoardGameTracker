import { PlayPlayer } from '../';
import { PlayFlag } from '../Games/PlayFlag';

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
