import { describe, expect, it } from "vitest";
import { localDateSchema, parseLocalDate } from "./localDate";

describe("parseLocalDate", () => {
	it("parses date-only strings as local midnight", () => {
		const result = parseLocalDate("2024-03-10");

		expect(result).toBeDefined();
		expect(result?.getFullYear()).toBe(2024);
		expect(result?.getMonth()).toBe(2);
		expect(result?.getDate()).toBe(10);
		expect(result?.getHours()).toBe(0);
	});

	it("keeps full timestamps", () => {
		const result = parseLocalDate("2024-03-10T20:30:00.000Z");

		expect(result?.toISOString()).toBe("2024-03-10T20:30:00.000Z");
	});

	it("returns Date instances untouched", () => {
		const date = new Date(2024, 2, 10, 15, 0);

		expect(parseLocalDate(date)).toBe(date);
	});

	it("returns undefined for empty or invalid input", () => {
		expect(parseLocalDate("")).toBeUndefined();
		expect(parseLocalDate("not a date")).toBeUndefined();
		expect(parseLocalDate(undefined)).toBeUndefined();
		expect(parseLocalDate(new Date("invalid"))).toBeUndefined();
	});
});

describe("localDateSchema", () => {
	it("accepts date-only strings and yields a local date", () => {
		const result = localDateSchema("required").safeParse("2024-03-10");

		expect(result.success).toBe(true);
		expect(result.data?.getDate()).toBe(10);
	});

	it("reports the given message when the value is missing", () => {
		const result = localDateSchema("required").safeParse("");

		expect(result.success).toBe(false);
		expect(result.error?.issues[0]?.message).toBe("required");
	});
});
