import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { CreateGame } from '@/models/Games/CreateGame';
import { FailResult, Game, QUERY_KEYS } from '@/models';
import { deleteGameCall, getGames, saveGameCall } from '@/hooks/services/gameService';

interface Props {
  onSuccess?: (state: Game) => void;
  onError?: () => void;
  onDeleteSuccess?: () => void;
}

export const useGames = (props: Props) => {
  const { onSuccess, onError, onDeleteSuccess } = props;
  const queryClient = useQueryClient();

  const games = useQuery<Game[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.games],
    queryFn: ({ signal }) => getGames(signal),
  });

  const { mutateAsync: saveGame, isPending: saveIsPending } = useMutation<Game, AxiosError<FailResult>, CreateGame>({
    mutationFn: saveGameCall,
    onSuccess: async (data) => {
      await games.refetch();
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      onSuccess?.(data);
    },
    onError: onError,
  });

  const deleteGame = async (id: string) => {
    await deleteGameCall(id);
    await games.refetch();
    queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
    onDeleteSuccess?.();
  };

  return {
    games: games.data ?? [],
    saveGame,
    saveIsPending,
    deleteGame,
  };
};
