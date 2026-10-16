import { useMutation, useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import type { Game } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { saveGameCall } from "@/services/gameService";
import { getSettings } from "@/services/queries/settings";
import { apiErrorMessage } from "@/utils/errorUtils";

interface Props {
	gameId?: string;
	onSuccess?: (game: Game) => void;
}
export const useNewGame = ({ onSuccess }: Props) => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const [settingsQuery] = useQueries({
		queries: [getSettings()],
	});

	const settings = settingsQuery.data;

	const saveGameMutation = useMutation({
		mutationFn: saveGameCall,
		onSuccess: async (data) => {
			await invalidator.invalidateGame(data.id);
			successToast("game:notifications.created");
			onSuccess?.(data);
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "game:notifications.create-failed"));
		},
	});

	const isLoading = settingsQuery.isLoading || saveGameMutation.isPending;

	return {
		isLoading,
		settings,
		saveGame: saveGameMutation.mutateAsync,
	};
};
