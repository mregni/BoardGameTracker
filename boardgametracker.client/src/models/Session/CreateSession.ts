import { z } from "zod";

export const CreatePlayerSessionNoScoringSchema = z.object({
	playerId: z.coerce
		.number({
			error: "player-session:new.player.required",
		})
		.int()
		.positive({ message: "player-session:new.player.required" }),
	won: z.boolean(),
	firstPlay: z.boolean(),
});

export const CreatePlayerSessionSchema = CreatePlayerSessionNoScoringSchema.extend({
	score: z
		.number({
			error: "player-session:score.required",
		})
		.nullable()
		.optional()
		.transform((value) => value ?? null),
});

const START_TOLERANCE_MS = 60_000;

export const CreateSessionSchema = z.object({
	gameId: z.coerce
		.number({ error: "player-session:new.game.required" })
		.int()
		.positive({ message: "player-session:new.game.required" }),
	locationId: z.coerce
		.number({
			error: "player-session:new.location.required",
		})
		.int()
		.nonnegative({
			message: "player-session:new.location.required",
		})
		.transform((value) => (value > 0 ? value : null)),
	start: z.coerce
		.date({
			error: "player-session:new.start.required",
		})
		.refine((value) => value.getTime() <= Date.now() + START_TOLERANCE_MS, {
			message: "player-session:new.start.future",
		}),
	minutes: z
		.number({
			error: "player-session:new.duration.required",
		})
		.positive({
			message: "player-session:new.duration.positive",
		}),
	comment: z.string().nullable(),
	playerSessions: CreatePlayerSessionSchema.or(CreatePlayerSessionNoScoringSchema).array().min(1, {
		message: "player-session:new.players.minimum",
	}),
	expansionIds: z.array(z.number()).optional(),
});

export type CreateSession = z.infer<typeof CreateSessionSchema>;
export type CreateSessionPlayer = z.infer<typeof CreatePlayerSessionSchema>;
export type CreatePlayerSessionNoScoring = z.infer<typeof CreatePlayerSessionNoScoringSchema>;
