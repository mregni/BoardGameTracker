import { describe, it, expect, vi } from 'vitest';
import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { useMenuInfo, menuItems } from './useMenuInfo';

import { renderHook, waitFor } from '@/test/test-utils';

// Mock the query functions
vi.mock('@/services/queries/settings', () => ({
  getVersionInfo: () => ({
    queryKey: ['version-info'],
    queryFn: () => Promise.resolve({ currentVersion: '1.0.0', latestVersion: '1.0.0', updateAvailable: false }),
  }),
}));

vi.mock('@/services/queries/count', () => ({
  getCounts: () => ({
    queryKey: ['counts'],
    queryFn: () => Promise.resolve({ games: 10, players: 5, sessions: 20 }),
  }),
}));

// Mock SVG icons
vi.mock('@/assets/icons/users.svg?react', () => ({
  default: () => <svg data-testid="users-icon" />,
}));

vi.mock('@/assets/icons/trend-up.svg?react', () => ({
  default: () => <svg data-testid="trend-up-icon" />,
}));

vi.mock('@/assets/icons/puzzle-piece.svg?react', () => ({
  default: () => <svg data-testid="puzzle-piece-icon" />,
}));

vi.mock('@/assets/icons/plus.svg?react', () => ({
  default: () => <svg data-testid="plus-icon" />,
}));

vi.mock('@/assets/icons/map-pin.svg?react', () => ({
  default: () => <svg data-testid="map-pin-icon" />,
}));

vi.mock('@/assets/icons/left-right-arrow.svg?react', () => ({
  default: () => <svg data-testid="left-right-arrow-icon" />,
}));

vi.mock('@/assets/icons/home.svg?react', () => ({
  default: () => <svg data-testid="home-icon" />,
}));

vi.mock('@/assets/icons/cog.svg?react', () => ({
  default: () => <svg data-testid="cog-icon" />,
}));

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });

  const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  Wrapper.displayName = 'QueryClientProviderWrapper';

  return Wrapper;
};

describe('useBgtMenuBar', () => {
  describe('menuItems', () => {
    it('should have 8 menu items', () => {
      expect(menuItems).toHaveLength(8);
    });

    it('should have dashboard as first item', () => {
      expect(menuItems[0].menuLabel).toBe('common.dashboard');
      expect(menuItems[0].path).toBe('/');
    });

    it('should have new session item', () => {
      const newSession = menuItems.find((item) => item.path === '/sessions/new');
      expect(newSession).toBeDefined();
      expect(newSession?.menuLabel).toBe('common.new-session');
    });

    it('should have games item', () => {
      const games = menuItems.find((item) => item.path === '/games');
      expect(games).toBeDefined();
      expect(games?.menuLabel).toBe('common.games');
    });

    it('should have players item', () => {
      const players = menuItems.find((item) => item.path === '/players');
      expect(players).toBeDefined();
      expect(players?.menuLabel).toBe('common.players');
    });

    it('should have compare item', () => {
      const compare = menuItems.find((item) => item.path === '/compare');
      expect(compare).toBeDefined();
      expect(compare?.menuLabel).toBe('common.compare');
    });

    it('should have loans item', () => {
      const loans = menuItems.find((item) => item.path === '/loans');
      expect(loans).toBeDefined();
      expect(loans?.menuLabel).toBe('common.loans');
    });

    it('should have locations item', () => {
      const locations = menuItems.find((item) => item.path === '/locations');
      expect(locations).toBeDefined();
      expect(locations?.menuLabel).toBe('common.locations');
    });

    it('should have settings as last item', () => {
      expect(menuItems[menuItems.length - 1].menuLabel).toBe('common.settings');
      expect(menuItems[menuItems.length - 1].path).toBe('/settings');
    });

    it('should have icons for all items', () => {
      menuItems.forEach((item) => {
        expect(item.icon).toBeDefined();
      });
    });
  });

  describe('useBgtMenuBar hook', () => {
    it('should return menuItems', () => {
      const { result } = renderHook(() => useMenuInfo(), {
        wrapper: createWrapper(),
      });

      expect(result.current.menuItems).toBe(menuItems);
    });

    it('should return versionInfo data when loaded', async () => {
      const { result } = renderHook(() => useMenuInfo(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.versionInfo).toBeDefined();
      });

      expect(result.current.versionInfo).toEqual({
        currentVersion: '1.0.0',
        latestVersion: '1.0.0',
        updateAvailable: false,
      });
    });

    it('should return counts data when loaded', async () => {
      const { result } = renderHook(() => useMenuInfo(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.counts).toBeDefined();
      });

      expect(result.current.counts).toEqual({
        games: 10,
        players: 5,
        sessions: 20,
      });
    });
  });
});
