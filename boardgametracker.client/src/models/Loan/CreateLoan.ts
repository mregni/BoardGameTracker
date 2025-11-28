import z from 'zod';

export const CreateLoanSchema = z.object({
  gameId: z.string({ required_error: 'player-session.new.game.required' }),
  playerId: z.string({
    required_error: 'player-session.new.player.required',
    invalid_type_error: 'player-session.new.player.required',
  }),
  loanDate: z.coerce.date({
    errorMap: () => {
      return { message: 'loan.new.start.required' };
    },
  }),
  returnDate: z
    .string()
    .optional()
    .transform((val) => (val === '' || val === undefined ? null : new Date(val)))
    .nullable(),
});

export type CreateLoan = z.infer<typeof CreateLoanSchema>;
