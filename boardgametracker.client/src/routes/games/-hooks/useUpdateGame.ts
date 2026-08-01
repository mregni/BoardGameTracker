import { useMutation, useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { updateGameCall } from "@/services/gameService";
import { getGame } from "@/services/queries/games";

interface Props {
	gameId: number;
	onSuccess?: () => void;
}
export const useUpdateGame = ({ gameId, onSuccess }: Props) => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const [gameQuery] = useQueries({
		queries: [getGame(gameId)],
	});

	const game = gameQuery.data;

	const saveGameMutation = useMutation({
		mutationFn: updateGameCall,
		onSuccess: async () => {
			await Promise.all([invalidator.invalidateGame(gameId), invalidator.invalidateShames()]);
			successToast("game:notifications.updated");
			onSuccess?.();
		},
		onError: () => {
			errorToast("game:notifications.update-failed");
		},
	});

	const isLoading = gameQuery.isLoading || saveGameMutation.isPending;

	return {
		isLoading,
		game,
		updateGame: saveGameMutation.mutateAsync,
	};
};
