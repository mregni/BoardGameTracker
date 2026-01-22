import { describe, it, expect, vi } from 'vitest';

import { ErrorFallback } from './ErrorFallback';

import { screen, userEvent, renderWithTheme } from '@/test/test-utils';


// i18next is mocked globally in setup.ts

vi.mock('@/assets/icons/alert-triangle.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="alert-icon" {...props} />,
}));

describe('ErrorFallback', () => {
  const defaultProps = {
    error: new Error('Test error message'),
    resetErrorBoundary: vi.fn(),
  };

  describe('Rendering', () => {
    it('should render error heading', () => {
      renderWithTheme(<ErrorFallback {...defaultProps} />);
      expect(screen.getByText('error.something-went-wrong')).toBeInTheDocument();
    });

    it('should render error description', () => {
      renderWithTheme(<ErrorFallback {...defaultProps} />);
      expect(screen.getByText('error.unexpected-error')).toBeInTheDocument();
    });

    it('should render alert icon', () => {
      renderWithTheme(<ErrorFallback {...defaultProps} />);
      expect(screen.getByTestId('alert-icon')).toBeInTheDocument();
    });

    it('should render try again button', () => {
      renderWithTheme(<ErrorFallback {...defaultProps} />);
      expect(screen.getByText('common.try-again')).toBeInTheDocument();
    });

    it('should render go home button', () => {
      renderWithTheme(<ErrorFallback {...defaultProps} />);
      expect(screen.getByText('common.go-home')).toBeInTheDocument();
    });
  });

  describe('Button Actions', () => {
    it('should call resetErrorBoundary when try again is clicked', async () => {
      const user = userEvent.setup();
      const resetErrorBoundary = vi.fn();
      renderWithTheme(<ErrorFallback {...defaultProps} resetErrorBoundary={resetErrorBoundary} />);

      await user.click(screen.getByText('common.try-again'));

      expect(resetErrorBoundary).toHaveBeenCalledTimes(1);
    });

    it('should navigate to home when go home is clicked', async () => {
      const user = userEvent.setup();
      const originalLocation = window.location;

      // Mock window.location
      Object.defineProperty(window, 'location', {
        value: { href: '' },
        writable: true,
      });

      renderWithTheme(<ErrorFallback {...defaultProps} />);
      await user.click(screen.getByText('common.go-home'));

      expect(window.location.href).toBe('/');

      // Restore
      Object.defineProperty(window, 'location', {
        value: originalLocation,
        writable: true,
      });
    });
  });

  describe('Error with Stack Trace', () => {
    it('should handle error with stack trace', () => {
      const errorWithStack = new Error('Test error');
      errorWithStack.stack = 'Error: Test error\n    at TestComponent';

      renderWithTheme(<ErrorFallback error={errorWithStack} resetErrorBoundary={vi.fn()} />);

      expect(screen.getByText('error.something-went-wrong')).toBeInTheDocument();
    });
  });
});
