import { useMemo } from 'react';
import { useQueries } from '@tanstack/react-query';

import { getPlayers } from '@/services/queries/players';
import { getGames } from '@/services/queries/games';
import { getCompare } from '@/services/queries/compare';

interface Props {
  playerLeft: number;
  playerRight: number;
}

export const useCompareData = ({ playerLeft, playerRight }: Props) => {
  const [playersQuery, compareQuery, gamesQuery] = useQueries({
    queries: [getPlayers(), getCompare(playerLeft, playerRight), getGames()],
  });

  const players = useMemo(() => playersQuery?.data ?? [], [playersQuery.data]);
  const games = useMemo(() => gamesQuery?.data ?? [], [gamesQuery.data]);
  const compare = useMemo(() => compareQuery?.data, [compareQuery.data]);

  return {
    players,
    games,
    compare,
  };
};
