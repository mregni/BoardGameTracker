import { useCallback } from "react";

interface UseGameActionsProps {
	deleteLoan: (loanId: number) => Promise<void>;
	returnLoan: (loanId: number, returnDate: Date) => Promise<void>;
	onDeleteModalClose: () => void;
}

export const useLoanActions = (props: UseGameActionsProps) => {
	const { deleteLoan, onDeleteModalClose, returnLoan } = props;

	const handleDelete = useCallback(
		async (loanId: number) => {
			await deleteLoan(loanId);
			onDeleteModalClose();
		},
		[deleteLoan, onDeleteModalClose],
	);

	const handleReturnLoan = useCallback(
		async (loanId: number, returnDate: Date) => {
			await returnLoan(loanId, returnDate);
		},
		[returnLoan],
	);

	return {
		handleDelete,
		handleReturnLoan,
	};
};
