import { describe, it, expect } from 'vitest';

import { BgtCenteredCard } from './BgtCenteredCard';

import { screen, renderWithTheme, render } from '@/test/test-utils';

describe('BgtCenteredCard', () => {
  describe('Rendering', () => {
    it('should render children', () => {
      renderWithTheme(
        <BgtCenteredCard>
          <div data-testid="content">Card Content</div>
        </BgtCenteredCard>
      );
      expect(screen.getByTestId('content')).toBeInTheDocument();
    });

    it('should render title when provided', () => {
      renderWithTheme(
        <BgtCenteredCard title="Card Title">
          <div>Content</div>
        </BgtCenteredCard>
      );
      expect(screen.getByText('Card Title')).toBeInTheDocument();
    });

    it('should not render title when not provided', () => {
      renderWithTheme(
        <BgtCenteredCard>
          <div>Content</div>
        </BgtCenteredCard>
      );
      expect(screen.queryByRole('heading')).not.toBeInTheDocument();
    });
  });

  describe('Hide Prop', () => {
    it('should render when hide is false', () => {
      renderWithTheme(
        <BgtCenteredCard hide={false}>
          <div data-testid="visible">Visible</div>
        </BgtCenteredCard>
      );
      expect(screen.getByTestId('visible')).toBeInTheDocument();
    });

    it('should not render when hide is true', () => {
      const { container } = render(
        <BgtCenteredCard hide={true}>
          <div data-testid="hidden">Hidden</div>
        </BgtCenteredCard>
      );
      expect(container.firstChild).toBeNull();
    });

    it('should render by default (hide not provided)', () => {
      renderWithTheme(
        <BgtCenteredCard>
          <div data-testid="default">Default</div>
        </BgtCenteredCard>
      );
      expect(screen.getByTestId('default')).toBeInTheDocument();
    });
  });

  describe('HTML Attributes', () => {
    it('should pass through other HTML attributes', () => {
      renderWithTheme(
        <BgtCenteredCard data-testid="card-wrapper" id="my-card">
          <div>Content</div>
        </BgtCenteredCard>
      );
      const wrapper = screen.getByTestId('card-wrapper');
      expect(wrapper).toHaveAttribute('id', 'my-card');
    });
  });

  describe('Combined Props', () => {
    it('should handle all props together', () => {
      renderWithTheme(
        <BgtCenteredCard title="Welcome" className="welcome-card" hide={false}>
          <div data-testid="main-content">Main content here</div>
        </BgtCenteredCard>
      );

      expect(screen.getByText('Welcome')).toBeInTheDocument();
      expect(screen.getByTestId('main-content')).toBeInTheDocument();
    });
  });
});
