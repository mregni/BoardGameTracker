import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { screen, renderWithTheme, render } from '@/test/test-utils';

import { BgtBarChart } from './BgtBarChart';

vi.mock('@nivo/bar', () => ({
  ResponsiveBar: ({
    data,
    keys,
    indexBy,
    tooltip,
  }: {
    data: unknown[];
    keys: string[];
    indexBy: string;
    tooltip: (props: { value: number; indexValue: string }) => React.ReactNode;
  }) => (
    <div data-testid="responsive-bar" data-keys={keys.join(',')} data-index={indexBy}>
      {data.map((item, i) => (
        <div key={i} data-testid={`bar-item-${i}`}>
          {JSON.stringify(item)}
        </div>
      ))}
      <div data-testid="tooltip">{tooltip({ value: 10, indexValue: 'January' })}</div>
    </div>
  ),
}));

// Mock ResizeObserver
class MockResizeObserver {
  callback: ResizeObserverCallback;

  constructor(callback: ResizeObserverCallback) {
    this.callback = callback;
  }

  observe() {
    // Simulate a resize event
    this.callback(
      [{ contentRect: { width: 800, height: 400 } } as ResizeObserverEntry],
      this as unknown as ResizeObserver
    );
  }

  unobserve() {}
  disconnect() {}
}

describe('BgtBarChart', () => {
  const defaultProps = {
    data: [
      { month: 'January', value: 10 },
      { month: 'February', value: 20 },
      { month: 'March', value: 15 },
    ],
    index: 'month',
    keys: ['value'],
  };

  beforeEach(() => {
    vi.stubGlobal('ResizeObserver', MockResizeObserver);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  describe('Rendering', () => {
    it('should render bar chart when data is provided', () => {
      renderWithTheme(<BgtBarChart {...defaultProps} />);
      expect(screen.getByTestId('responsive-bar')).toBeInTheDocument();
    });

    it('should pass correct keys to chart', () => {
      renderWithTheme(<BgtBarChart {...defaultProps} />);
      const chart = screen.getByTestId('responsive-bar');
      expect(chart).toHaveAttribute('data-keys', 'value');
    });

    it('should pass correct index to chart', () => {
      renderWithTheme(<BgtBarChart {...defaultProps} />);
      const chart = screen.getByTestId('responsive-bar');
      expect(chart).toHaveAttribute('data-index', 'month');
    });

    it('should render all data items', () => {
      renderWithTheme(<BgtBarChart {...defaultProps} />);
      expect(screen.getByTestId('bar-item-0')).toBeInTheDocument();
      expect(screen.getByTestId('bar-item-1')).toBeInTheDocument();
      expect(screen.getByTestId('bar-item-2')).toBeInTheDocument();
    });
  });

  describe('Null/Empty Data', () => {
    it('should return null when data is null', () => {
      const { container } = render(<BgtBarChart {...defaultProps} data={null as unknown as []} />);
      expect(container.firstChild).toBeNull();
    });

    it('should return null when data is empty array', () => {
      const { container } = render(<BgtBarChart {...defaultProps} data={[]} />);
      expect(container.firstChild).toBeNull();
    });
  });

  describe('Tooltip', () => {
    it('should render tooltip with value', () => {
      renderWithTheme(<BgtBarChart {...defaultProps} />);
      expect(screen.getByText('10 sessions')).toBeInTheDocument();
    });

    it('should render tooltip with index value', () => {
      renderWithTheme(<BgtBarChart {...defaultProps} />);
      expect(screen.getByText('January')).toBeInTheDocument();
    });
  });

  describe('Multiple Keys', () => {
    it('should handle multiple keys', () => {
      renderWithTheme(<BgtBarChart {...defaultProps} keys={['value', 'count']} />);
      const chart = screen.getByTestId('responsive-bar');
      expect(chart).toHaveAttribute('data-keys', 'value,count');
    });
  });
});
