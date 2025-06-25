import { useMemo } from 'react';
import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { getGame } from '@/services/queries/games';
import { updateGameCall } from '@/services/gameService';
import { QUERY_KEYS } from '@/models';

interface Props {
  gameId: string;
  onUpdateSuccess?: () => void;
  onUpdateError?: () => void;
}
export const useUpdateGame = ({ gameId, onUpdateSuccess, onUpdateError }: Props) => {
  const queryClient = useQueryClient();

  const [gameQuery] = useQueries({
    queries: [getGame(gameId)],
  });

  const game = useMemo(() => gameQuery.data, [gameQuery.data]);

  const saveGameMutation = useMutation({
    mutationFn: updateGameCall,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      onUpdateSuccess?.();
    },
    onError: () => {
      onUpdateError?.();
    },
  });

  const isLoading = gameQuery.isLoading || saveGameMutation.isPending;

  return {
    isLoading,
    game,
    updateGame: saveGameMutation.mutateAsync,
  };
};
