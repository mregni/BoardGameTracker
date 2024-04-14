import {PlayFlag} from '../models/Games/PlayFlag';

export const StringToColor = (value: string) => {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = value.charCodeAt(i) + ((hash << 5) - hash);
  }

  return `hsl(${hash % 360}, 85%, 35%)`;
};

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