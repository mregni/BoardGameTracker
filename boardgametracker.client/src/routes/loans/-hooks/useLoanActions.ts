interface UseGameActionsProps {
  deleteLoan: (loanId: number) => Promise<void>;
  returnLoan: (loanId: number, returnDate: Date) => Promise<void>;
  onDeleteModalClose: () => void;
}

export const useLoanActions = (props: UseGameActionsProps) => {
  const { deleteLoan, onDeleteModalClose, returnLoan } = props;

  const handleDelete = async (loanId: number) => {
    await deleteLoan(loanId);
    onDeleteModalClose();
  };

  const handleReturnLoan = async (loanId: number, returnDate: Date) => {
    await returnLoan(loanId, returnDate);
  };

  return {
    handleDelete,
    handleReturnLoan,
  };
};
