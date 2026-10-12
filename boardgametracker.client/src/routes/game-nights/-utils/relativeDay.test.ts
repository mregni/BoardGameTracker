import { describe, expect, it } from "vitest";
import { relativeDay } from "./relativeDay";

const now = new Date(2026, 9, 10, 21, 0);

describe("relativeDay", () => {
	it.each([
		[new Date(2026, 9, 10, 23, 0), "today", "vandaag"],
		[new Date(2026, 9, 11, 19, 30), "tomorrow", "morgen"],
		[new Date(2026, 9, 15, 20, 0), "in 5 days", "over 5 dagen"],
		[new Date(2026, 9, 24, 20, 0), "in 2 weeks", "over 2 weken"],
		[new Date(2026, 11, 24, 20, 0), "in 2 months", "over 2 maanden"],
		[new Date(2027, 3, 1, 20, 0), "in 6 months", "over 6 maanden"],
	])("describes %s relative to now", (date, english, dutch) => {
		expect(relativeDay(date, "en-US", now)).toBe(english);
		expect(relativeDay(date, "nl-BE", now)).toBe(dutch);
	});
});
