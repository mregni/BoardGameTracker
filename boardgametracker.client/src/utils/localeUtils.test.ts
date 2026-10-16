import { describe, expect, it } from "vitest";
import { getDatePickerLocale, getDateSeparator, getHourCycle, shouldForceLeadingZeros } from "./localeUtils";

describe("getDatePickerLocale", () => {
	it.each([
		["dd-MM-yyyy", "en-US", "en-GB"],
		["yyyy-MM-dd", "en-US", "en-CA"],
		["yy-MM-dd", "en-US", "en-CA"],
		["MM/dd/yyyy", "en-US", "en-US"],
		["dd-MM-yyyy", "nl-BE", "nl-BE"],
		["DD.MM.YYYY", "nl-NL", "nl-NL"],
		["dd/MM/yyyy", "es-ES", "es-ES"],
	])("should pick a locale whose order matches %s for %s", (dateFormat, uiLanguage, expected) => {
		expect(getDatePickerLocale(dateFormat, uiLanguage)).toBe(expected);
	});

	it("should keep the app language when no format is known", () => {
		expect(getDatePickerLocale(null, "nl-BE")).toBe("nl-BE");
	});

	it("should fall back to a locale with the right order when the language has none", () => {
		expect(getDatePickerLocale("yyyy-MM-dd", "nl-BE")).toBe("en-CA");
	});
});

describe("getDateSeparator", () => {
	it.each([
		["dd-MM-yyyy", "-"],
		["yy/MM/dd", "/"],
		["dd.MM.yyyy", "."],
		["MMddyyyy", null],
		[null, null],
	])("should read the separator of %s", (dateFormat, expected) => {
		expect(getDateSeparator(dateFormat)).toBe(expected);
	});
});

describe("shouldForceLeadingZeros", () => {
	it("should force leading zeros for two-digit day or month tokens", () => {
		expect(shouldForceLeadingZeros("dd-MM-yyyy")).toBe(true);
		expect(shouldForceLeadingZeros("d-M-yyyy")).toBe(false);
	});
});

describe("getHourCycle", () => {
	it.each([
		["HH:mm", 24],
		["H:mm", 24],
		["hh:mm a", 12],
		["h:mm a", 12],
		["KK:mm", 12],
		["HH'h'mm", 24],
		[null, 24],
		[undefined, 24],
	])("should derive the hour cycle of %s", (timeFormat, expected) => {
		expect(getHourCycle(timeFormat)).toBe(expected);
	});
});
