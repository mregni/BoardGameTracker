import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import BarChart from "@/assets/icons/bar-chart.svg?react";
import { BgtEmptyState } from "@/components/BgtLayout/BgtEmptyState";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { BgtSegmentedControl } from "@/components/BgtSegmentedControl/BgtSegmentedControl";
import { type Game, GameNightStatisticsPeriod, type Location, type Player, type Settings } from "@/models";
import { getGameNightStatistics } from "@/services/queries/gameNights";
import { NightAttendanceCard } from "./NightAttendanceCard";
import { NightGamesCard } from "./NightGamesCard";
import { NightRecordsCard } from "./NightRecordsCard";
import { NightStatsTiles } from "./NightStatsTiles";
import { NightStreaksCard } from "./NightStreaksCard";
import { NightWinnerHistoryCard } from "./NightWinnerHistoryCard";
import { NightWinnersCard } from "./NightWinnersCard";
import type { StatsLookup } from "./StatsParts";

const PERIODS = [
	GameNightStatisticsPeriod.AllTime,
	GameNightStatisticsPeriod.ThisYear,
	GameNightStatisticsPeriod.Last12Months,
];

const PERIOD_LABELS: Record<GameNightStatisticsPeriod, string> = {
	[GameNightStatisticsPeriod.AllTime]: "common:period.all-time",
	[GameNightStatisticsPeriod.ThisYear]: "common:period.this-year",
	[GameNightStatisticsPeriod.Last12Months]: "common:period.last-12-months",
};

interface Props {
	period: GameNightStatisticsPeriod;
	onPeriodChange: (period: GameNightStatisticsPeriod) => void;
	settings: Settings;
	players: Player[];
	games: Game[];
	locations: Location[];
}

export const GameNightStatsView = (props: Props) => {
	const { period, onPeriodChange, settings, players, games, locations } = props;
	const { t } = useTranslation("game-nights");
	const { data: statistics, isLoading } = useQuery({
		...getGameNightStatistics(period),
		placeholderData: keepPreviousData,
	});

	const lookup = useMemo<StatsLookup>(
		() => ({
			players: new Map(players.map((player) => [player.id, player])),
			games: new Map(games.map((game) => [game.id, game])),
			locations: new Map(locations.map((location) => [location.id, location])),
			uiLanguage: settings.uiLanguage,
			dateFormat: settings.dateFormat,
			timeFormat: settings.timeFormat,
		}),
		[players, games, locations, settings],
	);

	return (
		<div className="flex flex-col gap-3">
			<div className="flex flex-wrap items-center justify-between gap-2">
				<BgtSegmentedControl
					aria-label={t("common:period.label")}
					size="sm"
					value={period}
					onChange={onPeriodChange}
					options={PERIODS.map((value) => ({ value, label: t(PERIOD_LABELS[value]) }))}
				/>
				{statistics && statistics.totalNights > 0 && (
					<span className="text-xs text-gray-400">
						{t("stats.logged", { count: statistics.nightsWithSessions, total: statistics.totalNights })}
					</span>
				)}
			</div>

			{isLoading || !statistics ? (
				<BgtLoadingSpinner />
			) : statistics.nights === 0 ? (
				<BgtEmptyState icon={BarChart} title={t("stats.empty.title")} description={t("stats.empty.description")} />
			) : (
				<>
					<NightStatsTiles statistics={statistics} />
					<div className="grid items-start gap-3 lg:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
						<div className="flex min-w-0 flex-col gap-3">
							<NightWinnersCard winners={statistics.winners} tiedNights={statistics.tiedNights} lookup={lookup} />
							<NightWinnerHistoryCard history={statistics.history} lookup={lookup} />
							<NightGamesCard
								games={statistics.games}
								plannedNeverPlayed={statistics.plannedNeverPlayed}
								lookup={lookup}
							/>
						</div>
						<div className="flex min-w-0 flex-col gap-3">
							<NightStreaksCard streaks={statistics.streaks} lookup={lookup} />
							<NightAttendanceCard
								attendance={statistics.attendance}
								recentNights={statistics.recentNights}
								nights={statistics.nights}
								lookup={lookup}
							/>
							<NightRecordsCard records={statistics.records} lookup={lookup} />
						</div>
					</div>
				</>
			)}
		</div>
	);
};
