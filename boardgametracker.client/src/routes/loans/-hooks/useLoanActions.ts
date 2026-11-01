import { useCallback } from "react";

interface UseLoanActionsProps {
	deleteLoan: (loanId: number) => Promise<void>;
	returnLoans: (loanIds: number[], returnDate: Date) => Promise<void>;
	onDeleteModalClose: () => void;
}

export const useLoanActions = (props: UseLoanActionsProps) => {
	const { deleteLoan, onDeleteModalClose, returnLoans } = props;

	const handleDelete = useCallback(
		async (loanId: number) => {
			await deleteLoan(loanId);
			onDeleteModalClose();
		},
		[deleteLoan, onDeleteModalClose],
	);

	const handleReturnLoan = useCallback(
		async (loanIds: number[], returnDate: Date) => {
			await returnLoans(loanIds, returnDate);
		},
		[returnLoans],
	);

	return {
		handleDelete,
		handleReturnLoan,
	};
};
