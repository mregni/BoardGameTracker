import { describe, it, expect, vi } from 'vitest';
import { render, screen, userEvent } from '@/test/test-utils';

import { BgtCard } from './BgtCard';

// i18next is mocked globally in setup.ts

describe('BgtCard', () => {
  describe('Rendering', () => {
    it('should render children', () => {
      render(<BgtCard>Card Content</BgtCard>);
      expect(screen.getByText('Card Content')).toBeInTheDocument();
    });

    it('should render as div element', () => {
      const { container } = render(<BgtCard>Content</BgtCard>);
      const card = container.firstChild;
      expect(card?.nodeName).toBe('DIV');
    });

    it('should render multiple children', () => {
      render(
        <BgtCard>
          <p>Paragraph 1</p>
          <p>Paragraph 2</p>
        </BgtCard>
      );
      expect(screen.getByText('Paragraph 1')).toBeInTheDocument();
      expect(screen.getByText('Paragraph 2')).toBeInTheDocument();
    });
  });

  describe('Title Prop', () => {
    it('should render title when provided', () => {
      render(<BgtCard title="Card Title">Content</BgtCard>);
      expect(screen.getByText('Card Title')).toBeInTheDocument();
    });

    it('should not render title section when not provided', () => {
      render(<BgtCard>Content</BgtCard>);
      expect(screen.queryByRole('heading')).not.toBeInTheDocument();
    });

    it('should render title as h2 element', () => {
      render(<BgtCard title="Card Title">Content</BgtCard>);
      const heading = screen.getByRole('heading', { level: 2 });
      expect(heading).toHaveTextContent('Card Title');
    });
  });

  describe('Icon Prop', () => {
    const TestIcon = (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="test-icon" {...props} />;

    it('should render icon when provided with title', () => {
      render(
        <BgtCard title="Card Title" icon={TestIcon}>
          Content
        </BgtCard>
      );
      expect(screen.getByTestId('test-icon')).toBeInTheDocument();
    });

    it('should not render icon without title', () => {
      render(<BgtCard icon={TestIcon}>Content</BgtCard>);
      expect(screen.queryByTestId('test-icon')).not.toBeInTheDocument();
    });
  });

  describe('Actions Prop', () => {
    it('should render action buttons when provided', () => {
      const actions = [{ content: 'action.save', onClick: vi.fn() }];
      render(
        <BgtCard title="Card Title" actions={actions}>
          Content
        </BgtCard>
      );
      expect(screen.getByRole('button', { name: 'action.save' })).toBeInTheDocument();
    });

    it('should render multiple action buttons', () => {
      const actions = [
        { content: 'action.save', onClick: vi.fn() },
        { content: 'action.cancel', onClick: vi.fn(), variant: 'cancel' as const },
      ];
      render(
        <BgtCard title="Card Title" actions={actions}>
          Content
        </BgtCard>
      );
      expect(screen.getByRole('button', { name: 'action.save' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'action.cancel' })).toBeInTheDocument();
    });

    it('should call action onClick when clicked', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      const actions = [{ content: 'action.save', onClick: handleClick }];
      render(
        <BgtCard title="Card Title" actions={actions}>
          Content
        </BgtCard>
      );

      await user.click(screen.getByRole('button', { name: 'action.save' }));

      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('should render ReactNode as action content', () => {
      const actions = [{ content: <span data-testid="custom-content">Custom</span>, onClick: vi.fn() }];
      render(
        <BgtCard title="Card Title" actions={actions}>
          Content
        </BgtCard>
      );
      expect(screen.getByTestId('custom-content')).toBeInTheDocument();
    });

    it('should not render actions without title', () => {
      const actions = [{ content: 'action.save', onClick: vi.fn() }];
      render(<BgtCard actions={actions}>Content</BgtCard>);
      expect(screen.queryByRole('button', { name: 'action.save' })).not.toBeInTheDocument();
    });
  });

  describe('Hide Prop', () => {
    it('should render when hide is false', () => {
      render(<BgtCard hide={false}>Content</BgtCard>);
      expect(screen.getByText('Content')).toBeInTheDocument();
    });

    it('should render when hide is undefined', () => {
      render(<BgtCard>Content</BgtCard>);
      expect(screen.getByText('Content')).toBeInTheDocument();
    });

    it('should not render when hide is true', () => {
      render(<BgtCard hide={true}>Content</BgtCard>);
      expect(screen.queryByText('Content')).not.toBeInTheDocument();
    });

    it('should return null when hide is true', () => {
      const { container } = render(<BgtCard hide={true}>Content</BgtCard>);
      expect(container.firstChild).toBeNull();
    });
  });

  describe('ClassName Prop', () => {
    it('should apply custom className', () => {
      const { container } = render(<BgtCard className="custom-class">Content</BgtCard>);
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveClass('custom-class');
    });
  });

  describe('Additional Props', () => {
    it('should pass through data attributes', () => {
      render(<BgtCard data-testid="custom-card">Content</BgtCard>);
      expect(screen.getByTestId('custom-card')).toBeInTheDocument();
    });

    it('should pass through id attribute', () => {
      const { container } = render(<BgtCard id="card-id">Content</BgtCard>);
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveAttribute('id', 'card-id');
    });

    it('should support aria attributes', () => {
      const { container } = render(<BgtCard aria-label="Information card">Content</BgtCard>);
      const card = container.firstChild as HTMLElement;
      expect(card).toHaveAttribute('aria-label', 'Information card');
    });
  });

  describe('Combined Props', () => {
    const TestIcon = (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="test-icon" {...props} />;

    it('should handle all props together', async () => {
      const user = userEvent.setup();
      const handleClick = vi.fn();
      const actions = [{ content: 'action.submit', onClick: handleClick }];

      render(
        <BgtCard
          title="Full Card"
          icon={TestIcon}
          actions={actions}
          className="extra-class"
          data-testid="full-card"
        >
          Full Content
        </BgtCard>
      );

      expect(screen.getByTestId('full-card')).toBeInTheDocument();
      expect(screen.getByText('Full Card')).toBeInTheDocument();
      expect(screen.getByTestId('test-icon')).toBeInTheDocument();
      expect(screen.getByText('Full Content')).toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'action.submit' }));
      expect(handleClick).toHaveBeenCalled();
    });
  });
});
