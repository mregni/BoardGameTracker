import { describe, it, expect, vi } from 'vitest';

import { BgtTextStatistic } from './BgtTextStatistic';

import { screen, renderWithTheme, render } from '@/test/test-utils';

vi.mock('@/assets/icons/trophy.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="trophy-icon" {...props} />,
}));

describe('BgtTextStatistic', () => {
  describe('Rendering', () => {
    it('should render title', () => {
      renderWithTheme(<BgtTextStatistic title="Total Games" content={42} />);
      expect(screen.getByText('Total Games')).toBeInTheDocument();
    });

    it('should render content as string', () => {
      renderWithTheme(<BgtTextStatistic title="Best Game" content="Chess" />);
      expect(screen.getByText('Chess')).toBeInTheDocument();
    });

    it('should render content as number', () => {
      renderWithTheme(<BgtTextStatistic title="Wins" content={100} />);
      expect(screen.getByText('100')).toBeInTheDocument();
    });

    it('should format large numbers with locale', () => {
      const { container } = renderWithTheme(<BgtTextStatistic title="Points" content={1234567} />);
      // toLocaleString format varies by locale (1,234,567 or 1.234.567)
      const textContent = container.textContent;
      expect(textContent).toContain('1');
      expect(textContent).toContain('234');
      expect(textContent).toContain('567');
    });
  });

  describe('Null/Undefined Content', () => {
    it('should return null when content is null', () => {
      const { container } = render(<BgtTextStatistic title="Test" content={null} />);
      expect(container.firstChild).toBeNull();
    });

    it('should return null when content is undefined', () => {
      const { container } = render(<BgtTextStatistic title="Test" content={undefined as unknown as null} />);
      expect(container.firstChild).toBeNull();
    });
  });

  describe('Prefix and Suffix', () => {
    it('should render prefix', () => {
      renderWithTheme(<BgtTextStatistic title="Price" content={99} prefix="$" />);
      expect(screen.getByText('$')).toBeInTheDocument();
    });

    it('should render suffix', () => {
      renderWithTheme(<BgtTextStatistic title="Duration" content={45} suffix="min" />);
      expect(screen.getByText('min')).toBeInTheDocument();
    });

    it('should render both prefix and suffix', () => {
      renderWithTheme(<BgtTextStatistic title="Score" content={100} prefix="+" suffix="pts" />);
      expect(screen.getByText('+')).toBeInTheDocument();
      expect(screen.getByText('pts')).toBeInTheDocument();
    });

    it('should not render prefix when null', () => {
      renderWithTheme(<BgtTextStatistic title="Value" content={50} prefix={null} />);
      expect(screen.queryByText('null')).not.toBeInTheDocument();
    });

    it('should not render suffix when null', () => {
      renderWithTheme(<BgtTextStatistic title="Value" content={50} suffix={null} />);
      expect(screen.queryByText('null')).not.toBeInTheDocument();
    });
  });

  describe('Icon', () => {
    it('should render icon when provided', () => {
      const TestIcon = (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="test-icon" {...props} />;
      renderWithTheme(<BgtTextStatistic title="Achievements" content={5} icon={<TestIcon />} />);
      expect(screen.getByTestId('test-icon')).toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('should handle zero content', () => {
      renderWithTheme(<BgtTextStatistic title="Losses" content={0} />);
      expect(screen.getByText('0')).toBeInTheDocument();
    });

    it('should handle empty string content', () => {
      renderWithTheme(<BgtTextStatistic title="Status" content="" />);
      expect(screen.getByText('Status')).toBeInTheDocument();
    });

    it('should handle negative numbers', () => {
      renderWithTheme(<BgtTextStatistic title="Change" content={-50} />);
      expect(screen.getByText('-50')).toBeInTheDocument();
    });

    it('should handle decimal numbers', () => {
      const { container } = renderWithTheme(<BgtTextStatistic title="Average" content={3.5} />);
      // toLocaleString may format as 3.5 or 3,5 depending on locale
      const textContent = container.textContent;
      expect(textContent).toContain('3');
      expect(textContent?.includes('.5') || textContent?.includes(',5')).toBe(true);
    });
  });
});
