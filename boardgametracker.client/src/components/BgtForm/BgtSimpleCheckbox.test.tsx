import { describe, it, expect, vi } from 'vitest';

import { BgtSimpleCheckbox } from './BgtSimpleCheckbox';

import { render, screen, userEvent } from '@/test/test-utils';

vi.mock('@/assets/icons/check.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="check-icon" {...props} />,
}));

describe('BgtSimpleCheckbox', () => {
  const defaultProps = {
    id: 'test-checkbox',
    label: 'Test Label',
    checked: false,
    onCheckedChange: vi.fn(),
  };

  describe('Rendering', () => {
    it('should render the label', () => {
      render(<BgtSimpleCheckbox {...defaultProps} />);
      expect(screen.getByText('Test Label')).toBeInTheDocument();
    });

    it('should render checkbox with correct id', () => {
      render(<BgtSimpleCheckbox {...defaultProps} />);
      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).toHaveAttribute('id', 'test-checkbox');
    });

    it('should associate label with checkbox via htmlFor', () => {
      render(<BgtSimpleCheckbox {...defaultProps} />);
      const label = screen.getByText('Test Label');
      expect(label).toHaveAttribute('for', 'test-checkbox');
    });

    it('should render check icon when checked', () => {
      render(<BgtSimpleCheckbox {...defaultProps} checked={true} />);
      expect(screen.getByTestId('check-icon')).toBeInTheDocument();
    });
  });

  describe('Checked State', () => {
    it('should render unchecked by default', () => {
      render(<BgtSimpleCheckbox {...defaultProps} checked={false} />);
      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).not.toBeChecked();
    });

    it('should render checked when checked prop is true', () => {
      render(<BgtSimpleCheckbox {...defaultProps} checked={true} />);
      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).toBeChecked();
    });
  });

  describe('onChange Handler', () => {
    it('should call onCheckedChange when clicked', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      render(<BgtSimpleCheckbox {...defaultProps} onCheckedChange={handleChange} />);

      await user.click(screen.getByRole('checkbox'));

      expect(handleChange).toHaveBeenCalledTimes(1);
      expect(handleChange).toHaveBeenCalledWith(true);
    });

    it('should call onCheckedChange with false when unchecking', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      render(<BgtSimpleCheckbox {...defaultProps} checked={true} onCheckedChange={handleChange} />);

      await user.click(screen.getByRole('checkbox'));

      expect(handleChange).toHaveBeenCalledWith(false);
    });

    it('should be clickable via label', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      render(<BgtSimpleCheckbox {...defaultProps} onCheckedChange={handleChange} />);

      await user.click(screen.getByText('Test Label'));

      expect(handleChange).toHaveBeenCalledTimes(1);
    });
  });

  describe('Disabled State', () => {
    it('should not be disabled by default', () => {
      render(<BgtSimpleCheckbox {...defaultProps} />);
      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).not.toBeDisabled();
    });

    it('should be disabled when disabled prop is true', () => {
      render(<BgtSimpleCheckbox {...defaultProps} disabled={true} />);
      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).toBeDisabled();
    });

    it('should not call onCheckedChange when disabled', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      render(<BgtSimpleCheckbox {...defaultProps} disabled={true} onCheckedChange={handleChange} />);

      await user.click(screen.getByRole('checkbox'));

      expect(handleChange).not.toHaveBeenCalled();
    });
  });

  describe('Accessibility', () => {
    it('should be focusable', () => {
      render(<BgtSimpleCheckbox {...defaultProps} />);
      const checkbox = screen.getByRole('checkbox');
      checkbox.focus();
      expect(checkbox).toHaveFocus();
    });

    it('should toggle with keyboard', async () => {
      const user = userEvent.setup();
      const handleChange = vi.fn();
      render(<BgtSimpleCheckbox {...defaultProps} onCheckedChange={handleChange} />);

      const checkbox = screen.getByRole('checkbox');
      checkbox.focus();
      await user.keyboard(' ');

      expect(handleChange).toHaveBeenCalledWith(true);
    });
  });
});
