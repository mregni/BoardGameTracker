import type { AnyFieldApi } from "@tanstack/react-form";
import i18next from "i18next";
import type { z } from "zod";

export function zodValidator<TSchema extends z.ZodObject>(schema: TSchema, name: keyof z.infer<TSchema> & string) {
	const fieldSchema = schema.shape[name] as z.ZodSchema;
	return {
		onChange: ({ value }: { value: unknown }) => {
			const result = fieldSchema.safeParse(value);
			return result.success ? undefined : i18next.t(result.error.issues[0].message);
		},
	};
}

const hasValue = (value: unknown) => value !== undefined && value !== null && value !== "";

export function rangeValidator<TSchema extends z.ZodObject>(
	schema: TSchema,
	name: keyof z.infer<TSchema> & string,
	otherName: keyof z.infer<TSchema> & string,
	role: "min" | "max",
) {
	const fieldSchema = schema.shape[name] as z.ZodSchema;
	return {
		onChangeListenTo: [otherName],
		onChange: ({ value, fieldApi }: { value: unknown; fieldApi: AnyFieldApi }) => {
			const result = fieldSchema.safeParse(value);
			if (!result.success) {
				return i18next.t(result.error.issues[0].message);
			}

			const other: unknown = fieldApi.form.getFieldValue(otherName);
			if (hasValue(value) !== hasValue(other)) {
				return i18next.t("game:validation.range-both");
			}

			if (hasValue(value) && hasValue(other)) {
				const min = Number(role === "min" ? value : other);
				const max = Number(role === "min" ? other : value);
				if (min > max) {
					return i18next.t("game:validation.min-max");
				}
			}

			return undefined;
		},
	};
}
