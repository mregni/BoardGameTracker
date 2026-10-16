import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import type { NightGameStatistic, PlannedGameStatistic } from "@/models";
import { StatRow, StatsCard, StatsEmpty, type StatsLookup } from "./StatsParts";

const VISIBLE_GAMES = 5;

interface Props {
	games: NightGameStatistic[];
	plannedNeverPlayed: PlannedGameStatistic[];
	lookup: StatsLookup;
}

export const NightGamesCard = ({ games, plannedNeverPlayed, lookup }: Props) => {
	const { t } = useTranslation("game-nights");
	const top = games.slice(0, VISIBLE_GAMES);
	const most = top[0]?.timesPlayed ?? 0;

	return (
		<StatsCard id="night-games" title={t("stats.games.title")}>
			{top.length === 0 ? (
				<StatsEmpty>{t("stats.games.empty")}</StatsEmpty>
			) : (
				<div className="flex flex-col gap-1">
					{top.map((entry, index) => {
						const game = lookup.games.get(entry.gameId);
						const name = game?.title ?? "";
						const detail = [t("stats.games.nights", { count: entry.nights })];
						if (entry.timesPlanned > 0) {
							detail.push(t("stats.games.planned", { count: entry.timesPlanned }));
						}
						return (
							<StatRow
								key={entry.gameId}
								rank={index + 1}
								avatar={<BgtAvatar title={name} image={game?.image} size="medium" />}
								name={name}
								detail={detail.join(" · ")}
								bar={
									<BgtProgressBar
										value={entry.timesPlayed}
										max={most}
										size="thin"
										tone="primary"
										aria-label={t("stats.games.bar", { name, count: entry.timesPlayed })}
									/>
								}
								value={t("stats.games.times", { count: entry.timesPlayed })}
							/>
						);
					})}
				</div>
			)}
			{plannedNeverPlayed.length > 0 && (
				<div className="flex flex-wrap items-center gap-1.5">
					<span className="text-xs text-gray-400">{t("stats.games.never-played")}</span>
					{plannedNeverPlayed.map((entry) => {
						const game = lookup.games.get(entry.gameId);
						return (
							<AvatarChip key={entry.gameId} title={game?.title ?? ""} image={game?.image}>
								<span className="text-gray-300">· {t("stats.games.times", { count: entry.timesPlanned })}</span>
							</AvatarChip>
						);
					})}
				</div>
			)}
		</StatsCard>
	);
};
