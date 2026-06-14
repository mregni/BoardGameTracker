import * as z from "zod";

import { GameState } from "./GameState";

export interface BggSearch {
	bggId: string;
	price: number;
	date: Date;
	state: GameState;
	hasScoring: boolean;
	shopUrl?: string;
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
	shopUrl: z
		.string()
		.trim()
		.refine((value) => value === "" || /^https?:\/\//i.test(value), {
			message: "game:shop-url.invalid",
		})
		.optional(),
});

export const BggUserNameSchema = z.object({
	username: z
		.string({ error: "games:import.start.bgg-username.required" })
		.min(1, { message: "games:import.start.bgg-username.required" }),
});

export type BggUserName = z.infer<typeof BggUserNameSchema>;
