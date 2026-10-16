import type { CreateSessionPlayer } from "@/models";

export const applyScoreWinners = (players: CreateSessionPlayer[]): CreateSessionPlayer[] => {
	const scores = players.map((x) => x.score).filter((x): x is number => typeof x === "number");
	const best = scores.length > 0 ? Math.max(...scores) : null;

	return players.map((player) => {
		const won = best !== null && player.score === best;
		return player.won === won ? player : { ...player, won };
	});
};

export const winnersMatchScores = (players: CreateSessionPlayer[]): boolean => {
	const derived = applyScoreWinners(players);
	return derived.every((player, index) => player.won === players[index].won);
};
