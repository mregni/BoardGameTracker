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
		async onSuccess(data, search) {
			await invalidator.invalidateGameCreated();
			if (String(data.bggId) === search.bggId.trim()) {
				successToast("game:notifications.created");
			} else {
				successToast("game:notifications.expansion-created", undefined, { title: data.title });
			}
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
