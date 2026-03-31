import { useQueries, useQueryClient } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { getGame, getGameSessions } from "@/services/queries/games";
import { getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { deleteSessionCall } from "@/services/sessionService";

interface UseGameSessionsDataProps {
	gameId: number;
	onDeleteSuccess?: () => void;
}

export const useGameSessionsData = ({ gameId, onDeleteSuccess }: UseGameSessionsDataProps) => {
	const queryClient = useQueryClient();
	const { infoToast, errorToast } = useToasts();

	const [gameQuery, settingsQuery, sessionsQuery, playersQuery] = useQueries({
		queries: [getGame(gameId), getSettings(), getGameSessions(gameId), getPlayers()],
	});

	const game = gameQuery.data;
	const sessions = sessionsQuery.data ?? [];
	const settings = settingsQuery.data;
	const players = playersQuery.data ?? [];
	const isLoading = gameQuery.isLoading || settingsQuery.isLoading;

	const deleteSession = async (id: number) => {
		try {
			await deleteSessionCall(id);
			await queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.sessions],
			});
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.shames] });
			infoToast("sessions:notifications.deleted");
			onDeleteSuccess?.();
		} catch {
			errorToast("sessions:notifications.delete-failed");
		}
	};

	return {
		isLoading,
		game,
		settings,
		sessions,
		players,
		deleteSession,
	};
};
