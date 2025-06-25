import { useMutation, useQueryClient } from '@tanstack/react-query';

import { addGameWithBggCall } from '@/services/gameService';
import { Game, QUERY_KEYS } from '@/models';

interface Props {
  onSuccess?: (game: Game) => void;
}

export const useBggGameModal = ({ onSuccess }: Props) => {
  const queryClient = useQueryClient();

  const addGameMutation = useMutation({
    mutationFn: addGameWithBggCall,
    async onSuccess(data) {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });

      onSuccess?.(data);
    },
  });

  return {
    save: addGameMutation.mutateAsync,
    isPending: addGameMutation.isPending,
  };
};
