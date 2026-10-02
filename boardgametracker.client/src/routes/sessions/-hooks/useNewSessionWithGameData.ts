import { useMutation, useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { getGame } from "@/services/queries/games";
import { addSessionCall } from "@/services/sessionService";

interface Props {
	gameId: number;
	onSuccess?: () => void;
}

export const useNewSessionWithGameData = ({ gameId, onSuccess }: Props) => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const [gameQuery] = useQueries({
		queries: [getGame(gameId)],
	});

	const game = gameQuery.data;

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
		onError: () => {
			errorToast("player-session:new.notifications.create-failed");
		},
	});

	return {
		game,
		isLoading: gameQuery.isLoading,
		isPending: saveSessionMutation.isPending,
		saveSession: saveSessionMutation.mutateAsync,
	};
};
