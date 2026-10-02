import { describe, expect, it } from "vitest";
import { CreateGameSchema } from "./CreateGame";
import { GameState } from "./GameState";

const baseGame = {
	title: "Brass: Birmingham",
	additionDate: "2026-01-01",
	state: GameState.Wanted,
	hasScoring: false,
};

describe("CreateGameSchema yearPublished and minAge", () => {
	it("should accept a BCE year such as Go's", () => {
		expect(CreateGameSchema.parse({ ...baseGame, yearPublished: -2200 }).yearPublished).toBe(-2200);
	});

	it("should reject a year outside -5000..9999", () => {
		expect(CreateGameSchema.safeParse({ ...baseGame, yearPublished: -6000 }).success).toBe(false);
	});

	it("should treat a minimum age of 0 as unknown", () => {
		expect(CreateGameSchema.parse({ ...baseGame, minAge: 0 }).minAge).toBeNull();
	});
});

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

describe("CreateGameSchema soldPrice", () => {
	it("should keep a sold price of 0 for a game given away", () => {
		expect(CreateGameSchema.parse({ ...baseGame, soldPrice: 0 }).soldPrice).toBe(0);
	});

	it("should map an absent sold price to null", () => {
		expect(CreateGameSchema.parse({ ...baseGame, soldPrice: undefined }).soldPrice).toBeNull();
	});
});
