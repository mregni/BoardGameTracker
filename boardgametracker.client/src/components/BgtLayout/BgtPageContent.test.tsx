import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@/test/test-utils';

import { BgtPageContent } from './BgtPageContent';

vi.mock('../BgtDataGuard/BgtDataGuard', () => ({
  BgtDataGuard: ({ isLoading, data, children }: { isLoading: boolean; data: Record<string, unknown>; children: (data: Record<string, unknown>) => React.ReactNode }) => {
    if (isLoading) return <div data-testid="loading">Loading...</div>;
    const hasUndefined = Object.values(data).some((v) => v === undefined);
    if (hasUndefined) return <div data-testid="loading">Loading...</div>;
    return <>{children(data)}</>;
  },
}));

describe('BgtPageContent', () => {
  describe('Without Guard (Simple Children)', () => {
    it('should render children', () => {
      render(
        <BgtPageContent>
          <div data-testid="content">Page Content</div>
        </BgtPageContent>
      );
      expect(screen.getByTestId('content')).toBeInTheDocument();
    });

    it('should render multiple children', () => {
      render(
        <BgtPageContent>
          <div data-testid="child1">Child 1</div>
          <div data-testid="child2">Child 2</div>
        </BgtPageContent>
      );
      expect(screen.getByTestId('child1')).toBeInTheDocument();
      expect(screen.getByTestId('child2')).toBeInTheDocument();
    });
  });

  describe('With Guard (Data Loading)', () => {
    it('should show loading when isLoading is true', () => {
      render(
        <BgtPageContent isLoading={true} data={{ value: 'test' }}>
          {({ value }) => <div>{value}</div>}
        </BgtPageContent>
      );
      expect(screen.getByTestId('loading')).toBeInTheDocument();
    });

    it('should render children with data when loaded', () => {
      render(
        <BgtPageContent isLoading={false} data={{ name: 'John' }}>
          {({ name }) => <div data-testid="content">Hello {name}</div>}
        </BgtPageContent>
      );
      expect(screen.getByTestId('content')).toBeInTheDocument();
      expect(screen.getByText('Hello John')).toBeInTheDocument();
    });

    it('should show loading when data has undefined values', () => {
      render(
        <BgtPageContent isLoading={false} data={{ value: undefined }}>
          {({ value }) => <div>{value as string}</div>}
        </BgtPageContent>
      );
      expect(screen.getByTestId('loading')).toBeInTheDocument();
    });
  });

  describe('ClassName Prop', () => {
    it('should apply custom className', () => {
      const { container } = render(
        <BgtPageContent className="custom-class">
          <div>Content</div>
        </BgtPageContent>
      );
      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toHaveClass('custom-class');
    });

    it('should apply custom className with guard', () => {
      const { container } = render(
        <BgtPageContent isLoading={false} data={{ value: 'test' }} className="guarded-class">
          {({ value }) => <div>{value}</div>}
        </BgtPageContent>
      );
      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toHaveClass('guarded-class');
    });
  });
});
