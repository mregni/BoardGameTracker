import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, renderWithTheme } from '@/test/test-utils';

import { BgtSelect } from './BgtSelect';

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

const createMockField = (value: string = '') => ({
  state: {
    value,
    meta: {
      errors: [] as string[],
    },
  },
  handleChange: vi.fn(),
  handleBlur: vi.fn(),
});

const mockItems = [
  { value: '1', label: 'Option 1' },
  { value: '2', label: 'Option 2' },
  { value: '3', label: 'Option 3' },
];

describe('BgtSelect', () => {
  let mockField: ReturnType<typeof createMockField>;

  beforeEach(() => {
    mockField = createMockField();
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render select with label', () => {
      renderWithTheme(
        <BgtSelect field={mockField} label="Category" items={mockItems} />
      );
      expect(screen.getByText('Category')).toBeInTheDocument();
    });

    it('should render trigger button', () => {
      renderWithTheme(
        <BgtSelect field={mockField} label="Category" items={mockItems} />
      );
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    it('should show placeholder when no value', () => {
      renderWithTheme(
        <BgtSelect field={mockField} label="Category" items={mockItems} placeholder="Select an option" />
      );
      expect(screen.getByText('Select an option')).toBeInTheDocument();
    });

    it('should show caret down icon when closed', () => {
      renderWithTheme(
        <BgtSelect field={mockField} label="Category" items={mockItems} />
      );
      expect(screen.getByTestId('caret-down-icon')).toBeInTheDocument();
    });

    it('should display selected value', () => {
      mockField = createMockField('2');
      renderWithTheme(
        <BgtSelect field={mockField} label="Category" items={mockItems} />
      );
      expect(screen.getByText('Option 2')).toBeInTheDocument();
    });
  });

  describe('Disabled State', () => {
    it('should be disabled when disabled prop is true', () => {
      renderWithTheme(
        <BgtSelect field={mockField} label="Category" items={mockItems} disabled />
      );
      expect(screen.getByRole('combobox')).toBeDisabled();
    });

    it('should not be disabled by default', () => {
      renderWithTheme(
        <BgtSelect field={mockField} label="Category" items={mockItems} />
      );
      expect(screen.getByRole('combobox')).not.toBeDisabled();
    });
  });

  describe('Error State', () => {
    it('should display error message', () => {
      mockField.state.meta.errors = ['Please select an option'];
      renderWithTheme(
        <BgtSelect field={mockField} label="Category" items={mockItems} />
      );
      expect(screen.getByText('Please select an option')).toBeInTheDocument();
    });
  });

  describe('Props', () => {
    it('should accept empty items array', () => {
      renderWithTheme(
        <BgtSelect field={mockField} label="Category" items={[]} />
      );
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    it('should handle hasSearch prop without error', () => {
      renderWithTheme(
        <BgtSelect field={mockField} label="Category" items={mockItems} hasSearch />
      );
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });
  });
});
