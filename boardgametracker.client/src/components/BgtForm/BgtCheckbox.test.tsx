import { describe, it, expect, vi, beforeEach } from 'vitest';

import { BgtCheckbox } from './BgtCheckbox';

import { screen, userEvent, renderWithTheme } from '@/test/test-utils';

vi.mock('@/assets/icons/check.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="check-icon" {...props} />,
}));

const createMockField = (value: boolean = false) => ({
  state: {
    value,
    meta: {
      errors: [],
    },
  },
  handleChange: vi.fn(),
  handleBlur: vi.fn(),
});

describe('BgtCheckbox', () => {
  let mockField: ReturnType<typeof createMockField>;

  beforeEach(() => {
    mockField = createMockField();
  });

  describe('Rendering', () => {
    it('should render checkbox with label', () => {
      renderWithTheme(<BgtCheckbox field={mockField} id="test-checkbox" label="Test Label" />);
      expect(screen.getByText('Test Label')).toBeInTheDocument();
    });

    it('should render unchecked by default', () => {
      renderWithTheme(<BgtCheckbox field={mockField} id="test-checkbox" label="Test" />);
      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).not.toBeChecked();
    });

    it('should render checked when field value is true', () => {
      mockField = createMockField(true);
      renderWithTheme(<BgtCheckbox field={mockField} id="test-checkbox" label="Test" />);
      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).toBeChecked();
    });

    it('should have correct id', () => {
      renderWithTheme(<BgtCheckbox field={mockField} id="my-checkbox" label="Test" />);
      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).toHaveAttribute('id', 'my-checkbox');
    });

    it('should associate label with checkbox via htmlFor', () => {
      renderWithTheme(<BgtCheckbox field={mockField} id="linked-checkbox" label="Linked Label" />);
      const label = screen.getByText('Linked Label');
      expect(label).toHaveAttribute('for', 'linked-checkbox');
    });
  });

  describe('Interaction', () => {
    it('should call handleChange when clicked', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtCheckbox field={mockField} id="test-checkbox" label="Test" />);

      await user.click(screen.getByRole('checkbox'));
      expect(mockField.handleChange).toHaveBeenCalledWith(true);
    });

    it('should call handleChange with false when unchecking', async () => {
      const user = userEvent.setup();
      mockField = createMockField(true);
      renderWithTheme(<BgtCheckbox field={mockField} id="test-checkbox" label="Test" />);

      await user.click(screen.getByRole('checkbox'));
      expect(mockField.handleChange).toHaveBeenCalledWith(false);
    });

    it('should be clickable via label', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtCheckbox field={mockField} id="test-checkbox" label="Click Me" />);

      await user.click(screen.getByText('Click Me'));
      expect(mockField.handleChange).toHaveBeenCalled();
    });
  });

  describe('Disabled State', () => {
    it('should be disabled when disabled prop is true', () => {
      renderWithTheme(<BgtCheckbox field={mockField} id="test-checkbox" label="Test" disabled />);
      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).toBeDisabled();
    });

    it('should not be disabled by default', () => {
      renderWithTheme(<BgtCheckbox field={mockField} id="test-checkbox" label="Test" />);
      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).not.toBeDisabled();
    });

    it('should not call handleChange when disabled and clicked', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtCheckbox field={mockField} id="test-checkbox" label="Test" disabled />);

      await user.click(screen.getByRole('checkbox'));
      expect(mockField.handleChange).not.toHaveBeenCalled();
    });
  });

  describe('Icon', () => {
    it('should show check icon when checked', () => {
      mockField = createMockField(true);
      renderWithTheme(<BgtCheckbox field={mockField} id="test-checkbox" label="Test" />);
      expect(screen.getByTestId('check-icon')).toBeInTheDocument();
    });
  });
});
