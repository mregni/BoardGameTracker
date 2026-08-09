import { z } from "zod";

import type { Badge } from "..";

export interface Player {
	id: number;
	name: string;
	image: string | null;
	email: string | null;
	badges: Badge[];
}

export const CreatePlayerSchema = z.object({
	name: z.string().min(1, { message: "player:name.required" }),
	email: z.string().email({ message: "player:email.invalid" }).or(z.literal("")).optional(),
});

export const UpdatePlayerSchema = CreatePlayerSchema.extend({
	id: z.number(),
});

export type CreatePlayer = z.infer<typeof CreatePlayerSchema>;
export type UpdatePlayer = z.infer<typeof UpdatePlayerSchema>;
