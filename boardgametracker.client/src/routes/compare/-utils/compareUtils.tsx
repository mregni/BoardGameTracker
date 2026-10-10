import type { ReactNode } from "react";
import Clock from "@/assets/icons/clock.svg?react";
import GamePad from "@/assets/icons/gamepad.svg?react";
import TrendingUp from "@/assets/icons/trend-up.svg?react";
import Trophy from "@/assets/icons/trophy.svg?react";
import type { Player } from "@/models";
import { formatMinutesToDuration } from "@/utils/dateUtils";

export interface CompareData {
	winCount: { playerOne: number; playerTwo: number };
	winPercentage: { playerOne: number; playerTwo: number };
	sessionCounts: { playerOne: number; playerTwo: number };
	totalDuration: { playerOne: number; playerTwo: number };
}

export interface StatConfig {
	key: string;
	translationKey: string;
	icon: ReactNode;
	getRawValue: (data: CompareData, player: "playerOne" | "playerTwo") => number;
	getValue: (data: CompareData, player: "playerOne" | "playerTwo") => string | number;
}

export const formatPlayTime = (minutes: number, uiLanguage: string): string =>
	formatMinutesToDuration(minutes, ["weeks", "days", "hours", "minutes"], uiLanguage) ?? "0";

export const formatWinRate = (fraction: number): string => `${Math.round(fraction * 1000) / 10}%`;

export const resolveComparePlayers = (
	players: Pick<Player, "id">[],
	left: number | undefined,
	right: number | undefined,
): [number, number] => {
	const exists = (id: number | undefined) => id !== undefined && players.some((p) => p.id === id);
	const firstOther = (otherId: number) => players.find((p) => p.id !== otherId)?.id ?? 0;

	const resolvedLeft = exists(left) ? (left as number) : firstOther(exists(right) ? (right as number) : 0);
	const resolvedRight = exists(right) && right !== resolvedLeft ? (right as number) : firstOther(resolvedLeft);

	return players.length < 2 ? [0, 0] : [resolvedLeft, resolvedRight];
};

export const getStatConfigs = (uiLanguage: string): StatConfig[] => [
	{
		key: "winCount",
		translationKey: "stats.total-wins",
		icon: <Trophy className="size-6" />,
		getRawValue: (data, player) => data.winCount[player],
		getValue: (data, player) => data.winCount[player],
	},
	{
		key: "winPercentage",
		translationKey: "stats.win-percentage",
		icon: <TrendingUp className="size-6" />,
		getRawValue: (data, player) => data.winPercentage[player],
		getValue: (data, player) => formatWinRate(data.winPercentage[player]),
	},
	{
		key: "sessionCounts",
		translationKey: "stats.sessions",
		icon: <GamePad className="size-6" />,
		getRawValue: (data, player) => data.sessionCounts[player],
		getValue: (data, player) => data.sessionCounts[player],
	},
	{
		key: "totalDuration",
		translationKey: "stats.total-duration",
		icon: <Clock className="size-6" />,
		getRawValue: (data, player) => data.totalDuration[player],
		getValue: (data, player) => formatPlayTime(data.totalDuration[player], uiLanguage),
	},
];

export const isWinningValue = (playerValue: string | number, opponentValue: string | number): boolean => {
	if (typeof playerValue === "number" && typeof opponentValue === "number") {
		return playerValue > opponentValue;
	}
	return String(playerValue).replace(/[^\d.]/g, "") > String(opponentValue).replace(/[^\d.]/g, "");
};

export const calculateWinCount = (
	compare: CompareData,
	playerKey: "playerOne" | "playerTwo",
	uiLanguage: string,
): number => {
	const opponentKey = playerKey === "playerOne" ? "playerTwo" : "playerOne";
	const statConfigs = getStatConfigs(uiLanguage);

	return statConfigs.reduce((winCount, stat) => {
		const playerValue = stat.getRawValue(compare, playerKey);
		const opponentValue = stat.getRawValue(compare, opponentKey);
		const isWinner = isWinningValue(playerValue, opponentValue);

		return isWinner ? winCount + 1 : winCount;
	}, 0);
};

export const calculateOverallWinner = (compare: CompareData, uiLanguage: string): "playerOne" | "playerTwo" | null => {
	const playerOneWins = calculateWinCount(compare, "playerOne", uiLanguage);
	const playerTwoWins = calculateWinCount(compare, "playerTwo", uiLanguage);

	if (playerOneWins > playerTwoWins) return "playerOne";
	if (playerTwoWins > playerOneWins) return "playerTwo";
	return null; // tie
};
