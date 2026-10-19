import { useMutation, useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { saveLoanCall } from "@/services/loanService";
import { getGames } from "@/services/queries/games";
import { getLoans } from "@/services/queries/loans";
import { getPlayers } from "@/services/queries/players";
import { apiErrorMessage } from "@/utils/errorUtils";

interface Props {
	onSuccess?: () => void;
}

export const useNewLoanModal = ({ onSuccess }: Props) => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const [gamesQuery, playerQuery, loansQuery] = useQueries({
		queries: [getGames(), getPlayers(), getLoans()],
	});

	const games = gamesQuery.data ?? [];
	const players = playerQuery.data ?? [];
	const activeLoans = (loansQuery.data ?? []).filter((loan) => loan.isActive);

	const saveLoanMutation = useMutation({
		mutationFn: saveLoanCall,
		onSuccess: async (loan) => {
			await invalidator.invalidateLoan(loan.id, loan.gameId);
			successToast("loans:notifications.created");
			onSuccess?.();
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "loans:notifications.create-failed"));
		},
	});

	const isLoading = gamesQuery.isLoading || playerQuery.isLoading || saveLoanMutation.isPending;

	return {
		isLoading,
		games,
		players,
		activeLoans,
		saveLoan: saveLoanMutation.mutateAsync,
	};
};
