import { ReactNode } from 'react';
import { formatDuration, intervalToDuration } from 'date-fns';

import { getDateFnsLocale } from '@/utils/localeUtils';
import Trophy from '@/assets/icons/trophy.svg?react';
import TrendingUp from '@/assets/icons/trend-up.svg?react';
import GamePad from '@/assets/icons/gamepad.svg?react';
import Clock from '@/assets/icons/clock.svg?react';

export interface CompareData {
  winCount: { playerOne: number; playerTwo: number };
  winPercentage: { playerOne: number; playerTwo: number };
  sessionCounts: { playerOne: number; playerTwo: number };
  totalDuration: { playerOne: number; playerTwo: number };
}

export interface StatConfig {
  key: string;
  translationKey: string;
  icon: ReactNode;
  getRawValue: (data: CompareData, player: 'playerOne' | 'playerTwo') => number;
  getValue: (data: CompareData, player: 'playerOne' | 'playerTwo') => string | number;
}

const formatMinutesToDuration = (minutes: number, uiLanguage: string): string => {
  const duration = intervalToDuration({
    start: 0,
    end: minutes * 60 * 1000, // Convert minutes to milliseconds
  });

  const locale = getDateFnsLocale(uiLanguage);

  return formatDuration(duration, {
    format: ['years', 'months', 'weeks', 'days', 'hours', 'minutes'],
    locale,
  });
};

export const getStatConfigs = (uiLanguage: string): StatConfig[] => [
  {
    key: 'winCount',
    translationKey: 'compare.stats.total-wins',
    icon: <Trophy className="size-6" />,
    getRawValue: (data, player) => data.winCount[player],
    getValue: (data, player) => data.winCount[player],
  },
  {
    key: 'winPercentage',
    translationKey: 'compare.stats.win-percentage',
    icon: <TrendingUp className="size-6" />,
    getRawValue: (data, player) => data.winPercentage[player],
    getValue: (data, player) => (data.winPercentage[player] * 100).toFixed(0) + '%',
  },
  {
    key: 'sessionCounts',
    translationKey: 'compare.stats.sessions',
    icon: <GamePad className="size-6" />,
    getRawValue: (data, player) => data.sessionCounts[player],
    getValue: (data, player) => data.sessionCounts[player],
  },
  {
    key: 'totalDuration',
    translationKey: 'compare.stats.total-duration',
    icon: <Clock className="size-6" />,
    getRawValue: (data, player) => data.totalDuration[player],
    getValue: (data, player) => formatMinutesToDuration(data.totalDuration[player], uiLanguage),
  },
];

export const isWinningValue = (playerValue: string | number, opponentValue: string | number): boolean => {
  if (typeof playerValue === 'number' && typeof opponentValue === 'number') {
    return playerValue > opponentValue;
  }
  return String(playerValue).replace(/[^\d.]/g, '') > String(opponentValue).replace(/[^\d.]/g, '');
};

export const calculateWinCount = (
  compare: CompareData,
  playerKey: 'playerOne' | 'playerTwo',
  uiLanguage: string
): number => {
  const opponentKey = playerKey === 'playerOne' ? 'playerTwo' : 'playerOne';
  const statConfigs = getStatConfigs(uiLanguage);

  return statConfigs.reduce((winCount, stat) => {
    const playerValue = stat.getValue(compare, playerKey);
    const opponentValue = stat.getValue(compare, opponentKey);
    const isWinner = isWinningValue(playerValue, opponentValue);

    return isWinner ? winCount + 1 : winCount;
  }, 0);
};

export const calculateOverallWinner = (compare: CompareData, uiLanguage: string): 'playerOne' | 'playerTwo' | null => {
  const playerOneWins = calculateWinCount(compare, 'playerOne', uiLanguage);
  const playerTwoWins = calculateWinCount(compare, 'playerTwo', uiLanguage);

  if (playerOneWins > playerTwoWins) return 'playerOne';
  if (playerTwoWins > playerOneWins) return 'playerTwo';
  return null; // tie
};
