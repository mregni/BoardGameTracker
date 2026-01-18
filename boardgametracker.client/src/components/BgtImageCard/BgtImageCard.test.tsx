import { describe, it, expect, vi } from 'vitest';
import { screen, renderWithTheme } from '@/test/test-utils';

import { BgtImageCard } from './BgtImageCard';
import { GameState } from '@/models';

// i18next is mocked globally in setup.ts

vi.mock('@tanstack/react-router', () => ({
  Link: ({ children, to }: { children: React.ReactNode; to: string }) => <a href={to}>{children}</a>,
}));

describe('BgtImageCard', () => {
  const defaultProps = {
    title: 'Catan',
    image: '/game.jpg',
    link: '/games/1',
  };

  describe('Rendering', () => {
    it('should render title', () => {
      renderWithTheme(<BgtImageCard {...defaultProps} />);
      expect(screen.getByText('Catan')).toBeInTheDocument();
    });

    it('should render as link with correct href', () => {
      renderWithTheme(<BgtImageCard {...defaultProps} />);
      const link = screen.getByRole('link');
      expect(link).toHaveAttribute('href', '/games/1');
    });

    it('should display first letter when no image', () => {
      renderWithTheme(<BgtImageCard {...defaultProps} image={null} />);
      expect(screen.getByText('C')).toBeInTheDocument();
    });
  });

  describe('Image Handling', () => {
    it('should apply background image style when image provided', () => {
      const { container } = renderWithTheme(<BgtImageCard {...defaultProps} />);
      const imageDiv = container.querySelector('[style*="--image-url"]');
      expect(imageDiv).toBeInTheDocument();
    });

    it('should apply fallback color when no image', () => {
      const { container } = renderWithTheme(<BgtImageCard {...defaultProps} image={null} />);
      const imageDiv = container.querySelector('[style*="--fallback-color"]');
      expect(imageDiv).toBeInTheDocument();
    });
  });

  describe('Game State', () => {
    it('should render game state when provided', () => {
      renderWithTheme(<BgtImageCard {...defaultProps} state={GameState.Owned} />);
      expect(screen.getByText('game.state.owned')).toBeInTheDocument();
    });

    it('should not render state when null', () => {
      renderWithTheme(<BgtImageCard {...defaultProps} state={null as unknown as GameState} />);
      expect(screen.queryByText('game.state.')).not.toBeInTheDocument();
    });

    it('should not render state when undefined', () => {
      renderWithTheme(<BgtImageCard {...defaultProps} state={undefined} />);
      expect(screen.queryByText('game.state.')).not.toBeInTheDocument();
    });
  });

  describe('Loaned State', () => {
    it('should not be loaned by default', () => {
      renderWithTheme(<BgtImageCard {...defaultProps} state={GameState.Owned} />);
      expect(screen.queryByText('game.state.on-loan')).not.toBeInTheDocument();
    });

    it('should display loaned state when isLoaned is true', () => {
      renderWithTheme(<BgtImageCard {...defaultProps} state={GameState.Owned} isLoaned={true} />);
      expect(screen.getByText('game.state.on-loan')).toBeInTheDocument();
    });
  });

  describe('Combined Props', () => {
    it('should handle all props together', () => {
      renderWithTheme(
        <BgtImageCard title="Wingspan" image="/wingspan.jpg" link="/games/2" state={GameState.Owned} isLoaned={false} />
      );

      expect(screen.getByText('Wingspan')).toBeInTheDocument();
      expect(screen.getByText('game.state.owned')).toBeInTheDocument();
      expect(screen.getByRole('link')).toHaveAttribute('href', '/games/2');
    });
  });
});
