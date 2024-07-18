import { SessionFlag } from '../Games/SessionFlag';

import { PlayerSession } from './PlayerSession';

export interface Session {
  id: number;
  comment: string;
  ended: boolean;
  gameId: number;
  start: Date;
  minutes: number;
  playerSessions: PlayerSession[];
  locationId: number;
  flags: SessionFlag[];
}
