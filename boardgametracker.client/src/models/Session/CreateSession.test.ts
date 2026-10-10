import { describe, expect, it } from "vitest";
import { CreateSessionSchema } from "./CreateSession";

const firstMessage = (field: "start" | "minutes", value: unknown) => {
	const result = CreateSessionSchema.shape[field].safeParse(value);
	return result.success ? undefined : result.error.issues[0].message;
};

describe("CreateSessionSchema start", () => {
	it("accepts a start in the past", () => {
		expect(firstMessage("start", new Date(Date.now() - 3_600_000))).toBeUndefined();
	});

	it("refuses a start in the future", () => {
		expect(firstMessage("start", new Date(Date.now() + 86_400_000))).toBe("player-session:new.start.future");
	});
});

describe("CreateSessionSchema minutes", () => {
	it("asks for a duration when it is missing", () => {
		expect(firstMessage("minutes", undefined)).toBe("player-session:new.duration.required");
	});

	it("asks for a duration above zero", () => {
		expect(firstMessage("minutes", 0)).toBe("player-session:new.duration.positive");
		expect(firstMessage("minutes", -5)).toBe("player-session:new.duration.positive");
	});
});
