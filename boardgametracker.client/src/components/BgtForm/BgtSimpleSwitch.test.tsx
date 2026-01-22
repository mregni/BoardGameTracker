import { describe, it, expect, vi } from 'vitest';

import { BgtSimpleSwitch } from './BgtSimpleSwitch';

import { screen, userEvent, renderWithTheme } from '@/test/test-utils';


describe('BgtSimpleSwitch', () => {
  const defaultProps = {
    label: 'Test Switch',
    value: false,
    onChange: vi.fn(),
  };

  describe('Rendering', () => {
    it('should render the label', () => {
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} />);
      expect(screen.getByText('Test Switch')).toBeInTheDocument();
    });

    it('should render switch element', () => {
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} />);
      const switchElement = screen.getByRole('switch');
      expect(switchElement).toBeInTheDocument();
    });
  });

  describe('Value State', () => {
    it('should render unchecked when value is false', () => {
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} value={false} />);
      const switchElement = screen.getByRole('switch');
      expect(switchElement).not.toBeChecked();
    });

    it('should render checked when value is true', () => {
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} value={true} />);
      const switchElement = screen.getByRole('switch');
      expect(switchElement).toBeChecked();
    });
  });

  describe('onChange Handler', () => {
    it('should call onChange when clicked', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} onChange={handleChange} />);

      await user.click(screen.getByRole('switch'));

      expect(handleChange).toHaveBeenCalledTimes(1);
      expect(handleChange).toHaveBeenCalledWith(true);
    });

    it('should call onChange with false when toggling off', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} value={true} onChange={handleChange} />);

      await user.click(screen.getByRole('switch'));

      expect(handleChange).toHaveBeenCalledWith(false);
    });
  });

  describe('Disabled State', () => {
    it('should not be disabled by default', () => {
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} />);
      const switchElement = screen.getByRole('switch');
      expect(switchElement).not.toBeDisabled();
    });

    it('should be disabled when disabled prop is true', () => {
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} disabled={true} />);
      const switchElement = screen.getByRole('switch');
      expect(switchElement).toBeDisabled();
    });

    it('should not call onChange when disabled', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} disabled={true} onChange={handleChange} />);

      await user.click(screen.getByRole('switch'));

      expect(handleChange).not.toHaveBeenCalled();
    });
  });

  describe('Accessibility', () => {
    it('should be focusable', () => {
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} />);
      const switchElement = screen.getByRole('switch');
      switchElement.focus();
      expect(switchElement).toHaveFocus();
    });

    it('should toggle with keyboard', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      renderWithTheme(<BgtSimpleSwitch {...defaultProps} onChange={handleChange} />);

      const switchElement = screen.getByRole('switch');
      switchElement.focus();
      await user.keyboard(' ');

      expect(handleChange).toHaveBeenCalledWith(true);
    });
  });
});
