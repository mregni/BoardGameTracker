import { useMutation, useQueries, useQueryClient } from "@tanstack/react-query";
import { type Game, QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { addGameWithBggCall } from "@/services/gameService";
import { getSettings } from "@/services/queries/settings";

interface Props {
	onSuccess?: (game: Game) => void;
}

export const useBggGameModal = ({ onSuccess }: Props) => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const [settingsQuery] = useQueries({
		queries: [getSettings()],
	});

	const settings = settingsQuery.data;

	const addGameMutation = useMutation({
		mutationFn: addGameWithBggCall,
		async onSuccess(data) {
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });

			successToast("game.notifications.created");
			onSuccess?.(data);
		},
		onError: () => {
			errorToast("game.notifications.create-failed");
		},
	});

	return {
		save: addGameMutation.mutateAsync,
		isPending: addGameMutation.isPending,
		settings,
	};
};
