import { describe, it, expect, vi } from 'vitest';

import { screen, renderWithTheme } from '@/test/test-utils';
import { MenuItem } from '@/models';

// i18next is mocked globally in setup.ts

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to, className }: { children: React.ReactNode; to: string; className?: string }) => (
    <a href={to} className={className}>
      {children}
    </a>
  ),
}));

import { BgtMenuItem } from './BgtMenuItem';

const TestIcon = (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="menu-icon" {...props} />;

describe('BgtMenuItem', () => {
  const createMenuItem = (overrides: Partial<MenuItem> = {}): MenuItem => ({
    menuLabel: 'menu.games',
    path: '/games',
    icon: TestIcon,
    ...overrides,
  });

  describe('Rendering', () => {
    it('should render translated menu label', () => {
      renderWithTheme(<BgtMenuItem item={createMenuItem()} count={undefined} />);
      expect(screen.getByText('menu.games')).toBeInTheDocument();
    });

    it('should render icon', () => {
      renderWithTheme(<BgtMenuItem item={createMenuItem()} count={undefined} />);
      expect(screen.getByTestId('menu-icon')).toBeInTheDocument();
    });

    it('should render as link', () => {
      renderWithTheme(<BgtMenuItem item={createMenuItem()} count={undefined} />);
      const link = screen.getByRole('link');
      expect(link).toBeInTheDocument();
    });

    it('should have correct href', () => {
      renderWithTheme(<BgtMenuItem item={createMenuItem()} count={undefined} />);
      const link = screen.getByRole('link');
      expect(link).toHaveAttribute('href', '/games');
    });
  });

  describe('Count Badge', () => {
    it('should show count when greater than zero', () => {
      renderWithTheme(<BgtMenuItem item={createMenuItem()} count={5} />);
      expect(screen.getByText('5')).toBeInTheDocument();
    });

    it('should not show count when zero', () => {
      renderWithTheme(<BgtMenuItem item={createMenuItem()} count={0} />);
      expect(screen.queryByText('0')).not.toBeInTheDocument();
    });

    it('should not show count when undefined', () => {
      renderWithTheme(<BgtMenuItem item={createMenuItem()} count={undefined} />);
      const countElements = screen.queryAllByText(/^\d+$/);
      expect(countElements).toHaveLength(0);
    });

    it('should show large count numbers', () => {
      renderWithTheme(<BgtMenuItem item={createMenuItem()} count={999} />);
      expect(screen.getByText('999')).toBeInTheDocument();
    });
  });

  describe('Combined Props', () => {
    it('should render complete menu item', () => {
      renderWithTheme(<BgtMenuItem item={createMenuItem({ menuLabel: 'menu.games', path: '/games' })} count={42} />);

      expect(screen.getByText('menu.games')).toBeInTheDocument();
      expect(screen.getByText('42')).toBeInTheDocument();
      expect(screen.getByTestId('menu-icon')).toBeInTheDocument();
      expect(screen.getByRole('link')).toHaveAttribute('href', '/games');
    });
  });
});
