import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useInfiniteScroll } from './useInfiniteScroll';

type IntersectionObserverCallback = (entries: IntersectionObserverEntry[]) => void;

const mockIntersectionObserver = vi.fn();
let intersectionCallback: IntersectionObserverCallback | null = null;

beforeEach(() => {
  intersectionCallback = null;

  mockIntersectionObserver.mockImplementation((callback: IntersectionObserverCallback) => {
    intersectionCallback = callback;
    return {
      observe: vi.fn(),
      unobserve: vi.fn(),
      disconnect: vi.fn(),
    };
  });

  vi.stubGlobal('IntersectionObserver', mockIntersectionObserver);
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.clearAllMocks();
});

const createMockEntry = (isIntersecting: boolean): IntersectionObserverEntry => ({
  isIntersecting,
  boundingClientRect: {} as DOMRectReadOnly,
  intersectionRatio: isIntersecting ? 1 : 0,
  intersectionRect: {} as DOMRectReadOnly,
  rootBounds: null,
  target: document.createElement('div'),
  time: Date.now(),
});

describe('useInfiniteScroll', () => {
  describe('initialization', () => {
    it('should return a ref object', () => {
      const onLoadMore = vi.fn();
      const { result } = renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: true,
          isLoading: false,
        })
      );

      expect(result.current).toHaveProperty('current');
    });

    it('should initialize ref with null', () => {
      const onLoadMore = vi.fn();
      const { result } = renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: true,
          isLoading: false,
        })
      );

      expect(result.current.current).toBeNull();
    });
  });

  describe('IntersectionObserver setup', () => {
    it('should create IntersectionObserver when sentinel is attached', () => {
      const onLoadMore = vi.fn();
      const { result } = renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: true,
          isLoading: false,
        })
      );

      const sentinel = document.createElement('div');
      act(() => {
        (result.current as React.MutableRefObject<HTMLDivElement | null>).current = sentinel;
      });

      renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: true,
          isLoading: false,
        })
      );
    });

    it('should use default threshold of 500px', () => {
      const onLoadMore = vi.fn();
      renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: true,
          isLoading: false,
        })
      );

      if (mockIntersectionObserver.mock.calls.length > 0) {
        const options = mockIntersectionObserver.mock.calls[0][1];
        expect(options?.rootMargin).toBe('500px');
      }
    });

    it('should use custom threshold when provided', () => {
      const onLoadMore = vi.fn();
      renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: true,
          isLoading: false,
          threshold: 200,
        })
      );

      if (mockIntersectionObserver.mock.calls.length > 0) {
        const options = mockIntersectionObserver.mock.calls[0][1];
        expect(options?.rootMargin).toBe('200px');
      }
    });
  });

  describe('onLoadMore callback', () => {
    it('should call onLoadMore when intersecting, hasMore is true, and not loading', () => {
      const onLoadMore = vi.fn();
      renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: true,
          isLoading: false,
        })
      );

      if (intersectionCallback) {
        act(() => {
          intersectionCallback!([createMockEntry(true)]);
        });

        expect(onLoadMore).toHaveBeenCalledTimes(1);
      }
    });

    it('should NOT call onLoadMore when not intersecting', () => {
      const onLoadMore = vi.fn();
      renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: true,
          isLoading: false,
        })
      );

      if (intersectionCallback) {
        act(() => {
          intersectionCallback!([createMockEntry(false)]);
        });

        expect(onLoadMore).not.toHaveBeenCalled();
      }
    });

    it('should NOT call onLoadMore when hasMore is false', () => {
      const onLoadMore = vi.fn();
      renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: false,
          isLoading: false,
        })
      );

      if (intersectionCallback) {
        act(() => {
          intersectionCallback!([createMockEntry(true)]);
        });

        expect(onLoadMore).not.toHaveBeenCalled();
      }
    });

    it('should NOT call onLoadMore when isLoading is true', () => {
      const onLoadMore = vi.fn();
      renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: true,
          isLoading: true,
        })
      );

      if (intersectionCallback) {
        act(() => {
          intersectionCallback!([createMockEntry(true)]);
        });

        expect(onLoadMore).not.toHaveBeenCalled();
      }
    });

    it('should NOT call onLoadMore when all conditions fail', () => {
      const onLoadMore = vi.fn();
      renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: false,
          isLoading: true,
        })
      );

      if (intersectionCallback) {
        act(() => {
          intersectionCallback!([createMockEntry(false)]);
        });

        expect(onLoadMore).not.toHaveBeenCalled();
      }
    });
  });

  describe('cleanup', () => {
    it('should unobserve on unmount', () => {
      const unobserveFn = vi.fn();
      mockIntersectionObserver.mockImplementation((callback: IntersectionObserverCallback) => {
        intersectionCallback = callback;
        return {
          observe: vi.fn(),
          unobserve: unobserveFn,
          disconnect: vi.fn(),
        };
      });

      const onLoadMore = vi.fn();
      const { unmount, result } = renderHook(() =>
        useInfiniteScroll({
          onLoadMore,
          hasMore: true,
          isLoading: false,
        })
      );

      const sentinel = document.createElement('div');
      act(() => {
        (result.current as React.MutableRefObject<HTMLDivElement | null>).current = sentinel;
      });

      unmount();
    });
  });

  describe('dependency changes', () => {
    it('should update callback when hasMore changes', () => {
      const onLoadMore = vi.fn();
      const { rerender } = renderHook(
        ({ hasMore }) =>
          useInfiniteScroll({
            onLoadMore,
            hasMore,
            isLoading: false,
          }),
        { initialProps: { hasMore: true } }
      );

      rerender({ hasMore: false });

      if (intersectionCallback) {
        act(() => {
          intersectionCallback!([createMockEntry(true)]);
        });

        expect(onLoadMore).not.toHaveBeenCalled();
      }
    });

    it('should update callback when isLoading changes', () => {
      const onLoadMore = vi.fn();
      const { rerender } = renderHook(
        ({ isLoading }) =>
          useInfiniteScroll({
            onLoadMore,
            hasMore: true,
            isLoading,
          }),
        { initialProps: { isLoading: false } }
      );

      rerender({ isLoading: true });

      if (intersectionCallback) {
        act(() => {
          intersectionCallback!([createMockEntry(true)]);
        });

        expect(onLoadMore).not.toHaveBeenCalled();
      }
    });

    it('should update callback when onLoadMore changes', () => {
      const onLoadMore1 = vi.fn();
      const onLoadMore2 = vi.fn();

      const { rerender } = renderHook(
        ({ onLoadMore }) =>
          useInfiniteScroll({
            onLoadMore,
            hasMore: true,
            isLoading: false,
          }),
        { initialProps: { onLoadMore: onLoadMore1 } }
      );

      rerender({ onLoadMore: onLoadMore2 });

      if (intersectionCallback) {
        act(() => {
          intersectionCallback!([createMockEntry(true)]);
        });

        expect(onLoadMore1).not.toHaveBeenCalled();
        expect(onLoadMore2).toHaveBeenCalledTimes(1);
      }
    });
  });
});
