import { useQueries, useQueryClient } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { deletePlayerCall } from "@/services/playerService";
import { getBadges } from "@/services/queries/basdges";
import { getPlayer, getPlayerSessionsShortList, getPlayerStatistics } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { apiErrorMessage } from "@/utils/errorUtils";

interface UsePLayerDataProps {
	playerId: number;
	onDeleteSuccess?: () => void;
}

export const usePlayerData = ({ playerId, onDeleteSuccess }: UsePLayerDataProps) => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const [playerQuery, statisticsQuery, badgesQuery, sessionsQuery, settingsQuery] = useQueries({
		queries: [
			getPlayer(playerId),
			getPlayerStatistics(playerId),
			getBadges(),
			getPlayerSessionsShortList(playerId, 5),
			getSettings(),
		],
	});

	const player = playerQuery.data;
	const settings = settingsQuery.data;
	const statistics = statisticsQuery.data;
	const badges = badgesQuery.data;
	const sessions = sessionsQuery.data ?? [];
	const isLoading =
		playerQuery.isLoading ||
		statisticsQuery.isLoading ||
		badgesQuery.isLoading ||
		sessionsQuery.isLoading ||
		settingsQuery.isLoading;

	const deletePlayer = async (id: number, onDeleted?: () => Promise<unknown>) => {
		try {
			await deletePlayerCall(id);
		} catch (error) {
			errorToast(apiErrorMessage(error, "player:delete.failed"));
			return;
		}

		successToast("player:delete.successfull");
		await onDeleted?.();
		onDeleteSuccess?.();
		queryClient.removeQueries({ queryKey: [QUERY_KEYS.player, id] });
		await queryClient.invalidateQueries();
	};

	return {
		player,
		statistics,
		badges,
		settings,
		sessions,
		isLoading,
		deletePlayer,
	};
};
