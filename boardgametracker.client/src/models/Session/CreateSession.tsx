import { z } from 'zod';

export const CreatePlayerSessionNoScoringSchema = z.object({
  playerId: z.string({ required_error: 'player-session.new.player.required' }),
  won: z.boolean(),
  firstPlay: z.boolean(),
});

export const CreatePlayerSessionSchema = CreatePlayerSessionNoScoringSchema.extend({
  score: z
    .number({
      required_error: 'player-session.score.required',
      invalid_type_error: 'player-session.score.required',
    })
    .positive({
      message: 'player-session.score.required',
    }),
});

export const CreateSessionSchema = z.object({
  gameId: z.string({ required_error: 'player-session.new.game.required' }),
  locationId: z.string({
    required_error: 'player-session.new.location.required',
    invalid_type_error: 'player-session.new.location.required',
  }),
  start: z.coerce.date({
    errorMap: () => {
      return { message: 'player-session.new.start.required' };
    },
  }),
  minutes: z
    .number({
      required_error: 'player-session.new.duration.required',
      invalid_type_error: 'player-session.new.duration.required',
    })
    .positive({
      message: 'player-session.new.duration.required',
    }),
  comment: z.string().nullable(),
  playerSessions: CreatePlayerSessionSchema.or(CreatePlayerSessionNoScoringSchema).array().min(1, {
    message: 'player-session.new.players.minimum',
  }),
});

export type CreateSession = z.infer<typeof CreateSessionSchema>;
export type CreateSessionPlayer = z.infer<typeof CreatePlayerSessionSchema>;
export type CreatePlayerSessionNoScoring = z.infer<typeof CreatePlayerSessionNoScoringSchema>;
