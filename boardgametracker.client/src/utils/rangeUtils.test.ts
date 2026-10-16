import { describe, expect, it } from "vitest";
import { formatRange } from "./rangeUtils";

describe("formatRange", () => {
	it("shows both ends of a range", () => {
		expect(formatRange(2, 4)).toBe("2–4");
	});

	it("collapses an equal range to a single value", () => {
		expect(formatRange(45, 45)).toBe("45");
	});

	it("shows the known end when the other is missing", () => {
		expect(formatRange(null, 5)).toBe("5");
		expect(formatRange(3, undefined)).toBe("3");
	});

	it("returns null without any value", () => {
		expect(formatRange(null, null)).toBeNull();
	});
});
