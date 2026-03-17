import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import type { Settings } from "@/models";
import { formatMinutesToDuration } from "@/utils/dateUtils";
import { GetPercentage } from "@/utils/numberUtils";

interface PlayerStatistics {
	playCount: number;
	totalPlayedTime: number | null;
	winCount: number;
	distinctGameCount: number;
}

interface Props {
	statistics: PlayerStatistics;
	settings?: Settings;
}

export const PlayerStatisticsGrid = (props: Props) => {
	const { statistics, settings } = props;
	const { t } = useTranslation("statistics");

	const totalPlayedTime = formatMinutesToDuration(
		statistics.totalPlayedTime,
		["months", "weeks", "days", "hours", "minutes"],
		settings?.uiLanguage,
	);

	return (
		<div className="grid grid-cols-2 lg:grid-cols-4 xl:grid-cols-5 gap-3 xl:gap-6">
			<BgtTextStatistic content={statistics.playCount} title={t("play-count")} />
			<BgtTextStatistic content={totalPlayedTime} title={t("total-play-time")} />
			<BgtTextStatistic content={statistics.winCount} title={t("win-count")} />
			<BgtTextStatistic
				content={GetPercentage(statistics.winCount, statistics.playCount)}
				title={t("win-percentage")}
				suffix={"%"}
			/>
			<BgtTextStatistic content={statistics.distinctGameCount} title={t("distinct-game-count")} />
		</div>
	);
};
