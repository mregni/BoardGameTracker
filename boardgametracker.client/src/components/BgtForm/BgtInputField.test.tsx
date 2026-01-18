import { describe, it, expect, vi, beforeEach } from 'vitest';

import { BgtInputField } from './BgtInputField';

import { screen, userEvent, renderWithTheme } from '@/test/test-utils';

// i18next is mocked globally in setup.ts

const createMockField = (value: string | number | Date | undefined = '') => ({
  state: {
    value,
    meta: {
      errors: [] as string[],
    },
  },
  handleChange: vi.fn(),
  handleBlur: vi.fn(),
});

describe('BgtInputField', () => {
  let mockField: ReturnType<typeof createMockField>;

  beforeEach(() => {
    mockField = createMockField();
  });

  describe('Rendering', () => {
    it('should render input field', () => {
      renderWithTheme(<BgtInputField field={mockField} type="text" label="Username" placeholder="enter text" />);
      expect(screen.getByRole('textbox')).toBeInTheDocument();
      expect(screen.getByText('Username')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('ENTER TEXT')).toBeInTheDocument();
    });

    it('should render with prefix label', () => {
      renderWithTheme(<BgtInputField field={mockField} type="text" prefixLabel="$" />);
      expect(screen.getByText('$')).toBeInTheDocument();
    });

    it('should render with suffix label', () => {
      renderWithTheme(<BgtInputField field={mockField} type="text" suffixLabel="kg" />);
      expect(screen.getByText('kg')).toBeInTheDocument();
    });

    it('should display field value', () => {
      mockField = createMockField('Hello World');
      renderWithTheme(<BgtInputField field={mockField} type="text" />);
      expect(screen.getByDisplayValue('Hello World')).toBeInTheDocument();
    });
  });

  describe('Input Types', () => {
    it('should render text input', () => {
      renderWithTheme(<BgtInputField field={mockField} type="text" />);
      expect(screen.getByRole('textbox')).toHaveAttribute('type', 'text');
    });

    it('should render number input', () => {
      renderWithTheme(<BgtInputField field={mockField} type="number" />);
      expect(screen.getByRole('spinbutton')).toHaveAttribute('type', 'number');
    });

    it('should render date input', () => {
      const { container } = renderWithTheme(<BgtInputField field={mockField} type="date" />);
      const input = container.querySelector('input[type="date"]');
      expect(input).toBeInTheDocument();
    });

    it('should render datetime-local input', () => {
      const { container } = renderWithTheme(<BgtInputField field={mockField} type="datetime-local" />);
      const input = container.querySelector('input[type="datetime-local"]');
      expect(input).toBeInTheDocument();
    });
  });

  describe('Interaction', () => {
    it('should call handleChange on text input', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtInputField field={mockField} type="text" />);

      await user.type(screen.getByRole('textbox'), 'a');
      expect(mockField.handleChange).toHaveBeenCalledWith('a');
    });

    it('should call handleChange with number for number type', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtInputField field={mockField} type="number" />);

      await user.type(screen.getByRole('spinbutton'), '5');
      expect(mockField.handleChange).toHaveBeenCalledWith(5);
    });

    it('should call handleBlur on blur', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtInputField field={mockField} type="text" />);

      const input = screen.getByRole('textbox');
      await user.click(input);
      await user.tab();
      expect(mockField.handleBlur).toHaveBeenCalled();
    });
  });

  describe('Disabled State', () => {
    it('should be disabled when disabled prop is true', () => {
      renderWithTheme(<BgtInputField field={mockField} type="text" disabled />);
      expect(screen.getByRole('textbox')).toBeDisabled();
    });

    it('should not be disabled by default', () => {
      renderWithTheme(<BgtInputField field={mockField} type="text" />);
      expect(screen.getByRole('textbox')).not.toBeDisabled();
    });
  });

  describe('Error State', () => {
    it('should display error message when field has errors', () => {
      mockField = createMockField('');
      mockField.state.meta.errors = ['Required field'];
      renderWithTheme(<BgtInputField field={mockField} type="text" label="Test Field" />);
      expect(screen.getByText('Required field')).toBeInTheDocument();
    });

    it('should not display error message when no errors', () => {
      renderWithTheme(<BgtInputField field={mockField} type="text" />);
      expect(screen.queryByText('Required field')).not.toBeInTheDocument();
    });
  });

  describe('Date Formatting', () => {
    it('should format Date value for datetime-local', () => {
      const testDate = new Date('2024-06-15T14:30:00');
      mockField = createMockField(testDate);
      const { container } = renderWithTheme(<BgtInputField field={mockField} type="datetime-local" />);
      const input = container.querySelector('input[type="datetime-local"]') as HTMLInputElement;
      expect(input.value).toBe('2024-06-15T14:30');
    });

    it('should format Date value for date type', () => {
      const testDate = new Date('2024-06-15');
      mockField = createMockField(testDate);
      const { container } = renderWithTheme(<BgtInputField field={mockField} type="date" />);
      const input = container.querySelector('input[type="date"]') as HTMLInputElement;
      expect(input.value).toBe('2024-06-15');
    });

    it('should handle empty value gracefully', () => {
      mockField = createMockField(undefined);
      renderWithTheme(<BgtInputField field={mockField} type="text" />);
      expect(screen.getByRole('textbox')).toHaveValue('');
    });
  });
});
