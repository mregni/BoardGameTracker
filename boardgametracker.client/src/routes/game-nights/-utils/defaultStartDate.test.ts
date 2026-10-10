import { describe, expect, it } from "vitest";
import { defaultGameNightStart } from "./defaultStartDate";

describe("defaultGameNightStart", () => {
	it("uses today at 19:30 when that is still ahead", () => {
		const result = defaultGameNightStart(new Date(2026, 9, 10, 14, 5));

		expect(result).toEqual(new Date(2026, 9, 10, 19, 30));
	});

	it("moves to tomorrow at 19:30 once tonight's slot has passed", () => {
		const result = defaultGameNightStart(new Date(2026, 9, 10, 19, 30));

		expect(result).toEqual(new Date(2026, 9, 11, 19, 30));
	});
});
