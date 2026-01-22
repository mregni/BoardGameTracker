import { z } from 'zod';

const numericIdSchema = z.string().transform((val) => {
  const num = Number.parseInt(val, 10);
  if (Number.isNaN(num)) {
    throw new Error(`Invalid numeric ID: ${val}`);
  }
  return num;
});

export const playerIdParamSchema = z.object({
  playerId: numericIdSchema,
});

export const gameIdParamSchema = z.object({
  gameId: numericIdSchema,
});

export const sessionIdParamSchema = z.object({
  sessionId: numericIdSchema,
});

export function createNumericParamConfig<T extends string>(paramName: T) {
  const schema = z.object({
    [paramName]: numericIdSchema,
  } as Record<T, typeof numericIdSchema>);

  return {
    parse: (params: Record<string, string>) => schema.parse(params),
    stringify: (params: Record<T, number>) => {
      const result: Record<string, string> = {};
      result[paramName] = String(params[paramName]);
      return result as Record<T, string>;
    },
  };
}
