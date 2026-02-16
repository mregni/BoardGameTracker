import i18next from "i18next";
import type { z } from "zod";

export function zodValidator<TSchema extends z.AnyZodObject>(schema: TSchema, name: keyof z.infer<TSchema> & string) {
	const fieldSchema = schema.shape[name] as z.ZodSchema;
	return {
		onChange: ({ value }: { value: unknown }) => {
			const result = fieldSchema.safeParse(value);
			return result.success ? undefined : i18next.t(result.error.errors[0].message);
		},
	};
}
