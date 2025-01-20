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

  const { mutateAsync: saveGame, isPending } = useMutation<Game, AxiosError<FailResult>, CreateGame>({
    mutationFn: saveGameCall,
    onSuccess: async (data) => {
      await games.refetch();
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      onSuccess?.(data);
    },
    onError: onError,
  });

  const deleteGame = (id: number) => {
    void deleteGameCall(id)
      .then(() => {
        games.refetch();
        queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
        onDeleteSuccess?.();
      })
      .finally(() => {});
  };

  return {
    games,
    saveGame,
    isPending,
    deleteGame,
  };
};
