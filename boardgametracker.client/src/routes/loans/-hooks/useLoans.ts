import { useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import type { Loan } from "@/models/Loan/Loan";
import { useToasts } from "@/routes/-hooks/useToasts";
import { deleteLoanCall, returnLoanCall, undoReturnLoanCall, updateLoanCall } from "@/services/loanService";
import { getLoans } from "@/services/queries/loans";
import { getSettings } from "@/services/queries/settings";
import { apiErrorMessage } from "@/utils/errorUtils";

export const useLoans = () => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const [loansQuery, settingsQuery] = useQueries({
		queries: [getLoans(), getSettings()],
	});

	const loans = loansQuery.data ?? [];
	const settings = settingsQuery.data;
	const isLoading = loansQuery.isLoading || settingsQuery.isLoading;

	const deleteLoan = async (loanId: number) => {
		try {
			const gameId = loans.find((loan) => loan.id === loanId)?.gameId;
			await deleteLoanCall(loanId);
			await invalidator.invalidateLoan(loanId, gameId);
			successToast("loans:delete.successfull");
		} catch (error) {
			errorToast(apiErrorMessage(error, "loans:delete.failed"));
		}
	};

	const invalidateLoans = (loanIds: number[]) =>
		Promise.all(
			loanIds.map((loanId) => invalidator.invalidateLoan(loanId, loans.find((loan) => loan.id === loanId)?.gameId)),
		);

	const undoReturns = async (loanIds: number[]) => {
		try {
			await Promise.all(loanIds.map((loanId) => undoReturnLoanCall(loanId)));
			successToast("loans:return.undone");
		} catch (error) {
			errorToast(apiErrorMessage(error, "loans:return.undo-failed"));
		} finally {
			await invalidateLoans(loanIds);
		}
	};

	const returnLoans = async (loanIds: number[], date: Date) => {
		try {
			await Promise.all(loanIds.map((loanId) => returnLoanCall(loanId, date)));
			await invalidateLoans(loanIds);
			successToast("loans:return.successfull", { label: "loans:return.undo", onClick: () => undoReturns(loanIds) });
		} catch (error) {
			errorToast(apiErrorMessage(error, "loans:return.failed"));
		}
	};

	const updateLoan = async (loan: Loan) => {
		try {
			await updateLoanCall(loan);
			await invalidator.invalidateLoan(loan.id, loan.gameId);
			successToast("loans:notifications.updated");
		} catch (error) {
			errorToast(apiErrorMessage(error, "loans:notifications.update-failed"));
		}
	};

	return {
		isLoading,
		loans,
		settings,
		deleteLoan,
		returnLoans,
		updateLoan,
	};
};
