import { useMutation, useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { getGames } from "@/services/queries/games";
import { addSessionCall } from "@/services/sessionService";
import { apiErrorMessage } from "@/utils/errorUtils";

interface Props {
	onSuccess?: () => void;
}

export const useNewSessionData = ({ onSuccess }: Props = {}) => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const [gamesQuery] = useQueries({
		queries: [getGames()],
	});

	const games = gamesQuery.data ?? [];
	const isLoading = gamesQuery.isLoading;

	const saveSessionMutation = useMutation({
		mutationFn: addSessionCall,
		async onSuccess(sessionResult) {
			successToast("player-session:new.notifications.created");
			onSuccess?.();

			await invalidator.invalidateSession(
				sessionResult.id,
				sessionResult.gameId,
				sessionResult.playerSessions.map((x) => x.playerId),
			);
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "player-session:new.notifications.create-failed"));
		},
	});

	return {
		isLoading,
		isPending: saveSessionMutation.isPending,
		saveSession: saveSessionMutation.mutateAsync,
		games,
	};
};
