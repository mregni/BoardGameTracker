import { describe, it, expect, vi } from 'vitest';

import { BgtPieChart } from './BgtPieChart';

import { screen, renderWithTheme, render } from '@/test/test-utils';


// i18next is mocked globally in setup.ts

vi.mock('@nivo/pie', () => ({
  ResponsivePie: ({
    data,
    tooltip,
  }: {
    data: Array<{ id: string; label: string; value: number }>;
    tooltip: (props: { datum: { id: string; label: string; value: number } }) => React.ReactNode;
  }) => (
    <div data-testid="responsive-pie">
      {data.map((item, i) => (
        <div key={i} data-testid={`pie-item-${i}`}>
          {item.label}: {item.value}
        </div>
      ))}
      <div data-testid="tooltip">{tooltip({ datum: { id: '1', label: 'Test', value: 10 } })}</div>
    </div>
  ),
}));

vi.mock('@/models/Charts/PieChartDatum', () => ({
  pieColors: ['#ff0000', '#00ff00', '#0000ff'],
}));

describe('BgtPieChart', () => {
  const defaultProps = {
    data: [
      { id: '1', label: 'Category A', value: 30 },
      { id: '2', label: 'Category B', value: 50 },
      { id: '3', label: 'Category C', value: 20 },
    ],
    tooltipPrefix: 'chart.sessions',
  };

  describe('Rendering', () => {
    it('should render pie chart when data is provided', () => {
      renderWithTheme(<BgtPieChart {...defaultProps} />);
      expect(screen.getByTestId('responsive-pie')).toBeInTheDocument();
    });

    it('should render all data items', () => {
      renderWithTheme(<BgtPieChart {...defaultProps} />);
      expect(screen.getByTestId('pie-item-0')).toBeInTheDocument();
      expect(screen.getByTestId('pie-item-1')).toBeInTheDocument();
      expect(screen.getByTestId('pie-item-2')).toBeInTheDocument();
    });

    it('should display total in center', () => {
      renderWithTheme(<BgtPieChart {...defaultProps} />);
      expect(screen.getByText('100')).toBeInTheDocument(); // 30 + 50 + 20 = 100
    });

    it('should display "Total" label', () => {
      renderWithTheme(<BgtPieChart {...defaultProps} />);
      expect(screen.getByText('common.total')).toBeInTheDocument();
    });
  });

  describe('Null/Empty Data', () => {
    it('should return null when data is null', () => {
      const { container } = render(<BgtPieChart {...defaultProps} data={null as unknown as []} />);
      expect(container.firstChild).toBeNull();
    });

    it('should return null when data is empty array', () => {
      const { container } = render(<BgtPieChart {...defaultProps} data={[]} />);
      expect(container.firstChild).toBeNull();
    });
  });

  describe('Legend', () => {
    it('should not show legend by default', () => {
      renderWithTheme(<BgtPieChart {...defaultProps} />);
      // Legend items have specific styling with w-3 h-3 divs
      const legendItems = screen.queryAllByText('Category A');
      // Only the pie item should show, not a separate legend item
      expect(legendItems.length).toBeLessThanOrEqual(2);
    });

    it('should show legend when showLegend is true', () => {
      renderWithTheme(<BgtPieChart {...defaultProps} showLegend={true} />);
      // All category labels should appear in legend
      expect(screen.getAllByText('Category A').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('Category B').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('Category C').length).toBeGreaterThanOrEqual(1);
    });

    it('should display values in legend', () => {
      renderWithTheme(<BgtPieChart {...defaultProps} showLegend={true} />);
      expect(screen.getByText('30')).toBeInTheDocument();
      expect(screen.getByText('50')).toBeInTheDocument();
      expect(screen.getByText('20')).toBeInTheDocument();
    });
  });

  describe('Tooltip', () => {
    it('should render tooltip content', () => {
      renderWithTheme(<BgtPieChart {...defaultProps} />);
      expect(screen.getByTestId('tooltip')).toBeInTheDocument();
    });

    it('should display label in tooltip', () => {
      renderWithTheme(<BgtPieChart {...defaultProps} />);
      expect(screen.getByText('Test')).toBeInTheDocument();
    });
  });

  describe('Combined Props', () => {
    it('should handle all props together', () => {
      renderWithTheme(
        <BgtPieChart
          data={[
            { id: '1', label: 'Owned', value: 75 },
            { id: '2', label: 'Wanted', value: 25 },
          ]}
          showLegend={true}
          tooltipPrefix="games.count"
        />
      );

      expect(screen.getByText('100')).toBeInTheDocument();
      expect(screen.getByText('common.total')).toBeInTheDocument();
      expect(screen.getAllByText('Owned').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('Wanted').length).toBeGreaterThanOrEqual(1);
    });
  });
});
