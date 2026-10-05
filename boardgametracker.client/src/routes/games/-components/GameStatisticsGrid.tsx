import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { getSettings } from "@/services/queries/settings";
import { formatMinutesToDuration, toRelative } from "@/utils/dateUtils";
import { RoundDecimal } from "@/utils/numberUtils";
import { formatPrice } from "@/utils/priceUtils";

interface GameStats {
	totalPlayedTime: number | null;
	pricePerPlay: number | null;
	highScore: number | null;
	averageScore: number | null;
	averagePlayTime: number | null;
	lastPlayed: string | null;
}

interface Props {
	gameStats: GameStats;
	currency: string;
}

export const GameStatisticsGrid = (props: Props) => {
	const { gameStats, currency } = props;
	const { t } = useTranslation("statistics");
	const { data: settings } = useQuery(getSettings());

	const lastPlayedRelative =
		gameStats.lastPlayed && settings?.uiLanguage
			? toRelative(gameStats.lastPlayed, settings.uiLanguage, {
					addSuffix: false,
				})
			: null;

	const totalPlayedTime = formatMinutesToDuration(
		gameStats.totalPlayedTime,
		["weeks", "days", "hours", "minutes"],
		settings?.uiLanguage,
	);
	const averagePlayTime = formatMinutesToDuration(
		gameStats.averagePlayTime,
		["hours", "minutes", "seconds"],
		settings?.uiLanguage,
	);

	return (
		<div className="grid grid-cols-2 lg:grid-cols-3 gap-3 xl:gap-6">
			<BgtTextStatistic content={totalPlayedTime} title={t("total-play-time")} />
			<BgtTextStatistic
				content={
					gameStats.pricePerPlay != null ? formatPrice(gameStats.pricePerPlay, currency, settings?.uiLanguage) : null
				}
				title={t("price-per-play")}
			/>
			<BgtTextStatistic content={RoundDecimal(gameStats.highScore)} title={t("high-score")} />
			<BgtTextStatistic content={RoundDecimal(gameStats.averageScore)} title={t("average-score")} />
			<BgtTextStatistic content={averagePlayTime} title={t("average-playtime")} />
			<BgtTextStatistic content={lastPlayedRelative} title={t("last-played")} />
		</div>
	);
};
