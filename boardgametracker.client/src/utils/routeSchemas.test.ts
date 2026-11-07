import { describe, expect, it } from "vitest";
import { playerIdParamSchema } from "./routeSchemas";

const idOf = (playerId: string) => playerIdParamSchema.parse({ playerId }).playerId;

describe("route id schema", () => {
	it("reads a positive whole number", () => {
		expect(idOf("12")).toBe(12);
	});

	it.each(["abc", "0", "-1", "1abc", "1.5", "99999999999"])("maps %s to an id that never exists", (value) => {
		expect(idOf(value)).toBe(2_147_483_647);
	});
});
