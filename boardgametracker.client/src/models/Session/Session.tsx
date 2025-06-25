import { SessionFlag } from '../Games/SessionFlag';
import { Expansion } from '../Games/Expansion';

import { PlayerSession } from './PlayerSession';

export interface Session {
  id: string;
  comment: string;
  ended: boolean;
  gameId: string;
  start: Date;
  minutes: number;
  playerSessions: PlayerSession[];
  expansions: Expansion[];
  locationId: string;
  flags: SessionFlag[];
}
