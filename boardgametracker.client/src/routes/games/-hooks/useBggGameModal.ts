import { useMutation, useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import type { Game } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { addGameWithBggCall } from "@/services/gameService";
import { getSettings } from "@/services/queries/settings";
import { apiErrorMessage } from "@/utils/errorUtils";

interface Props {
	onSuccess?: (game: Game) => void;
}

export const useBggGameModal = ({ onSuccess }: Props) => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const [settingsQuery] = useQueries({
		queries: [getSettings()],
	});

	const settings = settingsQuery.data;

	const addGameMutation = useMutation({
		mutationFn: addGameWithBggCall,
		async onSuccess(data) {
			await invalidator.invalidateGameCreated();
			successToast("game:notifications.created");
			onSuccess?.(data);
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "game:notifications.create-failed"));
		},
	});

	return {
		save: addGameMutation.mutateAsync,
		isPending: addGameMutation.isPending,
		settings,
	};
};
