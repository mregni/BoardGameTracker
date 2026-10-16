import { useTranslation } from "react-i18next";
import Clock from "@/assets/icons/clock.svg?react";
import GamePad from "@/assets/icons/gamepad.svg?react";
import Target from "@/assets/icons/target.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { BgtTextStatistic } from "@/components/BgtStatistic/BgtTextStatistic";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import type { CompareResult, Player } from "@/models";
import { useGameById } from "@/routes/-hooks/useGameById";
import { toDisplay } from "@/utils/dateUtils";
import { useShortDuration } from "../-hooks/useShortDuration";

interface Props {
	compare: CompareResult;
	playerOne: Player;
	playerTwo: Player;
	dateFormat: string;
	uiLanguage: string;
}

export const TogetherTiles = ({ compare, playerOne, playerTwo, dateFormat, uiLanguage }: Props) => {
	const { t } = useTranslation("compare");
	const { gameById } = useGameById();
	const { number } = useNumberFormat();
	const duration = useShortDuration();

	const count = compare.totalSessionsTogether;
	const mostPlayed = compare.preferredGame?.gameId ? gameById(compare.preferredGame.gameId) : null;
	const closest = compare.closestGame;
	const closestGame = closest?.gameId ? gameById(closest.gameId) : null;
	const closestWinner =
		closest?.playerId === playerOne.id ? playerOne : closest?.playerId === playerTwo.id ? playerTwo : null;

	return (
		<div className="grid grid-cols-2 gap-2 sm:gap-3 lg:grid-cols-4">
			<BgtTextStatistic
				title={t("tiles.games-together")}
				icon={<Users />}
				content={count}
				caption={
					compare.firstGameTogether?.startDate
						? t("tiles.since", { date: toDisplay(compare.firstGameTogether.startDate, dateFormat, uiLanguage) })
						: null
				}
			/>
			<BgtTextStatistic
				title={t("tiles.time-together")}
				icon={<Clock />}
				content={duration(compare.minutesPlayed)}
				caption={count > 0 ? t("tiles.average", { duration: duration(compare.minutesPlayed / count) }) : null}
			/>
			{mostPlayed && compare.preferredGame && (
				<BgtTextStatistic
					title={t("tiles.most-played")}
					icon={<GamePad />}
					content={mostPlayed.title}
					link={`/games/${mostPlayed.id}`}
					caption={t("games", { count: compare.preferredGame.sessionCount })}
				/>
			)}
			{closest && closestGame && (
				<BgtTextStatistic
					title={t("tiles.closest")}
					icon={<Target />}
					content={t("tiles.points", { count: closest.scoringDifference, value: number(closest.scoringDifference) })}
					caption={
						closestWinner
							? t("tiles.closest-winner", { game: closestGame.title, player: closestWinner.name })
							: t("tiles.closest-other", { game: closestGame.title })
					}
				/>
			)}
		</div>
	);
};
