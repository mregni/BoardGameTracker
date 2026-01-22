import { describe, it, expect, vi } from 'vitest';

import { BgtIconButton } from './BgtIconButton';

import { render, screen, userEvent } from '@/test/test-utils';

describe('BgtIconButton', () => {
  const TestIcon = () => <svg data-testid="test-icon" />;

  describe('Rendering', () => {
    it('should render the icon', () => {
      render(<BgtIconButton icon={<TestIcon />} />);
      expect(screen.getByTestId('test-icon')).toBeInTheDocument();
    });

    it('should render as button element', () => {
      render(<BgtIconButton icon={<TestIcon />} />);
      const button = screen.getByRole('button');
      expect(button.tagName).toBe('BUTTON');
    });

    it('should have type button', () => {
      render(<BgtIconButton icon={<TestIcon />} />);
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('type', 'button');
    });

    it('should render text icon', () => {
      render(<BgtIconButton icon={<span>X</span>} />);
      expect(screen.getByText('X')).toBeInTheDocument();
    });
  });

  describe('Disabled State', () => {
    it('should not be disabled by default', () => {
      render(<BgtIconButton icon={<TestIcon />} />);
      const button = screen.getByRole('button');
      expect(button).not.toBeDisabled();
    });

    it('should be disabled when disabled prop is true', () => {
      render(<BgtIconButton icon={<TestIcon />} disabled />);
      const button = screen.getByRole('button');
      expect(button).toBeDisabled();
    });
  });

  describe('Click Handler', () => {
    it('should call onClick when clicked', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      render(<BgtIconButton icon={<TestIcon />} onClick={handleClick} />);

      await user.click(screen.getByRole('button'));

      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('should not call onClick when disabled', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      render(<BgtIconButton icon={<TestIcon />} onClick={handleClick} disabled />);

      await user.click(screen.getByRole('button'));

      expect(handleClick).not.toHaveBeenCalled();
    });

    it('should pass event to onClick handler', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      render(<BgtIconButton icon={<TestIcon />} onClick={handleClick} />);

      await user.click(screen.getByRole('button'));

      expect(handleClick).toHaveBeenCalledWith(expect.any(Object));
    });
  });

  describe('ClassName Prop', () => {
    it('should apply custom className', () => {
      render(<BgtIconButton icon={<TestIcon />} className="custom-class" />);
      const button = screen.getByRole('button');
      expect(button).toHaveClass('custom-class');
    });

    it('should accept multiple custom classes', () => {
      render(<BgtIconButton icon={<TestIcon />} className="class-one class-two" />);
      const button = screen.getByRole('button');
      expect(button).toHaveClass('class-one');
      expect(button).toHaveClass('class-two');
    });
  });

  describe('Additional Props', () => {
    it('should pass through data attributes', () => {
      render(<BgtIconButton icon={<TestIcon />} data-testid="custom-button" />);
      expect(screen.getByTestId('custom-button')).toBeInTheDocument();
    });

    it('should pass through id attribute', () => {
      render(<BgtIconButton icon={<TestIcon />} id="button-id" />);
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('id', 'button-id');
    });

    it('should support aria-label', () => {
      render(<BgtIconButton icon={<TestIcon />} aria-label="Delete item" />);
      const button = screen.getByRole('button', { name: 'Delete item' });
      expect(button).toBeInTheDocument();
    });
  });

  describe('Combined Props', () => {
    it('should handle multiple props together', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      render(
        <BgtIconButton
          icon={<TestIcon />}
          intent="danger"
          size="2"
          className="extra-class"
          onClick={handleClick}
          aria-label="Delete"
        />
      );
      const button = screen.getByRole('button', { name: 'Delete' });
      expect(button).toHaveClass('extra-class');

      await user.click(button);
      expect(handleClick).toHaveBeenCalled();
    });
  });
});
