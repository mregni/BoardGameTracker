import { describe, expect, it } from "vitest";
import { BggSearchSchema } from "./BggSearch";
import { GameState } from "./GameState";

const valid = { bggId: "174430", additionDate: "2024-03-01", state: GameState.Owned, hasScoring: true };

describe("BggSearchSchema", () => {
	it("sends the chosen date as additionDate, the field the API reads", () => {
		const parsed = BggSearchSchema.parse({ ...valid, price: 39.95 });

		expect(parsed).not.toHaveProperty("date");
		expect(parsed.additionDate).toEqual(new Date(2024, 2, 1));
		expect(parsed.price).toBe(39.95);
	});

	it("treats an empty or zero price as unknown", () => {
		expect(BggSearchSchema.parse({ ...valid, price: undefined }).price).toBeNull();
		expect(BggSearchSchema.parse({ ...valid, price: 0 }).price).toBeNull();
	});

	it("rejects a negative price", () => {
		expect(BggSearchSchema.safeParse({ ...valid, price: -1 }).success).toBe(false);
	});
});
