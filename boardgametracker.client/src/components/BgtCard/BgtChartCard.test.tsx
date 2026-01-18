import { describe, it, expect } from 'vitest';
import { screen, renderWithTheme, render } from '@/test/test-utils';

import { BgtChartCard } from './BgtChartCard';

describe('BgtChartCard', () => {
  describe('Rendering', () => {
    it('should render title', () => {
      renderWithTheme(
        <BgtChartCard title="Chart Title">
          <div>Chart content</div>
        </BgtChartCard>
      );
      expect(screen.getByText('Chart Title')).toBeInTheDocument();
    });

    it('should render children', () => {
      renderWithTheme(
        <BgtChartCard title="Title">
          <div data-testid="chart-content">My Chart</div>
        </BgtChartCard>
      );
      expect(screen.getByTestId('chart-content')).toBeInTheDocument();
    });

    it('should render multiple children', () => {
      renderWithTheme(
        <BgtChartCard title="Title">
          <div data-testid="child1">Child 1</div>
          <div data-testid="child2">Child 2</div>
        </BgtChartCard>
      );
      expect(screen.getByTestId('child1')).toBeInTheDocument();
      expect(screen.getByTestId('child2')).toBeInTheDocument();
    });
  });

  describe('Hide Prop', () => {
    it('should render when hide is false', () => {
      renderWithTheme(
        <BgtChartCard title="Visible Chart" hide={false}>
          <div data-testid="content">Content</div>
        </BgtChartCard>
      );
      expect(screen.getByTestId('content')).toBeInTheDocument();
    });

    it('should not render when hide is true', () => {
      const { container } = render(
        <BgtChartCard title="Hidden Chart" hide={true}>
          <div data-testid="hidden-content">Content</div>
        </BgtChartCard>
      );
      expect(container.firstChild).toBeNull();
    });

    it('should render by default when hide is not specified', () => {
      renderWithTheme(
        <BgtChartCard title="Default Chart">
          <div data-testid="default-content">Content</div>
        </BgtChartCard>
      );
      expect(screen.getByTestId('default-content')).toBeInTheDocument();
    });
  });

  describe('HTML Attributes', () => {
    it('should pass through additional props', () => {
      renderWithTheme(
        <BgtChartCard title="Title" data-testid="chart-card" id="my-chart">
          <div>Content</div>
        </BgtChartCard>
      );
      const card = screen.getByTestId('chart-card');
      expect(card).toHaveAttribute('id', 'my-chart');
    });
  });

  describe('Combined Props', () => {
    it('should handle all props together', () => {
      renderWithTheme(
        <BgtChartCard title="Sales Chart" className="sales-chart" hide={false} data-testid="combined-chart">
          <div data-testid="chart-visualization">Chart</div>
        </BgtChartCard>
      );

      expect(screen.getByText('Sales Chart')).toBeInTheDocument();
      expect(screen.getByTestId('chart-visualization')).toBeInTheDocument();
      expect(screen.getByTestId('combined-chart')).toBeInTheDocument();
    });
  });
});
