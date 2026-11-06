import { z } from "zod";

const UNKNOWN_ID = 2_147_483_647;

const numericIdSchema = z
	.string()
	.transform((val) => (/^[1-9]\d*$/.test(val) && Number(val) < UNKNOWN_ID ? Number(val) : UNKNOWN_ID));

export const playerIdParamSchema = z.object({
	playerId: numericIdSchema,
});

export const gameIdParamSchema = z.object({
	gameId: numericIdSchema,
});

export const sessionIdParamSchema = z.object({
	sessionId: numericIdSchema,
});

export const locationIdParamSchema = z.object({
	locationId: numericIdSchema,
});

export const gameNightIdParamSchema = z.object({
	gameNightId: numericIdSchema,
});
