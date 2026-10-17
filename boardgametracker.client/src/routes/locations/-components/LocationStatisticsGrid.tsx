import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { LocationStatistics } from "@/models";

interface Props {
	playCount: number;
	statistics: LocationStatistics;
}

export const LocationStatisticsGrid = (props: Props) => {
	const { playCount, statistics } = props;
	const { t } = useTranslation("location");
	const shortDuration = useShortDuration();

	const playTime = shortDuration(statistics.totalPlayedTime);

	return (
		<div className="grid grid-cols-2 md:grid-cols-4 gap-3 xl:gap-6">
			<BgtTextStatistic content={playCount} title={t("detail.sessions")} textSize="4" />
			<BgtTextStatistic content={statistics.distinctGameCount} title={t("detail.games")} textSize="4" />
			<BgtTextStatistic content={statistics.distinctPlayerCount} title={t("detail.players")} textSize="4" />
			<BgtTextStatistic content={playTime} title={t("detail.play-time")} textSize="4" />
		</div>
	);
};
