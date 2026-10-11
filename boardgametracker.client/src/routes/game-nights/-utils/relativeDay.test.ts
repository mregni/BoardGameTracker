import { describe, expect, it } from "vitest";
import { relativeDay } from "./relativeDay";

const now = new Date(2026, 9, 10, 21, 0);

describe("relativeDay", () => {
	it.each([
		[new Date(2026, 9, 10, 23, 0), { key: "today", count: 0 }],
		[new Date(2026, 9, 11, 19, 30), { key: "tomorrow", count: 1 }],
		[new Date(2026, 9, 15, 20, 0), { key: "days", count: 5 }],
		[new Date(2026, 9, 24, 20, 0), { key: "weeks", count: 2 }],
		[new Date(2026, 11, 24, 20, 0), { key: "months", count: 2 }],
		[new Date(2027, 3, 1, 20, 0), { key: "months", count: 6 }],
	])("describes %s relative to now", (date, expected) => {
		expect(relativeDay(date, now)).toEqual(expected);
	});
});
