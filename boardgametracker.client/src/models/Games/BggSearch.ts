import * as z from "zod";

import { localDateSchema } from "@/utils/localDate";
import { GameState } from "./GameState";

export interface BggSearch {
	bggId: string;
	price: number | null;
	additionDate: Date;
	state: GameState;
	hasScoring: boolean;
}

export const BggSearchSchema = z.object({
	bggId: z
		.string({
			error: "game:bgg.required",
		})
		.min(1, { message: "game:bgg.required" }),
	price: z
		.number({ error: "game:validation.positive-number" })
		.nonnegative({ message: "game:validation.positive-number" })
		.nullable()
		.optional()
		.transform((value) => value || null),
	additionDate: localDateSchema("game:added-date.required"),
	state: z.nativeEnum(GameState),
	hasScoring: z.boolean(),
});

export const BggUserNameSchema = z.object({
	username: z
		.string({ error: "games:import.start.bgg-username.required" })
		.min(1, { message: "games:import.start.bgg-username.required" }),
});
