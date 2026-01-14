import { queryOptions } from '@tanstack/react-query';

export const createSingletonQuery =
  <T>(key: string, fetchFn: () => Promise<T>) =>
  () =>
    queryOptions({
      queryKey: [key],
      queryFn: fetchFn,
    });

export const createListQuery =
  <T>(key: string, fetchFn: () => Promise<T[]>) =>
  () =>
    queryOptions({
      queryKey: [key],
      queryFn: fetchFn,
    });

export const createEntityQuery =
  <T>(key: string, fetchFn: (id: number) => Promise<T>) =>
  (id: number) =>
    queryOptions({
      queryKey: [key, id],
      queryFn: () => fetchFn(id),
    });

export const createNestedQuery =
  <T, TParams = unknown>(parentKey: string, childKey: string, fetchFn: (id: number, params?: TParams) => Promise<T>) =>
  (id: number, params?: TParams) =>
    queryOptions({
      queryKey: [parentKey, id, childKey],
      queryFn: () => fetchFn(id, params),
    });

export const createNestedQueryWithKeys =
  <T, TParams = unknown>(
    parentKey: string,
    childKey: string,
    additionalKeys: string[],
    fetchFn: (id: number, params?: TParams) => Promise<T>
  ) =>
  (id: number, params?: TParams) =>
    queryOptions({
      queryKey: [parentKey, id, childKey, ...additionalKeys],
      queryFn: () => fetchFn(id, params),
    });

export const createEntityQueryWithStringId =
  <T>(key: string, fetchFn: (id: string) => Promise<T>) =>
  (id: string) =>
    queryOptions({
      queryKey: [key, id],
      queryFn: () => fetchFn(id),
    });
