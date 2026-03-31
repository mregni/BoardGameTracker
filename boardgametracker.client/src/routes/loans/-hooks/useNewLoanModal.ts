import { useMutation, useQueries, useQueryClient } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { saveLoanCall } from "@/services/loanService";
import { getGames } from "@/services/queries/games";
import { getPlayers } from "@/services/queries/players";

interface Props {
	onSuccess?: () => void;
}

export const useNewLoanModal = ({ onSuccess }: Props) => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const [gamesQuery, playerQuery] = useQueries({
		queries: [getGames(), getPlayers()],
	});

	const games = gamesQuery.data ?? [];
	const players = playerQuery.data ?? [];

	const saveLoanMutation = useMutation({
		mutationFn: saveLoanCall,
		onSuccess: async () => {
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.loans] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
			successToast("loans:notifications.created");
			onSuccess?.();
		},
		onError: () => {
			errorToast("loans:notifications.create-failed");
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
