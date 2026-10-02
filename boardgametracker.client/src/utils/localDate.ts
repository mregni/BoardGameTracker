import { isValid, parse } from "date-fns";
import { z } from "zod";

const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/;

export const parseLocalDate = (value: unknown): Date | undefined => {
	if (value instanceof Date) {
		return isValid(value) ? value : undefined;
	}

	if (typeof value !== "string" || value.length === 0) {
		return undefined;
	}

	const parsed = DATE_ONLY.test(value) ? parse(value, "yyyy-MM-dd", new Date()) : new Date(value);
	return isValid(parsed) ? parsed : undefined;
};

export const localDateSchema = (message: string) => z.preprocess(parseLocalDate, z.date({ error: message }));
