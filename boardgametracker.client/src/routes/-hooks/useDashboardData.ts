import { useQueries } from "@tanstack/react-query";
import { getDashboardStatistics } from "@/services/queries/dashboard";
import { getSettings } from "@/services/queries/settings";

export const useDashboardData = () => {
	const [statisticsQuery, settingsQuery] = useQueries({
		queries: [getDashboardStatistics(), getSettings()],
	});

	const statistics = statisticsQuery.data;
	const settings = settingsQuery.data;

	const isLoading = statisticsQuery.isLoading || settingsQuery.isLoading;

	return {
		isLoading,
		statistics,
		settings,
	};
};
