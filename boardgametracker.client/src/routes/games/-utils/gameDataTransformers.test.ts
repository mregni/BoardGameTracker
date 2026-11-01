import { describe, expect, it } from "vitest";
import { shortWeekday } from "./gameDataTransformers";

describe("shortWeekday", () => {
	it("abbreviates a weekday in the given language", () => {
		expect(shortWeekday("monday", "en-US")).toBe("Mon");
		expect(shortWeekday("sunday", "en-US")).toBe("Sun");
		expect(shortWeekday("wednesday", "nl-BE")).toBe("wo");
	});

	it("returns null for an unknown day", () => {
		expect(shortWeekday("someday", "en-US")).toBeNull();
	});
});
