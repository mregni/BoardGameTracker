import { describe, it, expect, vi, beforeEach } from 'vitest';

import { RecentActivityCard } from './RecentActivityCard';

import { screen, userEvent, renderWithTheme } from '@/test/test-utils';

const mockNavigate = vi.fn();

// i18next is mocked globally in setup.ts

vi.mock('@tanstack/react-router', () => ({
  useNavigate: () => mockNavigate,
}));

describe('RecentActivityCard', () => {
  interface TestItem {
    id: number;
    name: string;
  }

  const defaultProps = {
    items: [
      { id: 1, name: 'Item 1' },
      { id: 2, name: 'Item 2' },
      { id: 3, name: 'Item 3' },
    ] as TestItem[],
    renderItem: (item: TestItem) => <div data-testid={`item-${item.id}`}>{item.name}</div>,
    title: 'Recent Activity',
    viewAllRoute: '/sessions',
    getKey: (item: TestItem) => item.id,
  };

  beforeEach(() => {
    mockNavigate.mockClear();
  });

  describe('Rendering', () => {
    it('should render title', () => {
      renderWithTheme(<RecentActivityCard {...defaultProps} />);
      expect(screen.getByText('Recent Activity')).toBeInTheDocument();
    });

    it('should render all items', () => {
      renderWithTheme(<RecentActivityCard {...defaultProps} />);
      expect(screen.getByTestId('item-1')).toBeInTheDocument();
      expect(screen.getByTestId('item-2')).toBeInTheDocument();
      expect(screen.getByTestId('item-3')).toBeInTheDocument();
    });

    it('should render item content using renderItem', () => {
      renderWithTheme(<RecentActivityCard {...defaultProps} />);
      expect(screen.getByText('Item 1')).toBeInTheDocument();
      expect(screen.getByText('Item 2')).toBeInTheDocument();
      expect(screen.getByText('Item 3')).toBeInTheDocument();
    });

    it('should render view all button', () => {
      renderWithTheme(<RecentActivityCard {...defaultProps} />);
      expect(screen.getByText('game.sessions')).toBeInTheDocument();
    });

    it('should render custom view all text', () => {
      renderWithTheme(<RecentActivityCard {...defaultProps} viewAllText="See All Activities" />);
      expect(screen.getByText('See All Activities')).toBeInTheDocument();
    });
  });

  describe('Navigation', () => {
    it('should navigate to viewAllRoute when button is clicked', async () => {
      const user = userEvent.setup();
      renderWithTheme(<RecentActivityCard {...defaultProps} />);

      await user.click(screen.getByText('game.sessions'));

      expect(mockNavigate).toHaveBeenCalledWith({ to: '/sessions' });
    });

    it('should navigate to different route', async () => {
      const user = userEvent.setup();
      renderWithTheme(<RecentActivityCard {...defaultProps} viewAllRoute="/games" />);

      await user.click(screen.getByText('game.sessions'));

      expect(mockNavigate).toHaveBeenCalledWith({ to: '/games' });
    });
  });

  describe('Empty Items', () => {
    it('should render with empty items array', () => {
      renderWithTheme(<RecentActivityCard {...defaultProps} items={[]} />);
      expect(screen.getByText('Recent Activity')).toBeInTheDocument();
      expect(screen.getByText('game.sessions')).toBeInTheDocument();
    });
  });

  describe('Icon', () => {
    it('should render icon when provided', () => {
      const TestIcon = (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="activity-icon" {...props} />;
      renderWithTheme(<RecentActivityCard {...defaultProps} icon={TestIcon} />);
      expect(screen.getByTestId('activity-icon')).toBeInTheDocument();
    });

    it('should not render icon when not provided', () => {
      renderWithTheme(<RecentActivityCard {...defaultProps} />);
      expect(screen.queryByTestId('activity-icon')).not.toBeInTheDocument();
    });
  });

  describe('Key Generation', () => {
    it('should use getKey for item keys', () => {
      const getKey = vi.fn((item: TestItem) => item.id);
      renderWithTheme(<RecentActivityCard {...defaultProps} getKey={getKey} />);

      expect(getKey).toHaveBeenCalledTimes(3);
      expect(getKey).toHaveBeenCalledWith(defaultProps.items[0]);
      expect(getKey).toHaveBeenCalledWith(defaultProps.items[1]);
      expect(getKey).toHaveBeenCalledWith(defaultProps.items[2]);
    });

    it('should handle string keys', () => {
      const items = [
        { id: 'a', name: 'String A' },
        { id: 'b', name: 'String B' },
      ];
      renderWithTheme(<RecentActivityCard {...defaultProps} items={items} getKey={(item) => item.id} />);

      expect(screen.getByText('String A')).toBeInTheDocument();
      expect(screen.getByText('String B')).toBeInTheDocument();
    });
  });

  describe('Combined Props', () => {
    it('should handle all props together', async () => {
      const user = userEvent.setup();
      const TestIcon = (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="custom-icon" {...props} />;

      renderWithTheme(
        <RecentActivityCard
          items={[{ id: 1, name: 'Test Item' }]}
          renderItem={(item: TestItem) => <span>{item.name}</span>}
          title="My Activity"
          viewAllRoute="/custom"
          viewAllText="View All"
          icon={TestIcon}
          getKey={(item: TestItem) => item.id}
        />
      );

      expect(screen.getByText('My Activity')).toBeInTheDocument();
      expect(screen.getByText('Test Item')).toBeInTheDocument();
      expect(screen.getByText('View All')).toBeInTheDocument();
      expect(screen.getByTestId('custom-icon')).toBeInTheDocument();

      await user.click(screen.getByText('View All'));
      expect(mockNavigate).toHaveBeenCalledWith({ to: '/custom' });
    });
  });
});
