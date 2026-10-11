import type { CreateSessionPlayer } from "@/models";

const hasScore = (score: number | null | undefined): score is number =>
	typeof score === "number" && !Number.isNaN(score);

export const applyScoreWinners = (players: CreateSessionPlayer[]): CreateSessionPlayer[] => {
	const scores = players.map((x) => x.score).filter(hasScore);
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
