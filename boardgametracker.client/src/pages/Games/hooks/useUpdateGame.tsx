import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { FailResult, QUERY_KEYS, Game } from '@/models';
import { getGame, updateGameCall } from '@/hooks/services/gameService';

interface Props {
  id: string | undefined;
  onGameUpdateSuccess: (game: Game) => void;
}

export const useUpdateGame = (props: Props) => {
  const { id, onGameUpdateSuccess } = props;
  const queryClient = useQueryClient();

  const { data: game } = useQuery<Game, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id],
    queryFn: ({ signal }) => getGame(id!, signal),
    enabled: id !== undefined,
  });

  const gameMutation = useMutation<Game, AxiosError<FailResult>, Game>({
    mutationFn: updateGameCall,
    async onSuccess(result) {
      onGameUpdateSuccess(result);

      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.counts],
      });
      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.games],
      });
    },
  });

  return {
    game,
    updateGame: gameMutation.mutateAsync,
    updateIsPending: gameMutation.isPending,
  };
};
