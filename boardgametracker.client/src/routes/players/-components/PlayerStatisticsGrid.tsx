import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { BgtCard } from "@/components/BgtCard/BgtCard";
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

interface Nemesis {
	playerId: number;
	name: string;
	wins: number;
	losses: number;
}

interface Props {
	playerId: number;
	statistics: PlayerStatistics;
	settings?: Settings;
	lastPlayed?: LastPlayed | null;
	form?: { id: number; won: boolean }[];
	nemesis?: Nemesis | null;
}

const Figure = ({ label, value, note }: { label: string; value: ReactNode; note?: ReactNode }) => (
	<div className="flex min-w-0 flex-col">
		<dt className="text-sm text-stat-title">{label}</dt>
		<dd className="text-3xl font-bold tabular-nums text-card-value">{value}</dd>
		{note && <dd className="text-xs text-white/60">{note}</dd>}
	</div>
);

const Fact = ({ label, value, note }: { label: string; value: ReactNode; note?: ReactNode }) => (
	<div className="flex min-w-0 flex-col gap-0.5">
		<dt className="text-xs text-stat-title">{label}</dt>
		<dd className="min-w-0 font-semibold text-white">{value}</dd>
		{note && <dd className="truncate text-xs text-white/60">{note}</dd>}
	</div>
);

export const PlayerStatisticsGrid = (props: Props) => {
	const { playerId, statistics, settings, lastPlayed, form = [], nemesis } = props;
	const { t } = useTranslation(["statistics", "common", "player"]);
	const shortDuration = useShortDuration();
	const language = settings?.uiLanguage;

	const favorite = statistics.mostPlayedGames[0];
	const hasCoopPlays = statistics.coopWinCount + statistics.coopLossCount > 0;
	const hasFacts = Boolean(favorite || (lastPlayed && settings) || hasCoopPlays || nemesis);
	const formWins = form.filter((entry) => entry.won).length;

	return (
		<BgtCard className="@container">
			<dl className="grid grid-cols-2 gap-x-6 gap-y-4 @xl:grid-cols-4">
				<Figure label={t("play-count")} value={formatInteger(statistics.playCount, language)} />
				<Figure
					label={t("common:wins")}
					value={formatInteger(statistics.winCount, language)}
					note={t("player:record.win-rate", {
						value: formatRatio(statistics.winCount, statistics.playCount, language),
					})}
				/>
				<Figure label={t("total-play-time")} value={shortDuration(statistics.totalPlayedTime)} />
				<Figure label={t("distinct-game-count")} value={formatInteger(statistics.distinctGameCount, language)} />
			</dl>
			{form.length > 0 && (
				<div className="mt-4 flex flex-wrap items-center gap-x-3 gap-y-2 border-t border-white/10 pt-4">
					<span className="text-xs text-stat-title">{t("player:record.form", { count: form.length })}</span>
					<span className="sr-only">
						{t("player:record.form-summary", { won: formWins, lost: form.length - formWins })}
					</span>
					<ol aria-hidden="true" className="flex gap-1">
						{form.map((entry) => (
							<li
								key={entry.id}
								className={cx(
									"flex size-6 items-center justify-center rounded-sm text-xs font-bold",
									entry.won ? "bg-ok/20 text-ok" : "bg-white/5 text-white/50",
								)}
							>
								{entry.won ? t("player:record.form-win") : t("player:record.form-loss")}
							</li>
						))}
					</ol>
				</div>
			)}
			{hasFacts && (
				<dl className="mt-4 grid grid-cols-2 gap-3 border-t border-white/10 pt-4 @md:grid-cols-3 @2xl:grid-cols-4">
					{favorite && (
						<Fact
							label={t("favorite-game")}
							value={
								<Link
									to="/games/$gameId"
									params={{ gameId: favorite.id }}
									className="block truncate hover:text-primary-text"
								>
									{favorite.title}
								</Link>
							}
							note={t("common:sessions", { count: favorite.totalSessions })}
						/>
					)}
					{lastPlayed && settings && (
						<Fact
							label={t("last-played")}
							value={toDisplay(lastPlayed.start, settings.dateFormat, settings.uiLanguage)}
							note={lastPlayed.gameTitle}
						/>
					)}
					{nemesis && (
						<Fact
							label={t("player:record.nemesis")}
							value={
								<Link
									to="/compare"
									search={{ left: playerId, right: nemesis.playerId }}
									className="block truncate hover:text-primary-text"
								>
									{nemesis.name}
								</Link>
							}
							note={t("player:rivals.record", { wins: nemesis.wins, losses: nemesis.losses })}
						/>
					)}
					{hasCoopPlays && (
						<Fact
							label={t("coop-record")}
							value={t("coop-results", {
								won: formatInteger(statistics.coopWinCount, language),
								lost: formatInteger(statistics.coopLossCount, language),
							})}
						/>
					)}
				</dl>
			)}
		</BgtCard>
	);
};
