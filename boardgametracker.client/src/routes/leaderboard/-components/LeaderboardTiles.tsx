import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { Leaderboard, LeaderboardEntry, LeaderboardRankBy } from "@/models";
import { EMPTY_NUMBER } from "@/utils/numberUtils";
import { columnLabel } from "./LeaderboardRanking";

interface Props {
	leaderboard: Leaderboard;
	podium: LeaderboardEntry[];
}

interface Tile {
	key: string;
	title: string;
	entry: LeaderboardEntry | null;
	value: (entry: LeaderboardEntry) => string;
	suffix?: string;
	repeats?: LeaderboardRankBy;
}

const cell =
	"flex min-w-0 items-center justify-between gap-3 px-3 py-2.5 sm:flex-col sm:items-start sm:justify-start sm:gap-1 sm:px-4 sm:py-3";

export const LeaderboardTiles = ({ leaderboard, podium }: Props) => {
	const { t } = useTranslation("leaderboard");
	const format = useNumberFormat();
	const duration = useShortDuration();

	const tiles: Tile[] = [
		{
			key: "most-wins",
			title: t("highlights.most-wins"),
			entry: leaderboard.mostWins,
			value: (entry) => format.integer(entry.winCount),
			repeats: "wins",
		},
		{
			key: "most-sessions",
			title: t("highlights.most-sessions"),
			entry: leaderboard.mostPlays,
			value: (entry) => format.integer(entry.playCount),
			repeats: "sessions",
		},
		{
			key: "best-win-rate",
			title: t("highlights.best-win-rate"),
			entry: leaderboard.bestWinRate,
			value: (entry) => format.percent(entry.winPercentage / 100),
			suffix: t("highlights.min-sessions", { count: leaderboard.minimumSessions }),
			repeats: "winRate",
		},
		{
			key: "most-time",
			title: t("highlights.most-time"),
			entry: leaderboard.mostTimePlayed,
			value: (entry) => duration(entry.minutesPlayed),
		},
	];

	const shown = tiles.filter(
		(tile) =>
			tile.repeats !== leaderboard.rankBy &&
			!(tile.repeats && podium.some((entry) => entry.playerId === tile.entry?.playerId)),
	);

	return (
		<section aria-labelledby="leaderboard-records">
			<h2 id="leaderboard-records" className="sr-only">
				{t("highlights.title")}
			</h2>
			<ul className="grid divide-y divide-card-border overflow-hidden rounded-lg border border-card-border bg-card sm:auto-cols-fr sm:grid-flow-col sm:divide-x sm:divide-y-0">
				{shown.map((tile) => {
					const content = (
						<>
							<span className={cx(columnLabel, "truncate")}>
								{tile.title}
								{tile.suffix && ` · ${tile.suffix}`}
							</span>
							<span className="flex min-w-0 items-center gap-2">
								<span className="text-lg font-bold text-card-value tabular-nums">
									{tile.entry ? tile.value(tile.entry) : EMPTY_NUMBER}
								</span>
								{tile.entry ? (
									<>
										<span aria-hidden="true">
											<BgtAvatar image={tile.entry.image} title={tile.entry.name} size="small" />
										</span>
										<span className="truncate text-sm text-white/70">{tile.entry.name}</span>
									</>
								) : (
									<span className="truncate text-sm text-white/60">{t("highlights.nobody-yet")}</span>
								)}
							</span>
						</>
					);
					return (
						<li key={tile.key} className="min-w-0">
							{tile.entry ? (
								<Link
									to="/players/$playerId"
									params={{ playerId: tile.entry.playerId }}
									aria-label={`${tile.title}${tile.suffix ? `, ${tile.suffix}` : ""}: ${tile.entry.name}, ${tile.value(tile.entry)}`}
									className={cx(cell, "transition-colors hover:bg-primary/10")}
								>
									{content}
								</Link>
							) : (
								<div className={cell}>{content}</div>
							)}
						</li>
					);
				})}
			</ul>
		</section>
	);
};
