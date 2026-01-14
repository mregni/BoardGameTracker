import { SessionFlag } from '../Games/SessionFlag';
import { Expansion } from '../Games/Expansion';

import { PlayerSession } from './PlayerSession';

export interface Session {
  id: number;
  comment: string;
  ended: boolean;
  gameId: number;
  start: Date;
  minutes: number;
  playerSessions: PlayerSession[];
  expansions: Expansion[];
  locationId: string;
  flags: SessionFlag[];
}
