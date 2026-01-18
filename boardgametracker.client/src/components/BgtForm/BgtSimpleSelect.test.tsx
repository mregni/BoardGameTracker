import { describe, it, expect, vi, beforeAll, afterAll } from 'vitest';
import { screen, renderWithTheme } from '@/test/test-utils';

import { BgtSimpleSelect } from './BgtSimpleSelect';

// i18next is mocked globally in setup.ts

vi.mock('@/assets/icons/magnifying-glass.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="search-icon" {...props} />,
}));

vi.mock('@/assets/icons/check.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="check-icon" {...props} />,
}));

vi.mock('@/assets/icons/caret-up.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="caret-up-icon" {...props} />,
}));

vi.mock('@/assets/icons/caret-down.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="caret-down-icon" {...props} />,
}));

// Mock hasPointerCapture for Radix UI compatibility with jsdom
beforeAll(() => {
  HTMLElement.prototype.hasPointerCapture = vi.fn(() => false);
  HTMLElement.prototype.setPointerCapture = vi.fn();
  HTMLElement.prototype.releasePointerCapture = vi.fn();
});

afterAll(() => {
  vi.restoreAllMocks();
});

describe('BgtSimpleSelect', () => {
  const defaultItems = [
    { value: '1', label: 'Option 1' },
    { value: '2', label: 'Option 2' },
    { value: '3', label: 'Option 3' },
  ];

  const defaultProps = {
    items: defaultItems,
    onValueChange: vi.fn(),
  };

  describe('Rendering', () => {
    it('should render select trigger', () => {
      renderWithTheme(<BgtSimpleSelect {...defaultProps} />);
      const trigger = screen.getByRole('combobox');
      expect(trigger).toBeInTheDocument();
    });

    it('should render label when provided', () => {
      renderWithTheme(<BgtSimpleSelect {...defaultProps} label="Select Option" />);
      expect(screen.getByText('Select Option')).toBeInTheDocument();
    });

    it('should not render label when not provided', () => {
      renderWithTheme(<BgtSimpleSelect {...defaultProps} />);
      expect(screen.queryByText('Select Option')).not.toBeInTheDocument();
    });

    it('should render placeholder when provided', () => {
      renderWithTheme(<BgtSimpleSelect {...defaultProps} placeholder="Choose..." />);
      expect(screen.getByText('Choose...')).toBeInTheDocument();
    });

    it('should render caret down icon when closed', () => {
      renderWithTheme(<BgtSimpleSelect {...defaultProps} />);
      expect(screen.getByTestId('caret-down-icon')).toBeInTheDocument();
    });
  });

  describe('Selection Display', () => {
    it('should display selected value', () => {
      renderWithTheme(<BgtSimpleSelect {...defaultProps} value="2" />);
      expect(screen.getByText('Option 2')).toBeInTheDocument();
    });

    it('should handle number values', () => {
      const items = [
        { value: 1, label: 'Number 1' },
        { value: 2, label: 'Number 2' },
      ];
      renderWithTheme(<BgtSimpleSelect items={items} value={1} />);
      expect(screen.getByText('Number 1')).toBeInTheDocument();
    });

    it('should show placeholder when no value selected', () => {
      renderWithTheme(<BgtSimpleSelect {...defaultProps} value={null} placeholder="Select..." />);
      expect(screen.getByText('Select...')).toBeInTheDocument();
    });
  });

  describe('Disabled State', () => {
    it('should not be disabled by default', () => {
      renderWithTheme(<BgtSimpleSelect {...defaultProps} />);
      const trigger = screen.getByRole('combobox');
      expect(trigger).not.toBeDisabled();
    });

    it('should be disabled when disabled prop is true', () => {
      renderWithTheme(<BgtSimpleSelect {...defaultProps} disabled={true} />);
      const trigger = screen.getByRole('combobox');
      expect(trigger).toBeDisabled();
    });
  });

  describe('ClassName Prop', () => {
    it('should apply custom className to wrapper', () => {
      const { container } = renderWithTheme(<BgtSimpleSelect {...defaultProps} className="custom-class" />);
      const wrapper = container.querySelector('.custom-class');
      expect(wrapper).toBeInTheDocument();
    });
  });

  describe('Combined Props', () => {
    it('should handle multiple props together', () => {
      renderWithTheme(
        <BgtSimpleSelect
          items={defaultItems}
          label="Choose Player"
          value="1"
          placeholder="Select..."
          className="player-select"
        />
      );

      expect(screen.getByText('Choose Player')).toBeInTheDocument();
      expect(screen.getByText('Option 1')).toBeInTheDocument();
    });
  });
});
