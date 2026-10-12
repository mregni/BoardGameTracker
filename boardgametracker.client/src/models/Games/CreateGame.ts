import { z } from "zod";

import { LANGUAGE_NONE } from "@/utils/languageUtils";
import { localDateSchema } from "@/utils/localDate";
import { GameState } from "./GameState";

const positiveInteger = (message: string) =>
	z
		.number({ error: message })
		.int({ message: "game:validation.whole-number" })
		.positive({ message })
		.nullable()
		.optional()
		.transform((value) => value ?? null);

export const CreateGameSchema = z.object({
	title: z
		.string({
			error: "game:new.manual.game-title.required",
		})
		.min(1, { message: "game:new.manual.game-title.required" }),
	bggId: positiveInteger("game:validation.positive-number"),
	buyingPrice: z
		.number({ error: "game:validation.positive-number" })
		.nonnegative({ message: "game:validation.positive-number" })
		.nullable()
		.optional()
		.transform((value) => (value === undefined || Number.isNaN(value) ? null : value)),
	additionDate: localDateSchema("game:added-date.required"),
	state: z.nativeEnum(GameState),
	yearPublished: z
		.number({ error: "game:validation.year" })
		.int({ message: "game:validation.year" })
		.min(-5000, { message: "game:validation.year" })
		.max(new Date().getFullYear() + 1, { message: "game:validation.year" })
		.nullable()
		.optional()
		.transform((value) => value || null),
	description: z.string().optional(),
	minPlayers: positiveInteger("game:validation.positive-number"),
	maxPlayers: positiveInteger("game:validation.positive-number"),
	minPlayTime: positiveInteger("game:validation.positive-number"),
	maxPlayTime: positiveInteger("game:validation.positive-number"),
	minAge: z
		.number({ error: "game:validation.positive-number" })
		.int({ message: "game:validation.positive-number" })
		.nonnegative({ message: "game:validation.positive-number" })
		.max(120, { message: "game:validation.positive-number" })
		.nullable()
		.optional()
		.transform((value) => value || null),
	image: z.string().nullable().optional(),
	changeDetectionWatchId: z
		.string()
		.trim()
		.refine((value) => value === "" || /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value), {
			message: "game:watch-id.invalid",
		})
		.optional()
		.transform((value) => (value ? value : null)),
	language: z
		.string()
		.optional()
		.transform((value) => (!value || value === LANGUAGE_NONE ? null : value)),
	hasScoring: z.boolean(),
	rating: z
		.number({ error: "game:validation.rating" })
		.min(0, { message: "game:validation.rating" })
		.max(10, { message: "game:validation.rating" })
		.nullable()
		.optional()
		.transform((value) => value ?? null),
	weight: z
		.number({ error: "game:validation.weight" })
		.min(0, { message: "game:validation.weight" })
		.max(5, { message: "game:validation.weight" })
		.nullable()
		.optional()
		.transform((value) => value ?? null),
	soldPrice: z
		.number({ error: "game:validation.positive-number" })
		.nonnegative({ message: "game:validation.positive-number" })
		.nullable()
		.optional()
		.transform((value) => value ?? null),
});

export type CreateGame = z.infer<typeof CreateGameSchema>;
