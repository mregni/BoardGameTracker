import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { MostPlayedGame, Settings } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { formatInteger, formatRatio } from "@/utils/numberUtils";
import type { LastPlayed } from "../-utils/lastPlayed";

interface PlayerStatistics {
	playCount: number;
	totalPlayedTime: number | null;
	winCount: number;
	distinctGameCount: number;
	coopWinCount: number;
	coopLossCount: number;
	mostPlayedGames: MostPlayedGame[];
}

interface Props {
	statistics: PlayerStatistics;
	settings?: Settings;
	lastPlayed?: LastPlayed | null;
}

export const PlayerStatisticsGrid = (props: Props) => {
	const { statistics, settings, lastPlayed } = props;
	const { t } = useTranslation(["statistics", "common"]);
	const shortDuration = useShortDuration();

	const favorite = statistics.mostPlayedGames[0];
	const hasCoopPlays = statistics.coopWinCount + statistics.coopLossCount > 0;

	return (
		<div className="grid grid-cols-2 md:grid-cols-3 2xl:grid-cols-5 gap-3 xl:gap-6">
			{favorite && (
				<BgtTextStatistic
					content={favorite.title}
					caption={t("common:sessions", { count: favorite.totalSessions })}
					title={t("favorite-game")}
					textSize="4"
				/>
			)}
			{lastPlayed && settings && (
				<BgtTextStatistic
					content={toDisplay(lastPlayed.start, settings.dateFormat, settings.uiLanguage)}
					caption={lastPlayed.gameTitle}
					title={t("last-played")}
					textSize="4"
				/>
			)}
			<BgtTextStatistic content={shortDuration(statistics.totalPlayedTime)} title={t("total-play-time")} textSize="4" />
			<BgtTextStatistic
				content={formatRatio(statistics.winCount, statistics.playCount, settings?.uiLanguage)}
				title={t("win-percentage")}
				textSize="4"
			/>
			<BgtTextStatistic content={statistics.distinctGameCount} title={t("distinct-game-count")} textSize="4" />
			{hasCoopPlays && (
				<BgtTextStatistic
					content={t("coop-results", {
						won: formatInteger(statistics.coopWinCount, settings?.uiLanguage),
						lost: formatInteger(statistics.coopLossCount, settings?.uiLanguage),
					})}
					title={t("coop-won-lost")}
					textSize="4"
				/>
			)}
		</div>
	);
};
