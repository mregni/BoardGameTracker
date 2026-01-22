import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';

import { useModalState } from './useModalState';

describe('useModalState', () => {
  describe('initial state', () => {
    it('should default to closed (false)', () => {
      const { result } = renderHook(() => useModalState());
      expect(result.current.isOpen).toBe(false);
    });

    it('should accept initial state of true', () => {
      const { result } = renderHook(() => useModalState(true));
      expect(result.current.isOpen).toBe(true);
    });

    it('should accept initial state of false', () => {
      const { result } = renderHook(() => useModalState(false));
      expect(result.current.isOpen).toBe(false);
    });
  });

  describe('show function', () => {
    it('should set isOpen to true', () => {
      const { result } = renderHook(() => useModalState());

      act(() => {
        result.current.show();
      });

      expect(result.current.isOpen).toBe(true);
    });

    it('should remain true if already open', () => {
      const { result } = renderHook(() => useModalState(true));

      act(() => {
        result.current.show();
      });

      expect(result.current.isOpen).toBe(true);
    });

    it('should be a stable reference', () => {
      const { result, rerender } = renderHook(() => useModalState());
      const showFn = result.current.show;

      rerender();

      expect(result.current.show).toBe(showFn);
    });
  });

  describe('hide function', () => {
    it('should set isOpen to false', () => {
      const { result } = renderHook(() => useModalState(true));

      act(() => {
        result.current.hide();
      });

      expect(result.current.isOpen).toBe(false);
    });

    it('should remain false if already closed', () => {
      const { result } = renderHook(() => useModalState(false));

      act(() => {
        result.current.hide();
      });

      expect(result.current.isOpen).toBe(false);
    });

    it('should be a stable reference', () => {
      const { result, rerender } = renderHook(() => useModalState());
      const hideFn = result.current.hide;

      rerender();

      expect(result.current.hide).toBe(hideFn);
    });
  });

  describe('toggle function', () => {
    it('should toggle from false to true', () => {
      const { result } = renderHook(() => useModalState(false));

      act(() => {
        result.current.toggle();
      });

      expect(result.current.isOpen).toBe(true);
    });

    it('should toggle from true to false', () => {
      const { result } = renderHook(() => useModalState(true));

      act(() => {
        result.current.toggle();
      });

      expect(result.current.isOpen).toBe(false);
    });

    it('should toggle multiple times correctly', () => {
      const { result } = renderHook(() => useModalState(false));

      act(() => {
        result.current.toggle();
      });
      expect(result.current.isOpen).toBe(true);

      act(() => {
        result.current.toggle();
      });
      expect(result.current.isOpen).toBe(false);

      act(() => {
        result.current.toggle();
      });
      expect(result.current.isOpen).toBe(true);
    });

    it('should be a stable reference', () => {
      const { result, rerender } = renderHook(() => useModalState());
      const toggleFn = result.current.toggle;

      rerender();

      expect(result.current.toggle).toBe(toggleFn);
    });
  });

  describe('setIsOpen function', () => {
    it('should set isOpen to true', () => {
      const { result } = renderHook(() => useModalState(false));

      act(() => {
        result.current.setIsOpen(true);
      });

      expect(result.current.isOpen).toBe(true);
    });

    it('should set isOpen to false', () => {
      const { result } = renderHook(() => useModalState(true));

      act(() => {
        result.current.setIsOpen(false);
      });

      expect(result.current.isOpen).toBe(false);
    });

    it('should accept a function updater', () => {
      const { result } = renderHook(() => useModalState(false));

      act(() => {
        result.current.setIsOpen((prev) => !prev);
      });

      expect(result.current.isOpen).toBe(true);
    });
  });

  describe('return value structure', () => {
    it('should return all expected properties', () => {
      const { result } = renderHook(() => useModalState());

      expect(result.current).toHaveProperty('isOpen');
      expect(result.current).toHaveProperty('show');
      expect(result.current).toHaveProperty('hide');
      expect(result.current).toHaveProperty('toggle');
      expect(result.current).toHaveProperty('setIsOpen');
    });

    it('should have correct types for all properties', () => {
      const { result } = renderHook(() => useModalState());

      expect(typeof result.current.isOpen).toBe('boolean');
      expect(typeof result.current.show).toBe('function');
      expect(typeof result.current.hide).toBe('function');
      expect(typeof result.current.toggle).toBe('function');
      expect(typeof result.current.setIsOpen).toBe('function');
    });
  });

  describe('combined usage', () => {
    it('should handle show then hide', () => {
      const { result } = renderHook(() => useModalState());

      act(() => {
        result.current.show();
      });
      expect(result.current.isOpen).toBe(true);

      act(() => {
        result.current.hide();
      });
      expect(result.current.isOpen).toBe(false);
    });

    it('should handle mixed function calls', () => {
      const { result } = renderHook(() => useModalState());

      act(() => {
        result.current.show();
      });
      expect(result.current.isOpen).toBe(true);

      act(() => {
        result.current.toggle();
      });
      expect(result.current.isOpen).toBe(false);

      act(() => {
        result.current.setIsOpen(true);
      });
      expect(result.current.isOpen).toBe(true);

      act(() => {
        result.current.hide();
      });
      expect(result.current.isOpen).toBe(false);
    });
  });
});
