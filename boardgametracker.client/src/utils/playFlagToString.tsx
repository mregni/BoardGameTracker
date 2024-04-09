import {PlayFlag} from '../models/Games/PlayFlag';

export const PlayFlagToString = (flag: PlayFlag): string => {
  switch (flag) {
    case PlayFlag.LongestGame:
      return 'common.flags.longest-game';
    case PlayFlag.ShortestGame:
      return 'common.flags.shortest-game';
    case PlayFlag.HighestScore:
      return 'common.flags.highest-score';
    case PlayFlag.LowestScore:
      return 'common.flags.lowest-score';
  }
}