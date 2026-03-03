import { useMutation, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { getGame } from "@/services/queries/games";
import { getSession } from "@/services/queries/sessions";
import { updateSessionCall } from "@/services/sessionService";

interface Props {
	sessionId: number;
	onSuccess?: () => void;
}

export const useUpdateSessionData = ({ sessionId, onSuccess }: Props) => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const { data: session } = useSuspenseQuery(getSession(sessionId));
	const { data: game } = useSuspenseQuery(getGame(session.gameId));

	const updateSessionMutation = useMutation({
		mutationFn: updateSessionCall,
		async onSuccess(sessionResult) {
			successToast("player-session.update.notifications.updated");
			onSuccess?.();
			for (const x of sessionResult.playerSessions) {
				queryClient.invalidateQueries({
					queryKey: [QUERY_KEYS.player, x.playerId, QUERY_KEYS.sessions],
				});
			}

			queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.game, sessionResult.gameId],
			});
			queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.sessions, sessionId],
			});
		},
		onError: () => {
			errorToast("player-session.update.notifications.update-failed");
		},
	});

	return {
		session,
		game,
		isPending: updateSessionMutation.isPending,
		updateSession: updateSessionMutation.mutateAsync,
	};
};
