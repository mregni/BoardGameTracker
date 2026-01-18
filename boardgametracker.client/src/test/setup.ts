import { afterEach, vi } from 'vitest';
import { cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';

// Global mock for react-i18next - used by most components
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, options?: Record<string, unknown>) => {
      // Handle interpolation like { title: 'Game' }
      if (options && typeof options === 'object') {
        let result = key;
        Object.entries(options).forEach(([k, v]) => {
          result = result.replace(`{{${k}}}`, String(v));
        });
        // For delete modal pattern: common.delete.title with { title: 'X' } -> 'Delete X'
        if (key === 'common.delete.title' && options.title) {
          return `Delete ${options.title}`;
        }
        return result;
      }
      return key;
    },
    i18n: {
      language: 'en',
      changeLanguage: vi.fn(),
    },
  }),
  Trans: ({ children }: { children: React.ReactNode }) => children,
  initReactI18next: { type: '3rdParty', init: vi.fn() },
}));

// Global mock for i18next (direct usage)
vi.mock('i18next', () => ({
  t: (key: string) => key,
  default: {
    t: (key: string) => key,
    use: vi.fn().mockReturnThis(),
    init: vi.fn(),
  },
}));

afterEach(() => {
  cleanup();
});
