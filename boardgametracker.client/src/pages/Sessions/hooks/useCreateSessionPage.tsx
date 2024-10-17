import { AxiosError } from 'axios';
import { useMutation, UseMutationResult, useQuery, useQueryClient, UseQueryResult } from '@tanstack/react-query';

import { useToast } from '@/providers/BgtToastProvider';
import { CreateSession } from '@/models/Session/CreateSession';
import { CreateLocation, FailResult, ListResult, QUERY_KEYS, Result, Location, Session, Game } from '@/models';
import { addSession } from '@/hooks/services/sessionService';
import { addLocation, getLocations } from '@/hooks/services/locationService';
import { getGame, getGames } from '@/hooks/services/gameService';

interface Props {
  locations: UseQueryResult<ListResult<Location>, AxiosError<FailResult>>;
  saveLocation: UseMutationResult<Result<Location>, AxiosError<FailResult>, CreateLocation, unknown>;
  saveSession: UseMutationResult<Result<Session>, AxiosError<FailResult>, CreateSession, unknown>;
  games: UseQueryResult<ListResult<Game>, AxiosError<FailResult>>;
  game: UseQueryResult<Result<Game>, AxiosError<FailResult>>;
}

export const useCreateSessionPage = (gameId: string | undefined): Props => {
  const queryClient = useQueryClient();
  const { showInfoToast, showErrorToast } = useToast();

  const game = useQuery<Result<Game>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, gameId],
    queryFn: ({ signal }) => getGame(gameId!, signal),
    enabled: gameId !== undefined,
  });

  const locations = useQuery<ListResult<Location>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.locations],
    queryFn: ({ signal }) => getLocations(signal),
  });

  const games = useQuery<ListResult<Game>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.games],
    queryFn: ({ signal }) => getGames(signal),
  });

  const saveLocation = useMutation<Result<Location>, AxiosError<FailResult>, CreateLocation>({
    mutationFn: addLocation,
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: [QUERY_KEYS.locations] });
    },
    onSettled: async () => {
      await locations.refetch();
    },
    onSuccess(data) {
      const previousLocations = queryClient.getQueryData<ListResult<Location>>([QUERY_KEYS.locations]);

      if (previousLocations !== undefined) {
        previousLocations.count = previousLocations.count + 1;
        previousLocations.list = [...previousLocations.list, data.model];
        queryClient.setQueryData([QUERY_KEYS.locations], previousLocations);
      }

      showInfoToast('location.notifications.created');
    },
    onError: () => {
      showErrorToast('location.notifications.failed');
    },
  });

  const saveSession = useMutation<Result<Session>, AxiosError<FailResult>, CreateSession>({
    mutationFn: addSession,
    async onSuccess(sessionResult) {
      showInfoToast('player-session.new.notifications.created');
      const maps = sessionResult.model.playerSessions.map(async (x) => {
        return await queryClient.invalidateQueries({
          queryKey: [QUERY_KEYS.players, x.id, QUERY_KEYS.playerSessions],
        });
      });

      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.game, sessionResult.model.gameId],
      });

      await Promise.all(maps);
    },
  });

  return {
    locations,
    saveLocation,
    saveSession,
    games,
    game,
  };
};
