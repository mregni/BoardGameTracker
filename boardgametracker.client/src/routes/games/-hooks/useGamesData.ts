import { useMemo } from 'react';
import { useQueries } from '@tanstack/react-query';

import { getGames } from '@/services/queries/games';

export const useGamesData = () => {
  const [gamesQuery] = useQueries({
    queries: [getGames()],
  });

  const games = useMemo(() => gamesQuery.data ?? [], [gamesQuery.data]);
  const isLoading = gamesQuery.isLoading;

  return {
    isLoading,
    games,
  };
};
