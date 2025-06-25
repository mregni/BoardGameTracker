import { useMemo } from 'react';
import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { getGameExpansions } from '@/services/queries/games';
import { saveGameExpansionCall } from '@/services/gameService';
import { QUERY_KEYS } from '@/models';

interface Props {
  gameId: string;
  onSaveSuccess?: () => void;
  onSaveError?: () => void;
}

export const useExpansionSelectorModal = ({ gameId, onSaveError, onSaveSuccess }: Props) => {
  const queryClient = useQueryClient();

  const [expansionQuery] = useQueries({
    queries: [getGameExpansions(gameId)],
  });

  const expansions = useMemo(() => expansionQuery.data ?? [], [expansionQuery.data]);

  const mutateExpasions = useMutation({
    mutationFn: saveGameExpansionCall,
    onSuccess: async () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, gameId] });
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
      onSaveSuccess?.();
    },
    onError: () => {
      onSaveError?.();
    },
  });

  return {
    expansions,
    isLoading: expansionQuery.isLoading,
    saveExpansions: mutateExpasions.mutateAsync,
    isPending: mutateExpasions.isPending,
  };
};
