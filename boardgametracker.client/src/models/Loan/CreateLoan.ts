import { z } from "zod";

import { localDateSchema, parseLocalDate } from "@/utils/localDate";

export const CreateLoanSchema = z.object({
	gameId: z.coerce
		.number({ error: "player-session:new.game.required" })
		.int()
		.refine((val) => val > 0, { message: "player-session:new.game.required" }),
	playerId: z.coerce
		.number({
			error: "player-session:new.player.required",
		})
		.int()
		.refine((val) => val > 0, {
			message: "player-session:new.player.required",
		}),
	loanDate: localDateSchema("loans:new.start.required"),
	dueDate: z
		.string()
		.optional()
		.transform((val) => parseLocalDate(val) ?? null)
		.nullable(),
});

export type CreateLoan = z.infer<typeof CreateLoanSchema>;
