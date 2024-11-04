import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { CreateSession } from '@/models/Session/CreateSession';
import { CreateLocation, FailResult, QUERY_KEYS, Location, Game, Session } from '@/models';
import { addSession } from '@/hooks/services/sessionService';
import { addLocation, getLocations } from '@/hooks/services/locationService';
import { getGame, getGames } from '@/hooks/services/gameService';

interface Props {
  gameId: string | undefined;
  onSessionSaveSuccess: () => void;
  onLocationSaveSuccess: () => void;
  onLocationSaveError: () => void;
}

export const useCreateSessionPage = ({
  gameId,
  onSessionSaveSuccess,
  onLocationSaveError,
  onLocationSaveSuccess,
}: Props) => {
  const queryClient = useQueryClient();

  const game = useQuery<Game, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, gameId],
    queryFn: ({ signal }) => getGame(gameId!, signal),
    enabled: gameId !== undefined,
  });

  const locations = useQuery<Location[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.locations],
    queryFn: ({ signal }) => getLocations(signal),
  });

  const games = useQuery<Game[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.games],
    queryFn: ({ signal }) => getGames(signal),
  });

  const saveLocation = useMutation<Location, AxiosError<FailResult>, CreateLocation>({
    mutationFn: addLocation,
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: [QUERY_KEYS.locations] });
    },
    onSettled: async () => {
      await locations.refetch();
    },
    onSuccess() {
      onLocationSaveSuccess();
    },
    onError: () => {
      onLocationSaveError();
    },
  });

  const saveSession = useMutation<Session, AxiosError<FailResult>, CreateSession>({
    mutationFn: addSession,
    async onSuccess(sessionResult) {
      onSessionSaveSuccess();

      const maps = sessionResult.playerSessions.map(async (x) => {
        return await queryClient.invalidateQueries({
          queryKey: [QUERY_KEYS.players, x.id, QUERY_KEYS.playerSessions],
        });
      });

      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.game, sessionResult.gameId],
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
