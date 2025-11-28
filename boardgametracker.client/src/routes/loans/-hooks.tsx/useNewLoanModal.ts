import { useMemo } from 'react';
import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { getGames } from '@/services/queries/games';
import { getPlayers } from '@/services/queries/players';
import { QUERY_KEYS } from '@/models';
import { saveLoanCall } from '@/services/loanService';
import { Loan } from '@/models/Loan/Loan';

interface Props {
  onSaveSuccess?: (loan: Loan) => void;
  onSaveError?: () => void;
}

export const useNewLoanModal = ({ onSaveSuccess, onSaveError }: Props) => {
  const queryClient = useQueryClient();

  const [gamesQuery, playerQuery] = useQueries({
    queries: [getGames(), getPlayers()],
  });

  const games = useMemo(() => gamesQuery.data ?? [], [gamesQuery.data]);
  const players = useMemo(() => playerQuery.data ?? [], [playerQuery.data]);

  const saveLoanMutation = useMutation({
    mutationFn: saveLoanCall,
    onSuccess: async (data) => {
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.loans] });
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
