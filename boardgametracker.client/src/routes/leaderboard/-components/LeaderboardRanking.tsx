import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { LeaderboardEntry, LeaderboardRankBy } from "@/models";

interface Props {
	players: LeaderboardEntry[];
	rankBy: LeaderboardRankBy;
	minimumSessions: number;
	label: string;
}

const baseColumns = "grid items-center gap-x-3 gap-y-1 grid-cols-[1.375rem_1.75rem_minmax(0,1fr)_auto_0.875rem]";
const withPodiums =
	"md:grid-cols-[1.625rem_1.875rem_minmax(0,1.3fr)_3.5rem_4.25rem_minmax(8rem,1.3fr)_5rem_4.25rem_1rem]";
const withoutPodiums =
	"md:grid-cols-[1.625rem_1.875rem_minmax(0,1.3fr)_3.5rem_4.25rem_minmax(8rem,1.3fr)_4.25rem_1rem]";
const columnsFor = (showPodiums: boolean) => cx(baseColumns, showPodiums ? withPodiums : withoutPodiums);

const desktopOnly = "hidden md:block";
export const columnLabel = "text-xs font-medium text-white/60";

interface RowProps {
	entry: LeaderboardEntry;
	rankBy: LeaderboardRankBy;
	showPodiums: boolean;
}

const Row = ({ entry, rankBy, showPodiums }: RowProps) => {
	const { t } = useTranslation("leaderboard");
	const format = useNumberFormat();
	const duration = useShortDuration();
	const rate = format.percent(entry.winPercentage / 100);
	const ranked = cx("font-bold", entry.isRanked ? "text-card-value" : "text-white/60");

	const wins = t("values.wins", { count: entry.winCount });
	const sessions = t("values.sessions", { count: entry.playCount });
	const mobileValue = rankBy === "wins" ? wins : rankBy === "sessions" ? sessions : rate;
	const standing = entry.isRanked
		? showPodiums
			? t("values.podiums", { count: entry.podiumCount })
			: null
		: t("values.to-rank", { count: entry.sessionsToRank });
	const details = [rankBy === "wins" ? rate : wins, rankBy === "sessions" ? rate : sessions, standing]
		.filter(Boolean)
		.join(" · ");
	const spoken = [
		entry.rank === null ? t("ranking.not-ranked-rank") : `${t("ranking.place")} ${entry.rank}`,
		entry.name,
		wins,
		sessions,
		`${t("columns.win-rate")} ${rate}`,
		showPodiums && entry.isRanked ? t("values.podiums", { count: entry.podiumCount }) : null,
		entry.isRanked ? null : t("values.to-rank", { count: entry.sessionsToRank }),
		`${t("columns.time")} ${duration(entry.minutesPlayed)}`,
	]
		.filter(Boolean)
		.join(", ");

	return (
		<li>
			<Link
				to="/players/$playerId"
				params={{ playerId: entry.playerId }}
				data-testid="leaderboard-row"
				aria-label={spoken}
				className={cx(
					columnsFor(showPodiums),
					"rounded-[10px] border px-2.5 py-2 tabular-nums transition-colors hover:border-primary/60 md:px-3",
					entry.isRanked ? "border-card-border bg-card" : "border-dashed border-card-border bg-transparent",
				)}
			>
				<span
					className={cx(
						"text-right font-bold",
						entry.rank === 1 ? "text-gold" : entry.rank !== null && entry.rank <= 3 ? "text-white" : "text-white/60",
					)}
				>
					{entry.rank ?? (
						<>
							<span aria-hidden="true">–</span>
							<span className="sr-only">{t("ranking.not-ranked-rank")}</span>
						</>
					)}
				</span>
				<span aria-hidden="true">
					<BgtAvatar image={entry.image} title={entry.name} size="medium" />
				</span>
				<span className="min-w-0">
					<strong className="block truncate">{entry.name}</strong>
					<small className="block text-xs text-white/60 md:hidden">{details}</small>
					{!entry.isRanked && (
						<small className="hidden text-xs text-white/60 md:block">
							{t("values.to-rank", { count: entry.sessionsToRank })}
						</small>
					)}
				</span>
				<span className={cx(desktopOnly, "text-right", rankBy === "wins" && ranked)}>
					{format.integer(entry.winCount)}
				</span>
				<span className={cx(desktopOnly, "text-right", rankBy === "sessions" && ranked)}>
					{format.integer(entry.playCount)}
				</span>
				<span className="grid grid-cols-1 items-center gap-2 md:grid-cols-[minmax(0,1fr)_3.25rem]">
					<BgtProgressBar
						className="hidden md:flex"
						value={entry.winPercentage}
						max={100}
						size="thin"
						tone={entry.isRanked ? "value" : "muted"}
						aria-label={t("columns.win-rate")}
						aria-valuetext={rate}
					/>
					<span className={cx("hidden text-right md:block", rankBy === "winRate" ? ranked : "text-white/80")}>
						{rate}
					</span>
					<b
						data-testid="leaderboard-mobile-value"
						className={cx("text-right md:hidden", entry.isRanked ? "text-card-value" : "text-white/60")}
					>
						{mobileValue}
					</b>
				</span>
				{showPodiums && <span className={cx(desktopOnly, "text-right")}>{format.integer(entry.podiumCount)}</span>}
				<span className={cx(desktopOnly, "text-right text-sm text-white/60")}>{duration(entry.minutesPlayed)}</span>
				<CaretRight aria-hidden="true" className="size-3.5 text-white/60 md:size-4" />
			</Link>
		</li>
	);
};

export const LeaderboardRanking = ({ players, rankBy, minimumSessions, label }: Props) => {
	const { t } = useTranslation("leaderboard");
	const ranked = players.filter((entry) => entry.isRanked);
	const unranked = players.filter((entry) => !entry.isRanked);
	const showPodiums = players.some((entry) => entry.podiumCount > 0);

	return (
		<section aria-label={label} className="flex flex-col gap-1.5">
			<h2 className="sr-only">{label}</h2>
			<div
				aria-hidden="true"
				className={cx(columnsFor(showPodiums), columnLabel, "hidden px-3 md:grid [&>span]:whitespace-nowrap")}
			>
				<span className="text-right">#</span>
				<span />
				<span>{t("columns.player")}</span>
				<span className="text-right">{t("columns.wins")}</span>
				<span className="text-right">{t("columns.sessions")}</span>
				<span>{t("columns.win-rate")}</span>
				{showPodiums && <span className="text-right">{t("columns.podiums")}</span>}
				<span className="text-right">{t("columns.time")}</span>
				<span />
			</div>
			{ranked.length > 0 && (
				<ol className="flex flex-col gap-1.5">
					{ranked.map((entry) => (
						<Row key={entry.playerId} entry={entry} rankBy={rankBy} showPodiums={showPodiums} />
					))}
				</ol>
			)}
			{unranked.length > 0 && (
				<>
					<h3 className={cx(columnLabel, "pt-2")}>{t("ranking.not-ranked", { count: minimumSessions })}</h3>
					<ul className="flex flex-col gap-1.5">
						{unranked.map((entry) => (
							<Row key={entry.playerId} entry={entry} rankBy={rankBy} showPodiums={showPodiums} />
						))}
					</ul>
				</>
			)}
		</section>
	);
};
