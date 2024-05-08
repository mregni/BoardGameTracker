import { z } from 'zod';

export const CreatePlayPlayerNoScoringSchema = z.object({
  playerId: z.string({ required_error: 'playplayer.new.player.required' }),
  won: z.boolean(),
  firstPlay: z.boolean(),
  isBot: z.boolean(),
});

export const CreatePlayPlayerSchema = CreatePlayPlayerNoScoringSchema.extend({
  score: z
    .number({
      required_error: 'playplayer.new.score.required',
      invalid_type_error: 'playplayer.new.score.required',
    })
    .positive({
      message: 'playplayer.new.score.required',
    }),
});

export const CreatePlaySchema = z.object({
  gameId: z.string({ required_error: 'playplayer.new.game.required' }),
  locationId: z.string(),
  start: z.coerce.date({
    errorMap: () => {
      return { message: 'playplayer.new.start.required' };
    },
  }),
  minutes: z
    .number({
      required_error: 'playplayer.new.duration.required',
      invalid_type_error: 'playplayer.new.duration.required',
    })
    .positive({
      message: 'playplayer.new.duration.required',
    }),
  comment: z.string().nullable(),
  players: CreatePlayPlayerSchema.or(CreatePlayPlayerNoScoringSchema).array().min(1, {
    message: 'playplayer.new.players.minimum',
  }),
});

export type CreatePlay = z.infer<typeof CreatePlaySchema>;
export type CreatePlayPlayer = z.infer<typeof CreatePlayPlayerSchema>;
export type CreatePlayPlayerNoScoring = z.infer<typeof CreatePlayPlayerNoScoringSchema>;
