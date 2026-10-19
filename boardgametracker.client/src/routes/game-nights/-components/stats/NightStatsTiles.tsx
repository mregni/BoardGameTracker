import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { GameNightStatistics } from "@/models";

const DAYS_PER_WEEK = 7;
const MIN_NIGHTS_FOR_RHYTHM = 4;

interface Props {
	statistics: GameNightStatistics;
}

export const NightStatsTiles = ({ statistics }: Props) => {
	const { t } = useTranslation("game-nights");
	const format = useNumberFormat();
	const shortDuration = useShortDuration();
	const { nights, gamesPlayed, totalMinutes, averageTable, averageDaysBetweenNights } = statistics;

	const frequency = () => {
		if (averageDaysBetweenNights === null || nights < MIN_NIGHTS_FOR_RHYTHM) return undefined;
		if (averageDaysBetweenNights >= DAYS_PER_WEEK) {
			return t("stats.tiles.every-week", { count: Math.round(averageDaysBetweenNights / DAYS_PER_WEEK) });
		}
		return t("stats.tiles.every-day", { count: Math.max(1, Math.round(averageDaysBetweenNights)) });
	};

	const perNight = (value: number) => (nights === 0 ? 0 : value / nights);

	return (
		<div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
			<BgtTextStatistic title={t("stats.tiles.nights")} content={nights} caption={frequency()} textSize="6" />
			<BgtTextStatistic
				title={t("stats.tiles.games")}
				content={gamesPlayed}
				caption={t("stats.tiles.per-night", { value: format.decimal(perNight(gamesPlayed), 1) })}
				textSize="6"
			/>
			<BgtTextStatistic
				title={t("stats.tiles.play-time")}
				content={shortDuration(totalMinutes)}
				caption={t("stats.tiles.per-night", {
					value: shortDuration(perNight(totalMinutes)),
				})}
				textSize="6"
			/>
			<BgtTextStatistic
				title={t("stats.tiles.table")}
				content={format.decimal(averageTable, 1)}
				caption={t("stats.tiles.table-hint")}
				textSize="6"
			/>
		</div>
	);
};
