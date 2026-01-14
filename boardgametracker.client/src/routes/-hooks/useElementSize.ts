import { useCallback, useEffect, useRef, useState } from 'react';

interface Size {
  width: number;
  height: number;
}

/**
 * Custom hook to measure element dimensions using ResizeObserver
 * Replacement for react-use's useMeasure to reduce bundle size
 */
export const useElementSize = <T extends HTMLElement = HTMLDivElement>(): [(node: T | null) => void, Size] => {
  const [size, setSize] = useState<Size>({
    width: 0,
    height: 0,
  });

  const observerRef = useRef<ResizeObserver | null>(null);

  const ref = useCallback((node: T | null) => {
    // Cleanup previous observer
    if (observerRef.current) {
      observerRef.current.disconnect();
      observerRef.current = null;
    }

    // Set up new observer if node exists
    if (node) {
      observerRef.current = new ResizeObserver((entries) => {
        const entry = entries[0];
        if (entry) {
          const { width, height } = entry.contentRect;
          setSize({ width, height });
        }
      });

      observerRef.current.observe(node);
    }
  }, []);

  useEffect(() => {
    // Cleanup on unmount
    return () => {
      if (observerRef.current) {
        observerRef.current.disconnect();
      }
    };
  }, []);

  return [ref, size];
};
