import { z } from 'zod';

import { GameState } from './GameState';

export const CreateGameSchema = z.object({
  title: z
    .string({
      required_error: 'game.new.manual.game-title.required',
    })
    .min(1, { message: 'game.new.manual.game-title.required' }),
  bggId: z.number().int().optional(),
  buyingPrice: z.coerce.number({
    invalid_type_error: 'game.price.required',
  }),
  date: z.coerce.date({
    errorMap: () => ({
      message: 'game.added-date.required',
    }),
  }),
  state: z.preprocess((value) => Number(value), z.nativeEnum(GameState)),
  yearPublished: z.coerce.number({
    invalid_type_error: 'game.new.manual.year.required',
  }),
  description: z.string().optional(),
  minPlayers: z.coerce.number().int().optional(),
  maxPlayers: z.coerce.number().int().optional(),
  minPlayTime: z.coerce.number().int().optional(),
  maxPlayTime: z.coerce.number().int().optional(),
  minAge: z.coerce.number().int().optional(),
  image: z.string().optional(),
  hasScoring: z.boolean(),
});

export type CreateGame = z.infer<typeof CreateGameSchema>;
