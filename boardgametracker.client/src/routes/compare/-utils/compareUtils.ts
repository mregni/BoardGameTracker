import type { CompareGame, CompareOutcome, CompareResult, CompareRow, CompareSide } from "@/models";

export type GameSort = "most-played" | "biggest-lead";

export const resolveComparePlayers = (
	playerIds: number[],
	left: number | undefined,
	right: number | undefined,
	preferred: number[] = [],
): [number, number] => {
	if (playerIds.length < 2) {
		return [0, 0];
	}

	const known = (id: number | undefined): id is number => id !== undefined && playerIds.includes(id);
	const order = [...preferred.filter(known), ...playerIds];

	let resolvedLeft = known(left) ? left : undefined;
	let resolvedRight = known(right) && right !== resolvedLeft ? right : undefined;
	resolvedLeft ??= order.find((id) => id !== resolvedRight);
	resolvedRight ??= order.find((id) => id !== resolvedLeft);

	return [resolvedLeft ?? 0, resolvedRight ?? 0];
};

export const initialOf = (name: string): string => name.trim().charAt(0).toLocaleUpperCase() || "?";

export const leaderOf = (wins: CompareRow<number>): CompareSide | null => {
	if (wins.playerOne === wins.playerTwo) {
		return null;
	}

	return wins.playerOne > wins.playerTwo ? "playerOne" : "playerTwo";
};

export const leadOf = (game: CompareGame): number => Math.abs(game.playerOneWins - game.playerTwoWins);

export const sortGames = (games: CompareGame[], sort: GameSort): CompareGame[] => {
	const sorted = [...games];
	if (sort === "biggest-lead") {
		return sorted.sort((a, b) => leadOf(b) - leadOf(a) || b.sessionCount - a.sessionCount || a.gameId - b.gameId);
	}

	return sorted.sort(
		(a, b) =>
			b.sessionCount - a.sessionCount ||
			new Date(b.lastPlayed).getTime() - new Date(a.lastPlayed).getTime() ||
			a.gameId - b.gameId,
	);
};

export const isSide = (outcome: CompareOutcome): outcome is CompareSide =>
	outcome === "playerOne" || outcome === "playerTwo";

const flipped: Record<CompareOutcome, CompareOutcome> = {
	playerOne: "playerTwo",
	playerTwo: "playerOne",
	both: "both",
	other: "other",
	sameTeam: "sameTeam",
};

const flip = (outcome: CompareOutcome): CompareOutcome => flipped[outcome];

const swap = <T>(row: CompareRow<T>): CompareRow<T> => ({ playerOne: row.playerTwo, playerTwo: row.playerOne });

export const mirrorCompare = (result: CompareResult): CompareResult => ({
	...result,
	winCount: swap(result.winCount),
	winPercentage: swap(result.winPercentage),
	sessionCounts: swap(result.sessionCounts),
	totalDuration: swap(result.totalDuration),
	directWins: swap(result.directWins),
	bestGames: swap(result.bestGames),
	streak: result.streak && { ...result.streak, side: flip(result.streak.side) },
	games: result.games.map((game) => ({
		...game,
		playerOneWins: game.playerTwoWins,
		playerTwoWins: game.playerOneWins,
		playerOneAverageScore: game.playerTwoAverageScore,
		playerTwoAverageScore: game.playerOneAverageScore,
		lastOutcome: flip(game.lastOutcome),
	})),
	recentSessions: result.recentSessions.map((session) => ({
		...session,
		playerOneScore: session.playerTwoScore,
		playerTwoScore: session.playerOneScore,
		outcome: flip(session.outcome),
	})),
});
