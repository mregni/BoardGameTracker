import * as z from "zod";

import { GameState } from "./GameState";

export interface BggSearch {
	bggId: string;
	price: number;
	date: Date;
	state: GameState;
	hasScoring: boolean;
}

export const BggSearchSchema = z.object({
	bggId: z
		.string({
			error: "game:bgg.required",
		})
		.min(1, { message: "game:bgg.required" }),
	price: z.coerce.number({
		error: "game:price.required",
	}),
	date: z.coerce.date({
		error: "game:added-date.required",
	}),
	state: z.nativeEnum(GameState),
	hasScoring: z.boolean(),
});

export const BggUserNameSchema = z.object({
	username: z
		.string({ error: "games:import.start.bgg-username.required" })
		.min(1, { message: "games:import.start.bgg-username.required" }),
});

export type BggUserName = z.infer<typeof BggUserNameSchema>;
