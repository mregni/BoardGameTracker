import { useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { getGames } from "@/services/queries/games";
import { getPlayer, getPlayerSessions, getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { deleteSessionCall } from "@/services/sessionService";
import { apiErrorMessage } from "@/utils/errorUtils";

interface UsePlayerSessionDataProps {
	playerId: number;
	onDeleteSuccess?: () => void;
}

export const usePlayerSessionData = ({ playerId, onDeleteSuccess }: UsePlayerSessionDataProps) => {
	const invalidator = useQueryInvalidator();
	const { infoToast, errorToast } = useToasts();

	const [settingsQuery, playerQuery, gamesQuery, sessionsQuery, playersQuery] = useQueries({
		queries: [getSettings(), getPlayer(playerId), getGames(), getPlayerSessions(playerId), getPlayers()],
	});

	const player = playerQuery.data;
	const settings = settingsQuery.data;
	const games = gamesQuery.data ?? [];
	const sessions = sessionsQuery.data ?? [];
	const players = playersQuery.data ?? [];

	const isLoading = settingsQuery.isLoading || playerQuery.isLoading || gamesQuery.isLoading;

	const deleteSession = async (id: number) => {
		const deleted = sessions.find((session) => session.id === id);
		try {
			await deleteSessionCall(id);
			await invalidator.invalidateSessionDeleted(
				deleted?.gameId,
				deleted?.playerSessions.map((x) => x.playerId) ?? [playerId],
			);
			infoToast("sessions:notifications.deleted");
			onDeleteSuccess?.();
		} catch (error) {
			errorToast(apiErrorMessage(error, "sessions:notifications.delete-failed"));
		}
	};

	return {
		player,
		sessions,
		games,
		settings,
		players,
		isLoading,
		deleteSession,
	};
};
