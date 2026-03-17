import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { getSettings } from "@/services/queries/settings";
import { formatMinutesToDuration, toRelative } from "@/utils/dateUtils";
import { RoundDecimal } from "@/utils/numberUtils";

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
	expansionCount: number;
	currency: string;
	uiLanguage: string;
	dateFormat: string;
}

export const GameStatisticsGrid = (props: Props) => {
	const { gameStats, expansionCount, currency } = props;
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
		<div className="grid grid-cols-2 lg:grid-cols-4 gap-3 xl:gap-6">
			<BgtTextStatistic content={totalPlayedTime} title={t("total-play-time")} />
			<BgtTextStatistic content={gameStats.pricePerPlay} title={t("price-per-play")} prefix={currency} />
			<BgtTextStatistic content={RoundDecimal(gameStats.highScore)} title={t("high-score")} />
			<BgtTextStatistic content={RoundDecimal(gameStats.averageScore)} title={t("average-score")} />
			<BgtTextStatistic content={averagePlayTime} title={t("average-playtime")} />
			<BgtTextStatistic content={lastPlayedRelative} title={t("last-played")} />
			<BgtTextStatistic content={expansionCount} title={t("expansion-count")} />
		</div>
	);
};

//locale: getDateFnsLocale(settings?.uiLanguage ?? ''),
