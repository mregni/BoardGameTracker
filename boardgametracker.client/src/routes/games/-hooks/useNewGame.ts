import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { saveGameCall } from '@/services/gameService';
import { Game, QUERY_KEYS } from '@/models';

interface Props {
  gameId?: string;
  onSaveSuccess?: (game: Game) => void;
  onSaveError?: () => void;
}
export const useNewGame = ({ onSaveSuccess, onSaveError }: Props) => {
  const queryClient = useQueryClient();

  const [settingsQuery] = useQueries({
    queries: [getSettings()],
  });

  const settings = settingsQuery.data;

  const saveGameMutation = useMutation({
    mutationFn: saveGameCall,
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.dashboard] });
      onSaveSuccess?.(data);
    },
    onError: () => {
      onSaveError?.();
    },
  });

  const isLoading = settingsQuery.isLoading || saveGameMutation.isPending;

  return {
    isLoading,
    settings,
    saveGame: saveGameMutation.mutateAsync,
  };
};
