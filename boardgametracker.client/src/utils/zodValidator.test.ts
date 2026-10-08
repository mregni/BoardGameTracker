import type { AnyFieldApi } from "@tanstack/react-form";
import { describe, expect, it } from "vitest";
import { z } from "zod";
import { notBeforeValidator, rangeValidator, zodValidator } from "./zodValidator";

const schema = z.object({ start: z.string(), end: z.string().optional() });

const fieldApi = (start: string) => ({ form: { getFieldValue: () => start } }) as unknown as AnyFieldApi;

describe("notBeforeValidator", () => {
	const validator = notBeforeValidator(schema, "end", "start", "end.before-start");

	it("listens to the other field", () => {
		expect(validator.onChangeListenTo).toEqual(["start"]);
	});

	it("rejects a date before the other date", () => {
		expect(validator.onChange({ value: "2026-09-30", fieldApi: fieldApi("2026-10-01") })).toBe("end.before-start");
	});

	it("accepts the same day, a later day and an empty value", () => {
		expect(validator.onChange({ value: "2026-10-01", fieldApi: fieldApi("2026-10-01") })).toBeUndefined();
		expect(validator.onChange({ value: "2026-10-05", fieldApi: fieldApi("2026-10-01") })).toBeUndefined();
		expect(validator.onChange({ value: "", fieldApi: fieldApi("2026-10-01") })).toBeUndefined();
	});
});

describe("notBeforeValidator schema errors", () => {
	it("reports the field's own schema error before comparing dates", () => {
		const strict = z.object({ start: z.string(), end: z.string().min(1, { message: "end.required" }) });
		const validator = notBeforeValidator(strict, "end", "start", "end.before-start");

		expect(validator.onChange({ value: "", fieldApi: fieldApi("2026-10-01") })).toBe("end.required");
	});
});

describe("zodValidator", () => {
	const validator = zodValidator(z.object({ name: z.string().min(1, { message: "name.required" }) }), "name");

	it("returns the translated schema message for an invalid value", () => {
		expect(validator.onChange({ value: "" })).toBe("name.required");
	});

	it("accepts a valid value", () => {
		expect(validator.onChange({ value: "Brass" })).toBeUndefined();
	});
});

describe("rangeValidator", () => {
	const rangeSchema = z.object({
		minPlayers: z.number().positive({ message: "players.positive" }).optional(),
		maxPlayers: z.number().optional(),
	});
	const other = (value: unknown) => ({ form: { getFieldValue: () => value } }) as unknown as AnyFieldApi;
	const min = rangeValidator(rangeSchema, "minPlayers", "maxPlayers", "min");
	const max = rangeValidator(rangeSchema, "maxPlayers", "minPlayers", "max");

	it("listens to the other end of the range", () => {
		expect(min.onChangeListenTo).toEqual(["maxPlayers"]);
	});

	it("reports the schema error first", () => {
		expect(min.onChange({ value: -1, fieldApi: other(4) })).toBe("players.positive");
	});

	it("asks for both ends when only one is filled in", () => {
		expect(min.onChange({ value: 2, fieldApi: other(undefined) })).toBe("game:validation.range-both");
		expect(max.onChange({ value: undefined, fieldApi: other(2) })).toBe("game:validation.range-both");
	});

	it("refuses a minimum above the maximum from either side", () => {
		expect(min.onChange({ value: 5, fieldApi: other(4) })).toBe("game:validation.min-max");
		expect(max.onChange({ value: 4, fieldApi: other(5) })).toBe("game:validation.min-max");
	});

	it("accepts an empty range and a valid range", () => {
		expect(min.onChange({ value: undefined, fieldApi: other(undefined) })).toBeUndefined();
		expect(min.onChange({ value: 2, fieldApi: other(4) })).toBeUndefined();
		expect(max.onChange({ value: 4, fieldApi: other(4) })).toBeUndefined();
	});
});
