import { useQueries, useQueryClient } from "@tanstack/react-query";
import { isApiError, QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { deleteLoanCall, returnLoanCall } from "@/services/loanService";
import { getLoans } from "@/services/queries/loans";
import { getSettings } from "@/services/queries/settings";

export const useLoans = () => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

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
			successToast("loan.delete.successfull");
		} catch {
			errorToast("loan.delete.failed");
		}
	};

	const returnLoan = async (loanId: number, date: Date) => {
		try {
			await returnLoanCall(loanId, date);
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.loans] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
			successToast("loan.return.successfull");
		} catch (e: unknown) {
			if (isApiError(e) && e.message.includes("Return date cannot be before loan date.")) {
				errorToast("loan.return.date-failed");
			} else {
				errorToast("loan.return.failed");
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
