import { describe, it, expect, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useMultiQuery } from './useMultiQuery';

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
    },
  });

const createWrapper = () => {
  const queryClient = createTestQueryClient();
  return ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client: queryClient }, children);
};

describe('useMultiQuery', () => {
  describe('data aggregation', () => {
    it('should aggregate data from multiple queries', async () => {
      const { result } = renderHook(
        () =>
          useMultiQuery([
            { queryKey: ['test1'], queryFn: () => Promise.resolve('result1') },
            { queryKey: ['test2'], queryFn: () => Promise.resolve('result2') },
          ] as const),
        { wrapper: createWrapper() }
      );

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toEqual(['result1', 'result2']);
    });

    it('should handle different data types', async () => {
      const { result } = renderHook(
        () =>
          useMultiQuery([
            { queryKey: ['string'], queryFn: () => Promise.resolve('text') },
            { queryKey: ['number'], queryFn: () => Promise.resolve(42) },
            { queryKey: ['object'], queryFn: () => Promise.resolve({ id: 1 }) },
          ] as const),
        { wrapper: createWrapper() }
      );

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data[0]).toBe('text');
      expect(result.current.data[1]).toBe(42);
      expect(result.current.data[2]).toEqual({ id: 1 });
    });
  });

  describe('loading state', () => {
    it('should be loading initially', () => {
      const { result } = renderHook(
        () =>
          useMultiQuery([
            {
              queryKey: ['slow'],
              queryFn: () => new Promise((resolve) => setTimeout(() => resolve('done'), 100)),
            },
          ] as const),
        { wrapper: createWrapper() }
      );

      expect(result.current.isLoading).toBe(true);
    });

    it('should be loading if any query is loading', async () => {
      const { result } = renderHook(
        () =>
          useMultiQuery([
            { queryKey: ['fast'], queryFn: () => Promise.resolve('fast') },
            {
              queryKey: ['slow'],
              queryFn: () => new Promise((resolve) => setTimeout(() => resolve('slow'), 500)),
            },
          ] as const),
        { wrapper: createWrapper() }
      );

      expect(result.current.isLoading).toBe(true);
    });

    it('should not be loading when all queries complete', async () => {
      const { result } = renderHook(
        () =>
          useMultiQuery([
            { queryKey: ['test1'], queryFn: () => Promise.resolve('result1') },
            { queryKey: ['test2'], queryFn: () => Promise.resolve('result2') },
          ] as const),
        { wrapper: createWrapper() }
      );

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });
    });
  });

  describe('error state', () => {
    it('should set isError when any query fails', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

      const { result } = renderHook(
        () =>
          useMultiQuery([
            { queryKey: ['success'], queryFn: () => Promise.resolve('ok') },
            { queryKey: ['failure'], queryFn: () => Promise.reject(new Error('Failed')) },
          ] as const),
        { wrapper: createWrapper() }
      );

      await waitFor(() => {
        expect(result.current.isError).toBe(true);
      });

      consoleError.mockRestore();
    });

    it('should not set isError when all queries succeed', async () => {
      const { result } = renderHook(
        () =>
          useMultiQuery([
            { queryKey: ['success1'], queryFn: () => Promise.resolve('ok1') },
            { queryKey: ['success2'], queryFn: () => Promise.resolve('ok2') },
          ] as const),
        { wrapper: createWrapper() }
      );

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.isError).toBe(false);
    });

    it('should collect errors in errors array', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
      const testError = new Error('Test error');

      const { result } = renderHook(
        () =>
          useMultiQuery([
            { queryKey: ['success'], queryFn: () => Promise.resolve('ok') },
            { queryKey: ['failure'], queryFn: () => Promise.reject(testError) },
          ] as const),
        { wrapper: createWrapper() }
      );

      await waitFor(() => {
        expect(result.current.errors.length).toBeGreaterThan(0);
      });

      expect(result.current.errors).toContain(testError);

      consoleError.mockRestore();
    });

    it('should have empty errors array when all succeed', async () => {
      const { result } = renderHook(
        () =>
          useMultiQuery([
            { queryKey: ['success1'], queryFn: () => Promise.resolve('ok1') },
            { queryKey: ['success2'], queryFn: () => Promise.resolve('ok2') },
          ] as const),
        { wrapper: createWrapper() }
      );

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.errors).toEqual([]);
    });
  });

  describe('results array', () => {
    it('should expose raw query results', async () => {
      const { result } = renderHook(
        () =>
          useMultiQuery([
            { queryKey: ['test1'], queryFn: () => Promise.resolve('result1') },
            { queryKey: ['test2'], queryFn: () => Promise.resolve('result2') },
          ] as const),
        { wrapper: createWrapper() }
      );

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.results).toHaveLength(2);
      expect(result.current.results[0].data).toBe('result1');
      expect(result.current.results[1].data).toBe('result2');
    });

    it('should include query status in results', async () => {
      const { result } = renderHook(
        () => useMultiQuery([{ queryKey: ['test'], queryFn: () => Promise.resolve('done') }] as const),
        { wrapper: createWrapper() }
      );

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.results[0]).toHaveProperty('status');
      expect(result.current.results[0].status).toBe('success');
    });
  });

  describe('empty queries', () => {
    it('should handle empty queries array', async () => {
      const { result } = renderHook(() => useMultiQuery([] as const), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toEqual([]);
      expect(result.current.isError).toBe(false);
      expect(result.current.errors).toEqual([]);
    });
  });

  describe('single query', () => {
    it('should work with a single query', async () => {
      const { result } = renderHook(
        () => useMultiQuery([{ queryKey: ['single'], queryFn: () => Promise.resolve('single result') }] as const),
        { wrapper: createWrapper() }
      );

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toEqual(['single result']);
    });
  });
});
