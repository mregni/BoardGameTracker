import { describe, expect, it } from "vitest";
import { CreateGameSchema } from "./CreateGame";
import { GameState } from "./GameState";

const baseGame = {
	title: "Brass: Birmingham",
	additionDate: "2026-01-01",
	state: GameState.Wanted,
	hasScoring: false,
};

describe("CreateGameSchema buyingPrice", () => {
	it("should preserve a buying price of 0 instead of nulling it", () => {
		const result = CreateGameSchema.parse({ ...baseGame, buyingPrice: 0 });

		expect(result.buyingPrice).toBe(0);
	});

	it("should map an absent buying price to null", () => {
		const result = CreateGameSchema.parse({ ...baseGame, buyingPrice: undefined });

		expect(result.buyingPrice).toBeNull();
	});

	it("should keep a positive buying price", () => {
		const result = CreateGameSchema.parse({ ...baseGame, buyingPrice: 22.5 });

		expect(result.buyingPrice).toBe(22.5);
	});
});
