import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { getGame } from '@/services/queries/games';
import { updateGameCall } from '@/services/gameService';
import { useToasts } from '@/routes/-hooks/useToasts';
import { QUERY_KEYS } from '@/models';

interface Props {
  gameId: number;
  onSuccess?: () => void;
}
export const useUpdateGame = ({ gameId, onSuccess }: Props) => {
  const queryClient = useQueryClient();
  const { successToast, errorToast } = useToasts();

  const [gameQuery] = useQueries({
    queries: [getGame(gameId)],
  });

  const game = gameQuery.data;

  const saveGameMutation = useMutation({
    mutationFn: updateGameCall,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games, gameId] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
      successToast('game.notifications.updated');
      onSuccess?.();
    },
    onError: () => {
      errorToast('game.notifications.update-failed');
    },
  });

  const isLoading = gameQuery.isLoading || saveGameMutation.isPending;

  return {
    isLoading,
    game,
    updateGame: saveGameMutation.mutateAsync,
  };
};
