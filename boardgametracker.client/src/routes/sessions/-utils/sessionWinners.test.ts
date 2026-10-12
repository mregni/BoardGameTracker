import { describe, expect, it } from "vitest";
import type { CreateSessionPlayer } from "@/models";
import { applyScoreWinners, winnersMatchScores } from "./sessionWinners";

const entry = (playerId: number, score: number | null, won = false): CreateSessionPlayer => ({
	playerId,
	score,
	won,
	firstPlay: false,
});

describe("applyScoreWinners", () => {
	it("marks the player with the highest score as the winner", () => {
		const result = applyScoreWinners([entry(1, 70), entry(2, 120), entry(3, 95)]);

		expect(result.map((x) => x.won)).toEqual([false, true, false]);
	});

	it("marks every tied player as a winner", () => {
		const result = applyScoreWinners([entry(1, 80), entry(2, 80), entry(3, 40)]);

		expect(result.map((x) => x.won)).toEqual([true, true, false]);
	});

	it("ignores players without a score", () => {
		const result = applyScoreWinners([entry(1, null, true), entry(2, -5), entry(3, null)]);

		expect(result.map((x) => x.won)).toEqual([false, true, false]);
	});

	it("clears the winners when nobody has a score", () => {
		const result = applyScoreWinners([entry(1, null, true), entry(2, null)]);

		expect(result.map((x) => x.won)).toEqual([false, false]);
	});
});

describe("winnersMatchScores", () => {
	it("is true when the winners follow the scores", () => {
		expect(winnersMatchScores([entry(1, 10, true), entry(2, 5)])).toBe(true);
	});

	it("is false when a winner was picked by hand", () => {
		expect(winnersMatchScores([entry(1, 10), entry(2, 5, true)])).toBe(false);
	});
});
