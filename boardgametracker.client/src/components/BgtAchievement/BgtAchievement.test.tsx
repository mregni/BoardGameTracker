import { describe, it, expect, vi } from 'vitest';
import { screen, renderWithTheme } from '@/test/test-utils';

import { BgtAchievement, BgtAchievementIcon } from './BgtAchievement';
import { Badge } from '@/models';

// i18next is mocked globally in setup.ts

vi.mock('@/assets/icons/award.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="award-icon" {...props} />,
}));

describe('BgtAchievement', () => {
  const createBadge = (overrides: Partial<Badge> = {}): Badge => ({
    id: 1,
    titleKey: 'first-win',
    descriptionKey: 'first-win-desc',
    image: 'first-win.png',
    ...overrides,
  });

  describe('Rendering', () => {
    it('should render badge title', () => {
      renderWithTheme(<BgtAchievement badge={createBadge()} />);
      expect(screen.getByText('badges.first-win')).toBeInTheDocument();
    });

    it('should render badge description', () => {
      renderWithTheme(<BgtAchievement badge={createBadge()} />);
      expect(screen.getByText('badges.first-win-desc')).toBeInTheDocument();
    });

    it('should render badge image', () => {
      renderWithTheme(<BgtAchievement badge={createBadge()} />);
      const image = screen.getByRole('img');
      expect(image).toHaveAttribute('src', '/images/badges/first-win.png');
    });
  });

  describe('Earned State', () => {
    it('should show award icon when earned (default)', () => {
      renderWithTheme(<BgtAchievement badge={createBadge()} />);
      expect(screen.getByTestId('award-icon')).toBeInTheDocument();
    });

    it('should show award icon when earned is true', () => {
      renderWithTheme(<BgtAchievement badge={createBadge()} earned={true} />);
      expect(screen.getByTestId('award-icon')).toBeInTheDocument();
    });

    it('should not show award icon when not earned', () => {
      renderWithTheme(<BgtAchievement badge={createBadge()} earned={false} />);
      expect(screen.queryByTestId('award-icon')).not.toBeInTheDocument();
    });
  });
});

describe('BgtAchievementIcon', () => {
  const createBadge = (overrides: Partial<Badge> = {}): Badge => ({
    id: 1,
    titleKey: 'first-win',
    descriptionKey: 'first-win-desc',
    image: 'first-win.png',
    ...overrides,
  });

  describe('Rendering', () => {
    it('should render badge image', () => {
      renderWithTheme(<BgtAchievementIcon badge={createBadge()} />);
      const image = screen.getByRole('img');
      expect(image).toHaveAttribute('src', '/images/badges/first-win.png');
    });
  });
});
