import { z } from 'zod';

export const CreateLocationSchema = z.object({
  name: z
    .string({
      required_error: 'location.new.name.required',
    })
    .min(1, { message: 'location.new.name.required' }),
});

export type CreateLocation = z.infer<typeof CreateLocationSchema>;
