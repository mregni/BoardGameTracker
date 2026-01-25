import { useQueries, useQueryClient } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getLoans } from '@/services/queries/loans';
import { deleteLoanCall, returnLoanCall } from '@/services/loanService';
import { QUERY_KEYS } from '@/models';

interface Props {
  onDeleteError?: () => void;
  onDeleteSuccess?: () => void;
  onReturnError?: (text?: string) => void;
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
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
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
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      onReturnSuccess?.();
    } catch (e: unknown) {
      const error = e as { response?: { data?: string } };
      if (error.response?.data?.includes('Return date cannot be before loan date.')) {
        onReturnError?.('loan.return.date-failed');
      } else {
        onReturnError?.();
      }
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
