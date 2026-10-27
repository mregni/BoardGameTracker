import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
	isValidDate,
	monthsSince,
	normalizeDateFnsFormat,
	safeParseDate,
	toDisplay,
	toDisplayDateTime,
	toDisplayTime,
	toInputDate,
	toInputDateTime,
	toMonthYear,
	toRelative,
} from "./dateUtils";

describe("dateUtils", () => {
	describe("toInputDate", () => {
		beforeEach(() => {
			vi.useFakeTimers();
			vi.setSystemTime(new Date("2024-06-15T10:30:00"));
		});

		afterEach(() => {
			vi.useRealTimers();
		});

		it("should return today when date is undefined and fallbackToToday is true", () => {
			expect(toInputDate(undefined)).toBe("2024-06-15");
		});

		it("should return empty string when date is undefined and fallbackToToday is false", () => {
			expect(toInputDate(undefined, false)).toBe("");
		});

		it("should format a Date object correctly", () => {
			const date = new Date("2024-03-20T14:30:00");
			expect(toInputDate(date)).toBe("2024-03-20");
		});

		it("should parse and format an ISO string correctly", () => {
			expect(toInputDate("2024-03-20T14:30:00")).toBe("2024-03-20");
		});

		it("should return today for invalid date string with fallback", () => {
			expect(toInputDate("invalid-date")).toBe("2024-06-15");
		});

		it("should return empty string for invalid date without fallback", () => {
			expect(toInputDate("invalid-date", false)).toBe("");
		});

		it("should handle Date object with invalid date", () => {
			const invalidDate = new Date("invalid");
			expect(toInputDate(invalidDate)).toBe("2024-06-15");
			expect(toInputDate(invalidDate, false)).toBe("");
		});
	});

	describe("toInputDateTime", () => {
		beforeEach(() => {
			vi.useFakeTimers();
			vi.setSystemTime(new Date("2024-06-15T10:30:00"));
		});

		afterEach(() => {
			vi.useRealTimers();
		});

		it("should return now when date is undefined and fallbackToNow is true", () => {
			expect(toInputDateTime(undefined)).toBe("2024-06-15T10:30");
		});

		it("should return empty string when date is undefined and fallbackToNow is false", () => {
			expect(toInputDateTime(undefined, false)).toBe("");
		});

		it("should format a Date object correctly", () => {
			const date = new Date("2024-03-20T14:45:00");
			expect(toInputDateTime(date)).toBe("2024-03-20T14:45");
		});

		it("should parse and format an ISO string correctly", () => {
			expect(toInputDateTime("2024-03-20T14:45:00")).toBe("2024-03-20T14:45");
		});

		it("should return now for invalid date string with fallback", () => {
			expect(toInputDateTime("invalid-date")).toBe("2024-06-15T10:30");
		});

		it("should return empty string for invalid date without fallback", () => {
			expect(toInputDateTime("invalid-date", false)).toBe("");
		});
	});

	describe("normalizeDateFnsFormat", () => {
		it("should keep a valid date-fns pattern", () => {
			expect(normalizeDateFnsFormat("dd/MM/yyyy", "yy-MM-dd")).toBe("dd/MM/yyyy");
			expect(normalizeDateFnsFormat("h:mm a", "HH:mm")).toBe("h:mm a");
		});

		it("should convert moment-style year and day tokens that date-fns rejects", () => {
			expect(normalizeDateFnsFormat("MM/DD/YYYY", "yy-MM-dd")).toBe("MM/dd/yyyy");
			expect(normalizeDateFnsFormat("D-M-YY", "yy-MM-dd")).toBe("d-M-yy");
		});

		it("should fall back when the pattern cannot be formatted", () => {
			expect(normalizeDateFnsFormat("not a format", "yy-MM-dd")).toBe("yy-MM-dd");
			expect(normalizeDateFnsFormat("", "HH:mm")).toBe("HH:mm");
			expect(normalizeDateFnsFormat(null, "HH:mm")).toBe("HH:mm");
		});
	});

	describe("toDisplay", () => {
		it("should return empty string for null/undefined date", () => {
			expect(toDisplay(null, "DD/MM/YYYY", "en")).toBe("");
			expect(toDisplay(undefined, "DD/MM/YYYY", "en")).toBe("");
		});

		it("should format date with DD/MM/YYYY format", () => {
			const date = new Date("2024-03-20T14:30:00");
			expect(toDisplay(date, "DD/MM/YYYY", "en")).toBe("20/03/2024");
		});

		it("should format date with MM/DD/YYYY format", () => {
			const date = new Date("2024-03-20T14:30:00");
			expect(toDisplay(date, "MM/DD/YYYY", "en")).toBe("03/20/2024");
		});

		it("should format date with YYYY-MM-DD format", () => {
			const date = new Date("2024-03-20T14:30:00");
			expect(toDisplay(date, "YYYY-MM-DD", "en")).toBe("2024-03-20");
		});

		it("should handle ISO string input", () => {
			expect(toDisplay("2024-03-20T14:30:00", "DD/MM/YYYY", "en")).toBe("20/03/2024");
		});

		it("should return empty string for invalid date", () => {
			expect(toDisplay("invalid-date", "DD/MM/YYYY", "en")).toBe("");
		});

		it("should fall back to the default date format when the format is invalid", () => {
			const date = new Date("2024-03-20T14:30:00");
			expect(toDisplay(date, "DD/MM/YYYY QQQQQQ A", "en")).toBe("20-03-2024");
			expect(toDisplay(date, "", "en")).toBe("20-03-2024");
		});
	});

	describe("toDisplayDateTime", () => {
		it("should return empty string for undefined date", () => {
			expect(toDisplayDateTime(undefined, "DD/MM/YYYY", "HH:mm", "en")).toBe("");
		});

		it("should format date and time correctly", () => {
			const date = new Date("2024-03-20T14:45:30");
			expect(toDisplayDateTime(date, "DD/MM/YYYY", "HH:mm", "en")).toBe("20/03/2024 14:45");
		});

		it("should handle 12-hour format", () => {
			const date = new Date("2024-03-20T14:45:30");
			expect(toDisplayDateTime(date, "MM/DD/YYYY", "hh:mm", "en")).toBe("03/20/2024 02:45");
		});

		it("should handle ISO string input", () => {
			expect(toDisplayDateTime("2024-03-20T14:45:00", "DD/MM/YYYY", "HH:mm", "en")).toBe("20/03/2024 14:45");
		});

		it("should return empty string for invalid date", () => {
			expect(toDisplayDateTime("invalid-date", "DD/MM/YYYY", "HH:mm", "en")).toBe("");
		});

		it("should fall back to the default formats when the formats are invalid", () => {
			const date = new Date("2024-03-20T14:45:30");
			expect(toDisplayDateTime(date, "DD/MM/YYYY", "h:mm A", "en")).toBe("20/03/2024 14:45");
			expect(toDisplayDateTime(date, "A", "HH:mm", "en")).toBe("20-03-2024 14:45");
		});
	});

	describe("toDisplayTime", () => {
		it("should return empty string for a missing date", () => {
			expect(toDisplayTime(null, "HH:mm", "en")).toBe("");
		});

		it("should format only the time with the user's time format", () => {
			expect(toDisplayTime(new Date("2024-03-20T14:45:30"), "HH:mm", "en")).toBe("14:45");
			expect(toDisplayTime("2024-03-20T14:45:30", "hh:mm", "en")).toBe("02:45");
		});

		it("should fall back to the default time format when the format is invalid", () => {
			expect(toDisplayTime("2024-03-20T14:45:30", "", "en")).toBe("14:45");
			expect(toDisplayTime("2024-03-20T14:45:30", "h:mm A", "en")).toBe("14:45");
		});

		it("should return empty string for an invalid date", () => {
			expect(toDisplayTime("invalid-date", "HH:mm", "en")).toBe("");
		});
	});

	describe("toRelative", () => {
		beforeEach(() => {
			vi.useFakeTimers();
			vi.setSystemTime(new Date("2024-06-15T10:30:00"));
		});

		afterEach(() => {
			vi.useRealTimers();
		});

		it("should return empty string for undefined date", () => {
			expect(toRelative(undefined, "en")).toBe("");
		});

		it("should measure from a given moment, the same way as from now", () => {
			const lastPlay = new Date("2023-05-10T20:00:00");

			expect(toRelative(lastPlay, "en", { now: new Date("2024-06-15T10:30:00") })).toBe(toRelative(lastPlay, "en"));
			expect(toRelative(lastPlay, "en", { now: new Date("2024-06-15T10:30:00") })).toBe("about 1 year ago");
			expect(toRelative(null, "en", { now: new Date() })).toBe("");
		});

		it("should return relative time for a past date", () => {
			const pastDate = new Date("2024-06-14T10:30:00");
			const result = toRelative(pastDate, "en");
			expect(result).toContain("ago");
		});

		it("should handle ISO string input", () => {
			const result = toRelative("2024-06-14T10:30:00", "en");
			expect(result).toContain("ago");
		});

		it("should return empty string for invalid date", () => {
			expect(toRelative("invalid-date", "en")).toBe("");
		});

		it("should prefix future dates with in", () => {
			const result = toRelative(new Date("2025-06-15T10:30:00"), "en");
			expect(result).toBe("in about 1 year");
		});
	});

	describe("isValidDate", () => {
		it("should return false for null/undefined", () => {
			expect(isValidDate(null)).toBe(false);
			expect(isValidDate(undefined)).toBe(false);
		});

		it("should return true for valid Date object", () => {
			expect(isValidDate(new Date("2024-03-20"))).toBe(true);
		});

		it("should return false for invalid Date object", () => {
			expect(isValidDate(new Date("invalid"))).toBe(false);
		});

		it("should return true for valid ISO string", () => {
			expect(isValidDate("2024-03-20T14:30:00")).toBe(true);
		});

		it("should return false for invalid string", () => {
			expect(isValidDate("invalid-date")).toBe(false);
		});

		it("should return false for non-date types", () => {
			expect(isValidDate(123)).toBe(false);
			expect(isValidDate({})).toBe(false);
			expect(isValidDate([])).toBe(false);
		});

		it("should return false for empty string", () => {
			expect(isValidDate("")).toBe(false);
		});
	});

	describe("safeParseDate", () => {
		it("should return undefined for null/undefined", () => {
			expect(safeParseDate(undefined)).toBeUndefined();
		});

		it("should return Date object as-is if valid", () => {
			const date = new Date("2024-03-20");
			expect(safeParseDate(date)).toBe(date);
		});

		it("should return undefined for invalid Date object", () => {
			expect(safeParseDate(new Date("invalid"))).toBeUndefined();
		});

		it("should parse valid ISO string to Date", () => {
			const result = safeParseDate("2024-03-20T14:30:00");
			expect(result).toBeInstanceOf(Date);
			expect(result?.getFullYear()).toBe(2024);
			expect(result?.getMonth()).toBe(2); // March is 2 (0-indexed)
			expect(result?.getDate()).toBe(20);
		});

		it("should return undefined for invalid string", () => {
			expect(safeParseDate("invalid-date")).toBeUndefined();
		});
	});

	describe("toMonthYear", () => {
		it("shows the short month and the year", () => {
			expect(toMonthYear(new Date(2023, 7, 14), "en-US")).toBe("Aug 2023");
		});

		it("accepts an ISO string", () => {
			expect(toMonthYear("2023-08-14T10:00:00Z", "en-US")).toBe("Aug 2023");
		});

		it("returns an empty string for a missing or invalid date", () => {
			expect(toMonthYear(null, "en-US")).toBe("");
			expect(toMonthYear(new Date("nope"), "en-US")).toBe("");
		});
	});

	describe("monthsSince", () => {
		const now = new Date(2026, 9, 16);

		it("counts whole months", () => {
			expect(monthsSince(new Date(2025, 2, 10), now)).toBe(19);
			expect(monthsSince(new Date(2026, 8, 20), now)).toBe(0);
		});

		it("never goes below zero", () => {
			expect(monthsSince(new Date(2027, 0, 1), now)).toBe(0);
		});

		it("returns null without a date", () => {
			expect(monthsSince(null, now)).toBeNull();
		});
	});
});
