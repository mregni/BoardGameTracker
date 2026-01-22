import { describe, it, expect, vi } from 'vitest';

import { BgtMostWinnerCard } from './BgtMostWinnerCard';

import { screen, userEvent, renderWithTheme, render } from '@/test/test-utils';

describe('BgtMostWinnerCard', () => {
  const defaultProps = {
    image: '/player.jpg',
    name: 'John Doe',
    value: 42,
    nameHeader: 'Top Player',
    valueHeader: 'Wins',
  };

  describe('Rendering', () => {
    it('should render player name', () => {
      renderWithTheme(<BgtMostWinnerCard {...defaultProps} />);
      expect(screen.getByText('John Doe')).toBeInTheDocument();
    });

    it('should render name header', () => {
      renderWithTheme(<BgtMostWinnerCard {...defaultProps} />);
      expect(screen.getByText('Top Player')).toBeInTheDocument();
    });

    it('should render value header', () => {
      renderWithTheme(<BgtMostWinnerCard {...defaultProps} />);
      expect(screen.getByText('Wins')).toBeInTheDocument();
    });

    it('should render value', () => {
      renderWithTheme(<BgtMostWinnerCard {...defaultProps} />);
      expect(screen.getByText('42')).toBeInTheDocument();
    });

    it('should render avatar with image', () => {
      renderWithTheme(<BgtMostWinnerCard {...defaultProps} />);
      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('src', '/player.jpg');
    });

    it('should render avatar initial when no image', () => {
      renderWithTheme(<BgtMostWinnerCard {...defaultProps} image={null} />);
      expect(screen.getByText('J')).toBeInTheDocument();
    });
  });

  describe('Null Name Handling', () => {
    it('should return null when name is undefined', () => {
      const { container } = render(<BgtMostWinnerCard {...defaultProps} name={undefined} />);
      expect(container.firstChild).toBeNull();
    });

    it('should return null when name is empty string', () => {
      const { container } = render(<BgtMostWinnerCard {...defaultProps} name="" />);
      expect(container.firstChild).toBeNull();
    });
  });

  describe('Click Handler', () => {
    it('should call onClick when avatar is clicked', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      renderWithTheme(<BgtMostWinnerCard {...defaultProps} onClick={handleClick} />);

      await user.click(screen.getByRole('img'));

      expect(handleClick).toHaveBeenCalledTimes(1);
    });
  });

  describe('Value Types', () => {
    it('should handle string value', () => {
      renderWithTheme(<BgtMostWinnerCard {...defaultProps} value="100%" />);
      expect(screen.getByText('100%')).toBeInTheDocument();
    });

    it('should handle number value', () => {
      renderWithTheme(<BgtMostWinnerCard {...defaultProps} value={99} />);
      expect(screen.getByText('99')).toBeInTheDocument();
    });

    it('should handle undefined value', () => {
      renderWithTheme(<BgtMostWinnerCard {...defaultProps} value={undefined} />);
      expect(screen.getByText('John Doe')).toBeInTheDocument();
    });
  });

  describe('Combined Props', () => {
    it('should handle all props together', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();

      renderWithTheme(
        <BgtMostWinnerCard
          image="/avatar.png"
          name="Alice"
          value={75}
          nameHeader="Champion"
          valueHeader="Score"
          onClick={handleClick}
        />
      );

      expect(screen.getByText('Alice')).toBeInTheDocument();
      expect(screen.getByText('Champion')).toBeInTheDocument();
      expect(screen.getByText('Score')).toBeInTheDocument();
      expect(screen.getByText('75')).toBeInTheDocument();

      await user.click(screen.getByRole('img'));
      expect(handleClick).toHaveBeenCalled();
    });
  });
});
