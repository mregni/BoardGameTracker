import { SessionFlag } from '../Games/SessionFlag';

import { PlayerSession } from './PlayerSession';

export interface Session {
  id: string;
  comment: string;
  ended: boolean;
  gameId: string;
  start: Date;
  minutes: number;
  playerSessions: PlayerSession[];
  locationId: string;
  flags: SessionFlag[];
}
