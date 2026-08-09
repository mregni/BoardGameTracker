import { z } from "zod";

import { LANGUAGE_NONE } from "@/utils/languageUtils";
import { GameState } from "./GameState";

export const CreateGameSchema = z.object({
	title: z
		.string({
			error: "game:new.manual.game-title.required",
		})
		.min(1, { message: "game:new.manual.game-title.required" }),
	bggId: z.number().int().optional(),
	buyingPrice: z.coerce
		.number()
		.optional()
		.transform((value) => value || null),
	additionDate: z.coerce.date({
		error: "game:added-date.required",
	}),
	state: z.nativeEnum(GameState),
	yearPublished: z.coerce
		.number()
		.int()
		.optional()
		.transform((value) => value || null),
	description: z.string().optional(),
	minPlayers: z.coerce.number().int().optional(),
	maxPlayers: z.coerce.number().int().optional(),
	minPlayTime: z.coerce.number().int().optional(),
	maxPlayTime: z.coerce.number().int().optional(),
	minAge: z.coerce.number().int().optional(),
	image: z.string().nullable().optional(),
	shopUrl: z
		.string()
		.trim()
		.refine((value) => value === "" || /^https?:\/\//i.test(value), {
			message: "game:shop-url.invalid",
		})
		.optional(),
	language: z
		.string()
		.optional()
		.transform((value) => (!value || value === LANGUAGE_NONE ? null : value)),
	hasScoring: z.boolean(),
});

export type CreateGame = z.infer<typeof CreateGameSchema>;
