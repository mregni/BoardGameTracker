import { ReactElement } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import { Theme } from '@radix-ui/themes';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

/**
 * Creates a new QueryClient configured for testing
 * - Disables retries to make tests deterministic
 * - Disables garbage collection time to prevent memory issues in tests
 */
const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  });

/**
 * Wrapper component that provides all necessary context providers for testing
 */
const AllTheProviders = ({ children }: { children: React.ReactNode }) => {
  const queryClient = createTestQueryClient();

  return (
    <QueryClientProvider client={queryClient}>
      <Theme>{children}</Theme>
    </QueryClientProvider>
  );
};

/**
 * Simple wrapper with just Theme (no QueryClient)
 * Use this for components that don't need React Query
 */
const ThemeProvider = ({ children }: { children: React.ReactNode }) => {
  return <Theme>{children}</Theme>;
};

/**
 * Custom render function that wraps component with all providers
 * Use this for components that need QueryClient
 */
export const renderWithProviders = (ui: ReactElement, options?: Omit<RenderOptions, 'wrapper'>) => {
  return render(ui, { wrapper: AllTheProviders, ...options });
};

/**
 * Custom render function that wraps component with just Theme
 * Use this for simple components that don't need QueryClient
 */
export const renderWithTheme = (ui: ReactElement, options?: Omit<RenderOptions, 'wrapper'>) => {
  return render(ui, { wrapper: ThemeProvider, ...options });
};

/**
 * Creates a mock field object for TanStack Form field testing
 */
export const createMockField = <T,>(value: T, errors: string[] = []) => ({
  state: {
    value,
    meta: {
      errors,
      isTouched: false,
      isValidating: false,
    },
  },
  handleChange: vi.fn(),
  handleBlur: vi.fn(),
  name: 'testField',
});

/**
 * Re-export everything from @testing-library/react
 */
export * from '@testing-library/react';

/**
 * Re-export userEvent for convenience
 */
export { default as userEvent } from '@testing-library/user-event';

// Make vi available for createMockField
import { vi } from 'vitest';
