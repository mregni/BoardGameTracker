import * as z from 'zod';

import {GameState} from './GameState';

export interface BggSearch {
  bggId: string;
  price: number;
  date: Date;
  state: GameState;
  hasScoring: boolean;
}

export const BggSearchSchema = z.object({
  bggId: z.string().min(1, { message: 'game.bgg.required' }),
  price: z.coerce.number(),
  date: z.coerce.date({
    errorMap: () => ({
      message: 'game.added-date.required'
    })
  }),
  state: z.preprocess((value) => Number(value), z.nativeEnum(GameState)),
  hasScoring: z.boolean()
});