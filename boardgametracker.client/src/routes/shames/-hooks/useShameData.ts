import { useQueries } from "@tanstack/react-query";
import { getShameStatistics, getShames } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";

export const useShameData = () => {
	const [shameQuery, shameStatisticsQuery, settingsQuery] = useQueries({
		queries: [getShames(), getShameStatistics(), getSettings()],
	});

	const shames = shameQuery.data ?? [];
	const statistics = shameStatisticsQuery.data;
	const settings = settingsQuery.data;

	return {
		shames,
		statistics,
		settings,
		isLoading: shameQuery.isLoading || shameStatisticsQuery.isLoading || settingsQuery.isLoading,
	};
};
