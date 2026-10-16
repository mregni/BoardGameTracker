import { useTranslation } from "react-i18next";
import CheckIcon from "@/assets/icons/check.svg?react";
import CrownIcon from "@/assets/icons/crown.svg?react";
import GamepadIcon from "@/assets/icons/gamepad.svg?react";
import type { GameNightStreaks, PlayerStreak } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { StatFact, StatsCard, StatsEmpty, type StatsLookup } from "./StatsParts";

interface Props {
	streaks: GameNightStreaks;
	lookup: StatsLookup;
}

const sameStreak = (left: PlayerStreak, right: PlayerStreak) =>
	left.playerId === right.playerId && left.length === right.length;

export const NightStreaksCard = ({ streaks, lookup }: Props) => {
	const { t } = useTranslation("game-nights");
	const { currentWinner, longestWinner, currentAttendance, longestGame } = streaks;
	const date = (value: Date) => toDisplay(value, lookup.dateFormat, lookup.uiLanguage);
	const playerName = (id: number) => lookup.players.get(id)?.name ?? "";

	const winnerFact = () => {
		if (currentWinner) {
			const hint = [t("stats.streaks.winner-current-hint")];
			if (longestWinner && !sameStreak(currentWinner, longestWinner)) {
				hint.push(
					t("stats.streaks.winner-record", {
						name: playerName(longestWinner.playerId),
						count: longestWinner.length,
						from: date(longestWinner.from),
						to: date(longestWinner.to),
					}),
				);
			}
			return (
				<StatFact
					tone="gold"
					icon={<CrownIcon />}
					title={t("stats.streaks.winner-current", {
						name: playerName(currentWinner.playerId),
						count: currentWinner.length,
					})}
					hint={hint.join(" · ")}
				/>
			);
		}

		if (longestWinner) {
			return (
				<StatFact
					tone="gold"
					icon={<CrownIcon />}
					title={t("stats.streaks.winner-longest", {
						name: playerName(longestWinner.playerId),
						count: longestWinner.length,
					})}
					hint={t("stats.streaks.winner-longest-hint", {
						from: date(longestWinner.from),
						to: date(longestWinner.to),
					})}
				/>
			);
		}

		return null;
	};

	const hasStreaks = currentWinner || longestWinner || currentAttendance || longestGame;

	return (
		<StatsCard id="night-streaks" title={t("stats.streaks.title")}>
			{hasStreaks ? (
				<div className="flex flex-col gap-2">
					{winnerFact()}
					{currentAttendance && (
						<StatFact
							icon={<CheckIcon />}
							title={t("stats.streaks.attendance", {
								name: playerName(currentAttendance.playerId),
								count: currentAttendance.length,
							})}
							hint={t("stats.streaks.attendance-hint", { date: date(currentAttendance.from) })}
						/>
					)}
					{longestGame && (
						<StatFact
							icon={<GamepadIcon />}
							title={t("stats.streaks.game", {
								name: lookup.games.get(longestGame.gameId)?.title ?? "",
								count: longestGame.length,
							})}
							hint={
								longestGame.isCurrent
									? t("stats.streaks.game-current", { date: date(longestGame.from) })
									: t("stats.streaks.game-longest", { from: date(longestGame.from), to: date(longestGame.to) })
							}
						/>
					)}
				</div>
			) : (
				<StatsEmpty>{t("stats.streaks.empty")}</StatsEmpty>
			)}
		</StatsCard>
	);
};
