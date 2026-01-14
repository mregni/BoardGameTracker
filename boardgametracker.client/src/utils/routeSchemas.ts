import { z } from 'zod';

/**
 * Reusable Zod schemas for route parameter validation and transformation
 */

/**
 * Schema for numeric ID route parameters
 * Transforms string param to number with validation
 */
export const numericIdSchema = z.string().transform((val) => {
  const num = parseInt(val, 10);
  if (isNaN(num)) {
    throw new Error(`Invalid numeric ID: ${val}`);
  }
  return num;
});

/**
 * Schema for playerId route parameter
 */
export const playerIdParamSchema = z.object({
  playerId: numericIdSchema,
});

/**
 * Schema for gameId route parameter
 */
export const gameIdParamSchema = z.object({
  gameId: numericIdSchema,
});

/**
 * Schema for sessionId route parameter
 */
export const sessionIdParamSchema = z.object({
  sessionId: numericIdSchema,
});

/**
 * Helper function to create param config for TanStack Router
 * Usage:
 * ```ts
 * export const Route = createFileRoute('/games/$gameId')({
 *   params: createNumericParamConfig('gameId'),
 *   ...
 * });
 * ```
 */
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
