import { useMemo } from 'react';
import { useQueries } from '@tanstack/react-query';

import { getPlayers } from '@/services/queries/players';

export const usePlayersData = () => {
  const [playersQuery] = useQueries({
    queries: [getPlayers()],
  });

  const players = useMemo(() => playersQuery.data ?? [], [playersQuery.data]);
  const isLoading = playersQuery.isLoading;

  return {
    players,
    isLoading,
  };
};
