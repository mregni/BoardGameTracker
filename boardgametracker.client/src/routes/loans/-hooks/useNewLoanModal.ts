import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { getPlayers } from '@/services/queries/players';
import { getGames } from '@/services/queries/games';
import { saveLoanCall } from '@/services/loanService';
import { Loan } from '@/models/Loan/Loan';
import { QUERY_KEYS } from '@/models';

interface Props {
  onSaveSuccess?: (loan: Loan) => void;
  onSaveError?: () => void;
}

export const useNewLoanModal = ({ onSaveSuccess, onSaveError }: Props) => {
  const queryClient = useQueryClient();

  const [gamesQuery, playerQuery] = useQueries({
    queries: [getGames(), getPlayers()],
  });

  const games = gamesQuery.data ?? [];
  const players = playerQuery.data ?? [];

  const saveLoanMutation = useMutation({
    mutationFn: saveLoanCall,
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.loans] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      onSaveSuccess?.(data);
    },
    onError: () => {
      onSaveError?.();
    },
  });

  const isLoading = gamesQuery.isLoading || playerQuery.isLoading || saveLoanMutation.isPending;

  return {
    isLoading,
    games,
    players,
    saveLoan: saveLoanMutation.mutateAsync,
  };
};
