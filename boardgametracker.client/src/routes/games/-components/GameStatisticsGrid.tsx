import { useQuery } from "@tanstack/react-query";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { useShortDuration } from "@/hooks/useShortDuration";
import { getSettings } from "@/services/queries/settings";
import { toRelative } from "@/utils/dateUtils";
import {
	formatDecimal,
	formatPrice,
	formatRatio,
	MIN_SESSIONS_FOR_AVERAGE,
	type NumberValue,
} from "@/utils/numberUtils";

interface GameStats {
	playCount: number;
	totalPlayedTime: number | null;
	pricePerPlay: number | null;
	highScore: number | null;
	averageScore: number | null;
	scoredSessionCount: number;
	averagePlayTime: number | null;
	lastPlayed: string | null;
	coopWinCount: number;
	coopLossCount: number;
}

interface Props {
	gameStats: GameStats;
	currency: string;
	hasScoringResults?: boolean;
}

export const MIN_COOP_SESSIONS_FOR_RATE = MIN_SESSIONS_FOR_AVERAGE;

export const statisticsGridColumns = (count: number) => {
	if (count <= 2) return "md:grid-cols-2";
	if (count === 3) return "md:grid-cols-3";
	if (count === 4) return "md:grid-cols-2 xl:grid-cols-4";
	if (count === 5) return "md:grid-cols-3 xl:grid-cols-5";
	if (count === 6) return "md:grid-cols-3 2xl:grid-cols-6";
	return "md:grid-cols-4";
};

export const GameStatisticsGrid = (props: Props) => {
	const { gameStats, currency, hasScoringResults = false } = props;
	const { t } = useTranslation("statistics");
	const { data: settings } = useQuery(getSettings());
	const shortDuration = useShortDuration();

	const lastPlayedRelative =
		gameStats.lastPlayed && settings?.uiLanguage ? toRelative(gameStats.lastPlayed, settings.uiLanguage) : null;

	const decimalOrNull = (value: NumberValue) => (value == null ? null : formatDecimal(value, settings?.uiLanguage));

	const totalPlayedTime = shortDuration(gameStats.totalPlayedTime);

	const coopPlayCount = gameStats.coopWinCount + gameStats.coopLossCount;
	const showCoopRate = coopPlayCount >= MIN_COOP_SESSIONS_FOR_RATE;
	const showScores = !hasScoringResults && gameStats.scoredSessionCount >= MIN_SESSIONS_FOR_AVERAGE;

	const tileCount =
		2 +
		[
			gameStats.pricePerPlay != null,
			showScores && gameStats.highScore != null,
			showScores && gameStats.averageScore != null,
			lastPlayedRelative !== null,
			showCoopRate,
		].filter(Boolean).length;

	return (
		<div
			className={cx("grid grid-cols-2 gap-3 xl:gap-6", statisticsGridColumns(tileCount))}
			data-testid="game-statistics-grid"
		>
			<BgtTextStatistic content={gameStats.playCount} title={t("play-count")} textSize="4" />
			<BgtTextStatistic content={totalPlayedTime} title={t("total-play-time")} textSize="4" />
			<BgtTextStatistic
				content={
					gameStats.pricePerPlay != null ? formatPrice(gameStats.pricePerPlay, currency, settings?.uiLanguage) : null
				}
				title={t("price-per-play")}
				textSize="4"
			/>
			{showScores && (
				<>
					<BgtTextStatistic content={decimalOrNull(gameStats.highScore)} title={t("high-score")} textSize="4" />
					<BgtTextStatistic content={decimalOrNull(gameStats.averageScore)} title={t("average-score")} textSize="4" />
				</>
			)}
			<BgtTextStatistic content={lastPlayedRelative} title={t("last-played")} textSize="4" />
			{showCoopRate && (
				<BgtTextStatistic
					content={formatRatio(gameStats.coopWinCount, coopPlayCount, settings?.uiLanguage)}
					title={t("team-win-rate")}
					caption={t("team-win-rate-count", { count: coopPlayCount, won: gameStats.coopWinCount })}
					textSize="4"
				/>
			)}
		</div>
	);
};
