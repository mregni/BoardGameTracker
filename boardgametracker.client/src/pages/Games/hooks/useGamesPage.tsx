import { AxiosError } from 'axios';
import { useQuery, UseQueryResult } from '@tanstack/react-query';

import { FailResult, Game, ListResult, QUERY_KEYS } from '@/models';
import { getGames } from '@/hooks/services/gameService';

interface Props {
  games: UseQueryResult<ListResult<Game>, AxiosError<FailResult>>;
}

export const useGamesPage = (): Props => {
  const games = useQuery<ListResult<Game>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.games],
    queryFn: ({ signal }) => getGames(signal),
  });

  return {
    games,
  };
};
