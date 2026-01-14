import { useQueries, useQueryClient } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getLoans } from '@/services/queries/loans';
import { deleteLoanCall, returnLoanCall } from '@/services/loanService';
import { QUERY_KEYS } from '@/models';

interface Props {
  onDeleteError?: () => void;
  onDeleteSuccess?: () => void;
  onReturnError?: () => void;
  onReturnSuccess?: () => void;
}

export const useLoans = (props: Props) => {
  const { onDeleteError, onDeleteSuccess, onReturnError, onReturnSuccess } = props;
  const queryClient = useQueryClient();
  const [loansQuery, settingsQuery] = useQueries({
    queries: [getLoans(), getSettings()],
  });

  const loans = loansQuery.data ?? [];
  const settings = settingsQuery.data;
  const isLoading = loansQuery.isLoading || settingsQuery.isLoading;

  const deleteLoan = async (loanId: number) => {
    try {
      await deleteLoanCall(loanId);
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.loans] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
      onDeleteSuccess?.();
    } catch {
      onDeleteError?.();
    }
  };

  const returnLoan = async (loanId: number, date: Date) => {
    try {
      await returnLoanCall(loanId, date);
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.loans] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
      onReturnSuccess?.();
    } catch {
      onReturnError?.();
    }
  };

  return {
    isLoading,
    loans,
    settings,
    deleteLoan,
    returnLoan,
  };
};
