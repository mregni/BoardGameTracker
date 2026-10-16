import { Link } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { GameNightDateBox } from "@/components/GameNight/GameNightDateBox";
import type { NightHistory } from "@/models";
import { StatsCard, StatsEmpty, type StatsLookup } from "./StatsParts";

const VISIBLE_NIGHTS = 5;
const STREAK_PILL_FROM = 3;

interface Props {
	history: NightHistory[];
	lookup: StatsLookup;
}

const Outcome = ({ night, lookup }: { night: NightHistory; lookup: StatsLookup }) => {
	const { t } = useTranslation("game-nights");

	if (night.gamesPlayed === 0) {
		return <span className="text-xs text-gray-400">{t("stats.history.no-sessions")}</span>;
	}

	if (night.sharedThroughCoop) {
		return (
			<span className="rounded-full bg-ok/15 px-2 py-0.5 text-xs font-semibold text-ok">
				{t("stats.history.coop-shared", { count: night.winnerIds.length })}
			</span>
		);
	}

	if (night.winnerIds.length === 0) {
		return (
			<span className="rounded-full bg-card-light px-2 py-0.5 text-xs font-semibold text-gray-400">
				{t("stats.history.no-winner")}
			</span>
		);
	}

	return (
		<>
			{night.winnerIds.map((id) => {
				const player = lookup.players.get(id);
				return (
					<AvatarChip key={id} title={player?.name ?? ""} image={player?.image} tone="winner">
						<span className="text-gray-300">
							· {t("stats.history.score", { wins: night.mostWins, total: night.gamesPlayed })}
						</span>
					</AvatarChip>
				);
			})}
			{night.winnerStreak >= STREAK_PILL_FROM && (
				<span className="rounded-full bg-warn/15 px-2 py-0.5 text-xs font-semibold text-warn">
					{t("stats.history.streak", { count: night.winnerStreak })}
				</span>
			)}
		</>
	);
};

export const NightWinnerHistoryCard = ({ history, lookup }: Props) => {
	const { t } = useTranslation("game-nights");
	const [showAll, setShowAll] = useState(false);
	const visible = showAll ? history : history.slice(0, VISIBLE_NIGHTS);

	return (
		<StatsCard
			id="night-winner-history"
			title={t("stats.history.title")}
			action={
				history.length > VISIBLE_NIGHTS && (
					<BgtButton variant="text" size="1" onClick={() => setShowAll(!showAll)} aria-expanded={showAll}>
						{showAll ? t("stats.history.less") : t("stats.history.all", { count: history.length })}
					</BgtButton>
				)
			}
		>
			{history.length === 0 ? (
				<StatsEmpty>{t("stats.winners.empty")}</StatsEmpty>
			) : (
				<ul className="flex flex-col">
					{visible.map((night) => {
						const location = lookup.locations.get(night.locationId)?.name;
						return (
							<li key={night.gameNightId} className="@container border-t border-card-border first:border-t-0">
								<Link
									to="/game-nights/$gameNightId"
									params={{ gameNightId: night.gameNightId }}
									className="grid grid-cols-[2.75rem_minmax(0,1fr)] items-center gap-x-3 gap-y-1 rounded-lg px-1 py-2 hover:bg-card-light @md:grid-cols-[2.75rem_minmax(0,1fr)_auto]"
								>
									<GameNightDateBox date={night.startDate} uiLanguage={lookup.uiLanguage} variant="compact" />
									<span className="min-w-0">
										<strong className="block truncate text-sm text-white">{night.title}</strong>
										<span className="block text-xs text-gray-400">
											{[
												t("stats.history.games", { count: night.gamesPlayed }),
												night.coopOnly && t("stats.history.coop-only"),
												location,
											]
												.filter(Boolean)
												.join(" · ")}
										</span>
									</span>
									<span className="col-start-2 flex flex-wrap items-center gap-1 @md:col-start-3 @md:max-w-[55cqw] @md:justify-end">
										<Outcome night={night} lookup={lookup} />
									</span>
								</Link>
							</li>
						);
					})}
				</ul>
			)}
		</StatsCard>
	);
};
