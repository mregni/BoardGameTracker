import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { LeaderboardEntry, LeaderboardRankBy } from "@/models";
import { HeaderWithHint } from "./HeaderWithHint";

interface Props {
	players: LeaderboardEntry[];
	rankBy: LeaderboardRankBy;
	minimumSessions: number;
	minimumPlayersForPodium: number;
	label: string;
	winCount: number;
	coopWinCount: number;
}

const baseColumns = "grid items-center gap-x-3 gap-y-1 grid-cols-[1.375rem_1.75rem_minmax(0,1fr)_auto_0.875rem]";
const withPodiums =
	"md:grid-cols-[1.625rem_1.875rem_minmax(0,1.3fr)_3.5rem_4.25rem_minmax(8rem,1.3fr)_5rem_4.25rem_1rem]";
const withoutPodiums =
	"md:grid-cols-[1.625rem_1.875rem_minmax(0,1.3fr)_3.5rem_4.25rem_minmax(8rem,1.3fr)_4.25rem_1rem]";
const columnsFor = (showPodiums: boolean) => cx(baseColumns, showPodiums ? withPodiums : withoutPodiums);

const desktopOnly = "hidden md:block";
const columnLabel = "text-xs font-medium uppercase tracking-wider text-white/60";

const tieRule: Record<LeaderboardRankBy, string> = {
	wins: "ranking.ties-wins",
	winRate: "ranking.ties-win-rate",
	sessions: "ranking.ties-sessions",
};

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
	const isTop = entry.rank !== null && entry.rank <= 3;

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

	return (
		<li>
			<Link
				to="/players/$playerId"
				params={{ playerId: entry.playerId }}
				data-testid="leaderboard-row"
				className={cx(
					columnsFor(showPodiums),
					"rounded-[10px] border px-2.5 py-2 tabular-nums transition-colors hover:border-primary/60 md:px-3",
					entry.isRanked ? "border-card-border bg-card" : "border-dashed border-card-border bg-transparent",
				)}
			>
				<span className={cx("text-right font-bold", isTop ? "text-yellow-400" : "text-white/60")}>
					{entry.rank ?? (
						<>
							<span aria-hidden="true">–</span>
							<span className="sr-only">{t("ranking.not-ranked-rank")}</span>
						</>
					)}
				</span>
				<BgtAvatar image={entry.image} title={entry.name} size="medium" />
				<span className="min-w-0">
					<strong className="block truncate">{entry.name}</strong>
					<small className="block text-xs text-white/60 md:hidden">{details}</small>
				</span>
				<span className={cx(desktopOnly, "text-right", rankBy === "wins" && "font-bold")}>
					{format.integer(entry.winCount)}
				</span>
				<span className={cx(desktopOnly, "text-right", rankBy === "sessions" && "font-bold")}>
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
					<span
						className={cx(
							"hidden text-right md:block",
							rankBy === "winRate" && "font-bold",
							entry.isRanked ? "text-card-value" : "text-white/60",
						)}
					>
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

export const LeaderboardRanking = ({
	players,
	rankBy,
	minimumSessions,
	minimumPlayersForPodium,
	label,
	winCount,
	coopWinCount,
}: Props) => {
	const { t } = useTranslation("leaderboard");
	const ranked = players.filter((entry) => entry.isRanked);
	const unranked = players.filter((entry) => !entry.isRanked);
	const showPodiums = players.some((entry) => entry.podiumCount > 0);

	return (
		<section aria-label={label} className="flex flex-col gap-1.5">
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
				{showPodiums && (
					<span className="text-right">
						<HeaderWithHint
							label={t("columns.podiums")}
							hint={t("columns.podiums-hint", { count: minimumPlayersForPodium })}
						/>
					</span>
				)}
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
			{(coopWinCount > 0 || ranked.length > 1) && (
				<p className="max-w-[75ch] pt-1 text-xs leading-5 text-pretty text-white/60">
					{[
						coopWinCount > 0 ? t("ranking.coop", { count: coopWinCount, total: winCount }) : null,
						ranked.length > 1 ? t(tieRule[rankBy]) : null,
					]
						.filter(Boolean)
						.join(" ")}
				</p>
			)}
		</section>
	);
};
