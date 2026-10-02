import type { AnyFieldApi } from "@tanstack/react-form";
import { describe, expect, it } from "vitest";
import { z } from "zod";
import { notBeforeValidator } from "./zodValidator";

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
