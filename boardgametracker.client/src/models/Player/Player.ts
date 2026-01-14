import z from 'zod';

import { Badge } from '..';

export interface Player {
  id: number;
  name: string;
  image: string | null;
  badges: Badge[];
}

export const CreatePlayerSchema = z.object({
  name: z.string().min(1, { message: 'player.name.required' }),
});

export const UpdatePlayerSchema = CreatePlayerSchema.extend({
  id: z.number(),
});

export type CreatePlayer = z.infer<typeof CreatePlayerSchema>;
export type UpdatePlayer = z.infer<typeof UpdatePlayerSchema>;
