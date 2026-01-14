import { useQueries } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getPlayers } from '@/services/queries/players';
import { getCompare } from '@/services/queries/compare';

interface Props {
  playerLeft: number;
  playerRight: number;
}

export const useCompareData = ({ playerLeft, playerRight }: Props) => {
  const [playersQuery, compareQuery, settingsQuery] = useQueries({
    queries: [getPlayers(), getCompare(playerLeft, playerRight), getSettings()],
  });

  const players = playersQuery?.data ?? [];
  const compare = compareQuery?.data;
  const settings = settingsQuery?.data;

  return {
    players,
    compare,
    settings,
  };
};
