import { useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
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
	const invalidator = useQueryInvalidator();
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
			await invalidator.invalidateSessionDeleted(gameId);
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
