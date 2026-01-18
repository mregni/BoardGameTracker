import { describe, it, expect, vi } from 'vitest';
import { render, screen, userEvent } from '@/test/test-utils';

import { BgtButton } from './BgtButton';

describe('BgtButton', () => {
  describe('Rendering', () => {
    it('should render children text content', () => {
      render(<BgtButton>Click Me</BgtButton>);
      expect(screen.getByRole('button', { name: 'Click Me' })).toBeInTheDocument();
    });

    it('should render as button element', () => {
      render(<BgtButton>Button</BgtButton>);
      const button = screen.getByRole('button');
      expect(button.tagName).toBe('BUTTON');
    });

    it('should render multiple children', () => {
      render(
        <BgtButton>
          <span>Icon</span>
          <span>Text</span>
        </BgtButton>
      );
      expect(screen.getByText('Icon')).toBeInTheDocument();
      expect(screen.getByText('Text')).toBeInTheDocument();
    });
  });

  describe('Disabled State', () => {
    it('should not be disabled by default', () => {
      render(<BgtButton>Enabled</BgtButton>);
      const button = screen.getByRole('button');
      expect(button).not.toBeDisabled();
    });

    it('should be disabled when disabled prop is true', () => {
      render(<BgtButton disabled>Disabled</BgtButton>);
      const button = screen.getByRole('button');
      expect(button).toBeDisabled();
    });
  });

  describe('Type Prop', () => {
    it('should default to type button', () => {
      render(<BgtButton>Button</BgtButton>);
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('type', 'button');
    });

    it('should accept type submit', () => {
      render(<BgtButton type="submit">Submit</BgtButton>);
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('type', 'submit');
    });

    it('should accept type reset', () => {
      render(<BgtButton type="reset">Reset</BgtButton>);
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('type', 'reset');
    });
  });

  describe('Click Handler', () => {
    it('should call onClick when clicked', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      render(<BgtButton onClick={handleClick}>Click Me</BgtButton>);

      await user.click(screen.getByRole('button'));

      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('should not call onClick when disabled', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      render(
        <BgtButton onClick={handleClick} disabled>
          Disabled
        </BgtButton>
      );

      await user.click(screen.getByRole('button'));

      expect(handleClick).not.toHaveBeenCalled();
    });
  });

  describe('ClassName Prop', () => {
    it('should apply custom className', () => {
      render(<BgtButton className="custom-class">Button</BgtButton>);
      const button = screen.getByRole('button');
      expect(button).toHaveClass('custom-class');
    });

    it('should accept multiple custom classes', () => {
      render(<BgtButton className="class-one class-two">Button</BgtButton>);
      const button = screen.getByRole('button');
      expect(button).toHaveClass('class-one');
      expect(button).toHaveClass('class-two');
    });
  });

  describe('Additional Props', () => {
    it('should pass through data attributes', () => {
      render(<BgtButton data-testid="custom-button">Button</BgtButton>);
      expect(screen.getByTestId('custom-button')).toBeInTheDocument();
    });

    it('should pass through id attribute', () => {
      render(<BgtButton id="button-id">Button</BgtButton>);
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('id', 'button-id');
    });

    it('should support aria attributes', () => {
      render(<BgtButton aria-label="Custom Label">Button</BgtButton>);
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('aria-label', 'Custom Label');
    });
  });

  describe('Combined Props', () => {
    it('should handle multiple props together', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      render(
        <BgtButton variant="error" size="3" className="extra-class" type="submit" onClick={handleClick}>
          Submit Error
        </BgtButton>
      );
      const button = screen.getByRole('button');
      expect(button).toHaveClass('extra-class');
      expect(button).toHaveAttribute('type', 'submit');

      await user.click(button);
      expect(handleClick).toHaveBeenCalled();
    });
  });
});
