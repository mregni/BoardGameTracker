import { useQuery } from '@tanstack/react-query';

import { getGames } from '@/services/queries/games';

export const useGamesData = () => {
  const { data, isLoading } = useQuery(getGames());

  return {
    isLoading,
    games: data ?? [],
  };
};
