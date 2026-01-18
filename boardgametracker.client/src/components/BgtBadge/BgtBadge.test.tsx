import { describe, it, expect, vi } from 'vitest';
import { render, screen, userEvent } from '@/test/test-utils';

import { BgtBadge } from './BgtBadge';

vi.mock('@/assets/icons/x.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="close-icon" {...props} />,
}));

describe('BgtBadge', () => {
  describe('Rendering', () => {
    it('should render children text content', () => {
      render(<BgtBadge>Badge Text</BgtBadge>);
      expect(screen.getByText('Badge Text')).toBeInTheDocument();
    });

    it('should render as div element', () => {
      const { container } = render(<BgtBadge>Badge</BgtBadge>);
      const badge = container.firstChild;
      expect(badge?.nodeName).toBe('DIV');
    });

    it('should render multiple children', () => {
      render(
        <BgtBadge>
          <span>Icon</span>
          <span>Label</span>
        </BgtBadge>
      );
      expect(screen.getByText('Icon')).toBeInTheDocument();
      expect(screen.getByText('Label')).toBeInTheDocument();
    });
  });

  describe('Click Handler', () => {
    it('should call onClick when clicked', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      render(<BgtBadge onClick={handleClick}>Clickable</BgtBadge>);

      await user.click(screen.getByText('Clickable'));

      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('should pass event to onClick handler', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      render(<BgtBadge onClick={handleClick}>Clickable</BgtBadge>);

      await user.click(screen.getByText('Clickable'));

      expect(handleClick).toHaveBeenCalledWith(expect.any(Object));
    });
  });

  describe('Close Button', () => {
    it('should not render close button by default', () => {
      render(<BgtBadge>No Close</BgtBadge>);
      expect(screen.queryByTestId('close-icon')).not.toBeInTheDocument();
    });

    it('should render close button when onClose is provided', () => {
      const handleClose = vi.fn();
      render(<BgtBadge onClose={handleClose}>With Close</BgtBadge>);
      expect(screen.getByTestId('close-icon')).toBeInTheDocument();
    });

    it('should call onClose when close button is clicked', async () => {
      const user = userEvent.setup();
      const handleClose = vi.fn();
      render(<BgtBadge onClose={handleClose}>Closable</BgtBadge>);

      await user.click(screen.getByTestId('close-icon'));

      expect(handleClose).toHaveBeenCalledTimes(1);
    });

    it('should stop propagation when close button is clicked', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      const handleClose = vi.fn();
      render(
        <BgtBadge onClick={handleClick} onClose={handleClose}>
          Both Handlers
        </BgtBadge>
      );

      await user.click(screen.getByTestId('close-icon'));

      expect(handleClose).toHaveBeenCalledTimes(1);
      expect(handleClick).not.toHaveBeenCalled();
    });
  });

  describe('ClassName Prop', () => {
    it('should apply custom className', () => {
      render(<BgtBadge className="custom-class">Badge</BgtBadge>);
      const badge = screen.getByText('Badge');
      expect(badge).toHaveClass('custom-class');
    });

    it('should accept multiple custom classes', () => {
      render(<BgtBadge className="class-one class-two">Badge</BgtBadge>);
      const badge = screen.getByText('Badge');
      expect(badge).toHaveClass('class-one');
      expect(badge).toHaveClass('class-two');
    });
  });

  describe('Additional Props', () => {
    it('should pass through data attributes', () => {
      render(<BgtBadge data-testid="custom-badge">Badge</BgtBadge>);
      expect(screen.getByTestId('custom-badge')).toBeInTheDocument();
    });

    it('should pass through id attribute', () => {
      render(<BgtBadge id="badge-id">Badge</BgtBadge>);
      const badge = screen.getByText('Badge');
      expect(badge).toHaveAttribute('id', 'badge-id');
    });

    it('should support aria attributes', () => {
      render(<BgtBadge aria-label="Status badge">Badge</BgtBadge>);
      const badge = screen.getByText('Badge');
      expect(badge).toHaveAttribute('aria-label', 'Status badge');
    });
  });

  describe('Combined Props', () => {
    it('should handle multiple props together', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      const handleClose = vi.fn();
      render(
        <BgtBadge color="red" className="extra-class" onClick={handleClick} onClose={handleClose}>
          Full Badge
        </BgtBadge>
      );
      const badge = screen.getByText('Full Badge');
      expect(badge).toHaveClass('extra-class');
      expect(screen.getByTestId('close-icon')).toBeInTheDocument();

      await user.click(badge);
      expect(handleClick).toHaveBeenCalled();
    });
  });

  describe('Edge Cases', () => {
    it('should handle empty children', () => {
      render(<BgtBadge data-testid="empty-badge">{''}</BgtBadge>);
      expect(screen.getByTestId('empty-badge')).toBeInTheDocument();
    });

    it('should handle numeric children', () => {
      render(<BgtBadge>{42}</BgtBadge>);
      expect(screen.getByText('42')).toBeInTheDocument();
    });
  });
});
