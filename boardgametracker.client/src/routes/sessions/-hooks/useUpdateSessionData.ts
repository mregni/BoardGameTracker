import { useMutation, useQuery } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { getGame } from "@/services/queries/games";
import { getSession } from "@/services/queries/sessions";
import { updateSessionCall } from "@/services/sessionService";
import { apiErrorMessage } from "@/utils/errorUtils";

interface Props {
	sessionId: number;
	onSuccess?: () => void;
}

export const useUpdateSessionData = ({ sessionId, onSuccess }: Props) => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const sessionQuery = useQuery(getSession(sessionId));
	const session = sessionQuery.data;
	const gameQuery = useQuery({ ...getGame(session?.gameId ?? 0), enabled: session !== undefined });
	const game = gameQuery.data;

	const updateSessionMutation = useMutation({
		mutationFn: updateSessionCall,
		async onSuccess(sessionResult) {
			successToast("player-session:update.notifications.updated");
			onSuccess?.();
			await invalidator.invalidateSession(
				sessionId,
				sessionResult.gameId,
				sessionResult.playerSessions.map((x) => x.playerId),
			);
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "player-session:update.notifications.update-failed"));
		},
	});

	return {
		session,
		game,
		isLoading: sessionQuery.isLoading || gameQuery.isLoading,
		isPending: updateSessionMutation.isPending,
		updateSession: updateSessionMutation.mutateAsync,
	};
};
