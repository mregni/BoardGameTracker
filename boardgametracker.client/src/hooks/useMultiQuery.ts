import { useMemo } from 'react';
import { useQueries, UseQueryOptions } from '@tanstack/react-query';

type QueryOptionsArray<T extends readonly unknown[]> = {
  [K in keyof T]: UseQueryOptions<T[K]>;
};

interface MultiQueryResult<T extends readonly unknown[]> {
  data: T;
  isLoading: boolean;
  isError: boolean;
  errors: unknown[];
  results: ReturnType<typeof useQueries>;
}

export const useMultiQuery = <T extends readonly unknown[]>(
  queries: readonly [...QueryOptionsArray<T>]
): MultiQueryResult<T> => {
  const results = useQueries({ queries: queries as UseQueryOptions<unknown>[] });

  const data = useMemo(() => results.map((r) => r.data) as T, [results]);

  const isLoading = useMemo(() => results.some((r) => r.isLoading), [results]);

  const isError = useMemo(() => results.some((r) => r.isError), [results]);

  const errors = useMemo(() => results.map((r) => r.error).filter(Boolean), [results]);

  return {
    data,
    isLoading,
    isError,
    errors,
    results,
  };
};
