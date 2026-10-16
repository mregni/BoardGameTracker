import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { GameNightStatistics } from "@/models";
import { formatMinutesToDuration } from "@/utils/dateUtils";

const DAYS_PER_WEEK = 7;

interface Props {
	statistics: GameNightStatistics;
	uiLanguage: string;
}

export const NightStatsTiles = ({ statistics, uiLanguage }: Props) => {
	const { t } = useTranslation("game-nights");
	const format = useNumberFormat();
	const { nights, gamesPlayed, totalMinutes, averageTable, averageDaysBetweenNights } = statistics;

	const frequency = () => {
		if (averageDaysBetweenNights === null) return null;
		if (averageDaysBetweenNights >= DAYS_PER_WEEK) {
			return t("stats.tiles.every-week", { count: Math.round(averageDaysBetweenNights / DAYS_PER_WEEK) });
		}
		return t("stats.tiles.every-day", { count: Math.max(1, Math.round(averageDaysBetweenNights)) });
	};

	const perNight = (value: number) => (nights === 0 ? 0 : value / nights);

	return (
		<div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
			<BgtTextStatistic title={t("stats.tiles.nights")} content={nights} hint={frequency()} textSize="6" />
			<BgtTextStatistic
				title={t("stats.tiles.games")}
				content={gamesPlayed}
				hint={t("stats.tiles.per-night", { value: format.decimal(perNight(gamesPlayed), 1) })}
				textSize="6"
			/>
			<BgtTextStatistic
				title={t("stats.tiles.play-time")}
				content={formatMinutesToDuration(totalMinutes, ["hours", "minutes"], uiLanguage) ?? format.integer(0)}
				hint={t("stats.tiles.per-night", {
					value: formatMinutesToDuration(perNight(totalMinutes), ["hours", "minutes"], uiLanguage) ?? format.integer(0),
				})}
				textSize="6"
			/>
			<BgtTextStatistic
				title={t("stats.tiles.table")}
				content={format.decimal(averageTable, 1)}
				hint={t("stats.tiles.table-hint")}
				textSize="6"
			/>
		</div>
	);
};
