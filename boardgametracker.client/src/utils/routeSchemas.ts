import { z } from "zod";

const numericIdSchema = z.string().transform((val, ctx) => {
	const num = Number.parseInt(val, 10);
	if (Number.isNaN(num) || num <= 0) {
		ctx.addIssue({ code: "custom", message: `Invalid numeric ID: ${val}` });
		return z.NEVER;
	}
	return num;
});

export const playerIdParamSchema = z.object({
	playerId: numericIdSchema,
});

export const gameIdParamSchema = z.object({
	gameId: numericIdSchema,
});

export const sessionIdParamSchema = z.object({
	sessionId: numericIdSchema,
});
