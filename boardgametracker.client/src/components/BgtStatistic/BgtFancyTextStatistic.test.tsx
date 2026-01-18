import { describe, it, expect } from 'vitest';
import { screen, renderWithTheme, render } from '@/test/test-utils';

import { BgtFancyTextStatistic } from './BgtFancyTextStatistic';

describe('BgtFancyTextStatistic', () => {
  describe('Rendering', () => {
    it('should render title', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Total Plays" content={150} />);
      expect(screen.getByText('Total Plays')).toBeInTheDocument();
    });

    it('should render string content', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Favorite" content="Catan" />);
      expect(screen.getByText('Catan')).toBeInTheDocument();
    });

    it('should render number content', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Score" content={2500} />);
      expect(screen.getByText('2500')).toBeInTheDocument();
    });

    it('should render Date content as string', () => {
      const date = new Date('2024-06-15');
      renderWithTheme(<BgtFancyTextStatistic title="Date" content={date} />);
      expect(screen.getByText(date.toString())).toBeInTheDocument();
    });
  });

  describe('Null/Undefined Content', () => {
    it('should return null when content is null', () => {
      const { container } = render(<BgtFancyTextStatistic title="Test" content={null as unknown as undefined} />);
      expect(container.firstChild).toBeNull();
    });

    it('should return null when content is undefined', () => {
      const { container } = render(<BgtFancyTextStatistic title="Test" content={undefined} />);
      expect(container.firstChild).toBeNull();
    });
  });

  describe('Suffix', () => {
    it('should render suffix when provided', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Streak" content={7} suffix="days in a row" />);
      expect(screen.getByText('days in a row')).toBeInTheDocument();
    });

    it('should not render suffix when null', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Value" content={100} suffix={null} />);
      expect(screen.getByText('100')).toBeInTheDocument();
    });

    it('should not render suffix when not provided', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Count" content={50} />);
      expect(screen.queryByText('undefined')).not.toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('should handle zero content', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Empty" content={0} />);
      expect(screen.getByText('0')).toBeInTheDocument();
    });

    it('should handle empty string content', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Status" content="" />);
      expect(screen.getByText('Status')).toBeInTheDocument();
    });

    it('should handle negative numbers', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Delta" content={-25} />);
      expect(screen.getByText('-25')).toBeInTheDocument();
    });

    it('should handle long content', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Game" content="Twilight Imperium Fourth Edition" />);
      expect(screen.getByText('Twilight Imperium Fourth Edition')).toBeInTheDocument();
    });
  });

  describe('Combined Props', () => {
    it('should handle all props together', () => {
      renderWithTheme(<BgtFancyTextStatistic title="Win Rate" content={75} suffix="% success rate" />);

      expect(screen.getByText('Win Rate')).toBeInTheDocument();
      expect(screen.getByText('75')).toBeInTheDocument();
      expect(screen.getByText('% success rate')).toBeInTheDocument();
    });
  });
});
