import { ReactNode } from 'react';

import { BgtLoadingSpinner } from '../BgtLoadingSpinner/BgtLoadingSpinner';

type NonUndefined<T> = T extends undefined ? never : T;
type RequiredData<T> = { [K in keyof T]: NonUndefined<T[K]> };

interface DataGuardProps<T extends Record<string, unknown>> {
  isLoading: boolean;
  data: T;
  children: (data: RequiredData<T>) => ReactNode;
  fallback?: ReactNode;
}

/**
 * A guard component that handles loading states and ensures all data is defined
 * before rendering children. This eliminates the need for verbose undefined checks.
 *
 * @example
 * ```tsx
 * <BgtDataGuard
 *   isLoading={isLoading}
 *   data={{ player, statistics, badges, settings }}
 * >
 *   {({ player, statistics, badges, settings }) => (
 *     <div>
 *       <h1>{player.name}</h1>
 *       <p>{statistics.playCount} plays</p>
 *     </div>
 *   )}
 * </BgtDataGuard>
 * ```
 */
export const BgtDataGuard = <T extends Record<string, unknown>>({
  isLoading,
  data,
  children,
  fallback = <BgtLoadingSpinner />,
}: DataGuardProps<T>) => {
  // Check if loading
  if (isLoading) {
    return <>{fallback}</>;
  }

  // Check if any data property is undefined
  const hasUndefinedData = Object.values(data).some((value) => value === undefined);

  if (hasUndefinedData) {
    return <>{fallback}</>;
  }

  // All data is defined, safe to render children
  // Type assertion is safe here because we've checked all values are defined
  return <>{children(data as RequiredData<T>)}</>;
};
