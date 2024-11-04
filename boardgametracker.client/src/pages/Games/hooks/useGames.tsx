import { AxiosError } from 'axios';
import { useQuery } from '@tanstack/react-query';

import { FailResult, Game, QUERY_KEYS } from '@/models';
import { getGames } from '@/hooks/services/gameService';

export const useGames = () => {
  const games = useQuery<Game[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.games],
    queryFn: ({ signal }) => getGames(signal),
  });

  return {
    games,
  };
};
