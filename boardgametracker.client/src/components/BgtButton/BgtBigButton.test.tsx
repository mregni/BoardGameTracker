import { describe, it, expect, vi } from 'vitest';
import { screen, userEvent, renderWithTheme } from '@/test/test-utils';

import BgtBigButton from './BgtBigButton';

describe('BgtBigButton', () => {
  const defaultProps = {
    title: 'Add Game',
    subText: 'Click to add a new game to your collection',
    onClick: vi.fn(),
  };

  describe('Rendering', () => {
    it('should render title', () => {
      renderWithTheme(<BgtBigButton {...defaultProps} />);
      expect(screen.getByText('Add Game')).toBeInTheDocument();
    });

    it('should render subText', () => {
      renderWithTheme(<BgtBigButton {...defaultProps} />);
      expect(screen.getByText('Click to add a new game to your collection')).toBeInTheDocument();
    });

    it('should render as button', () => {
      renderWithTheme(<BgtBigButton {...defaultProps} />);
      expect(screen.getByRole('button')).toBeInTheDocument();
    });
  });

  describe('onClick Handler', () => {
    it('should call onClick when clicked', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      renderWithTheme(<BgtBigButton {...defaultProps} onClick={handleClick} />);

      await user.click(screen.getByRole('button'));

      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('should not call onClick when disabled', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      renderWithTheme(<BgtBigButton {...defaultProps} onClick={handleClick} disabled={true} />);

      await user.click(screen.getByRole('button'));

      expect(handleClick).not.toHaveBeenCalled();
    });
  });

  describe('Disabled State', () => {
    it('should not be disabled by default', () => {
      renderWithTheme(<BgtBigButton {...defaultProps} />);
      expect(screen.getByRole('button')).not.toBeDisabled();
    });

    it('should be disabled when disabled prop is true', () => {
      renderWithTheme(<BgtBigButton {...defaultProps} disabled={true} />);
      expect(screen.getByRole('button')).toBeDisabled();
    });

  });

  describe('Combined Props', () => {
    it('should handle all props together', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();

      renderWithTheme(
        <BgtBigButton
          title="Start Session"
          subText="Begin tracking a new game session"
          onClick={handleClick}
          disabled={false}
        />
      );

      expect(screen.getByText('Start Session')).toBeInTheDocument();
      expect(screen.getByText('Begin tracking a new game session')).toBeInTheDocument();

      await user.click(screen.getByRole('button'));
      expect(handleClick).toHaveBeenCalled();
    });
  });
});
