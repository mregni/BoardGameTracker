import { useMutation, useQueries, useQueryClient } from "@tanstack/react-query";
import { type Game, QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { saveGameCall } from "@/services/gameService";
import { getSettings } from "@/services/queries/settings";

interface Props {
	gameId?: string;
	onSuccess?: (game: Game) => void;
}
export const useNewGame = ({ onSuccess }: Props) => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const [settingsQuery] = useQueries({
		queries: [getSettings()],
	});

	const settings = settingsQuery.data;

	const saveGameMutation = useMutation({
		mutationFn: saveGameCall,
		onSuccess: async (data) => {
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.dashboard] });
			successToast("game.notifications.created");
			onSuccess?.(data);
		},
		onError: () => {
			errorToast("game.notifications.create-failed");
		},
	});

	const isLoading = settingsQuery.isLoading || saveGameMutation.isPending;

	return {
		isLoading,
		settings,
		saveGame: saveGameMutation.mutateAsync,
	};
};
