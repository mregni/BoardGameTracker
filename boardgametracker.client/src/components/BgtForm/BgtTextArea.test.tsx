import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, userEvent, renderWithTheme } from '@/test/test-utils';

import { BgtTextArea } from './BgtTextArea';

// i18next is mocked globally in setup.ts

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

describe('BgtTextArea', () => {
  let mockField: ReturnType<typeof createMockField>;

  beforeEach(() => {
    mockField = createMockField();
  });

  describe('Rendering', () => {
    it('should render textarea', () => {
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);
      expect(screen.getByRole('textbox')).toBeInTheDocument();
    });

    it('should render with label', () => {
      renderWithTheme(<BgtTextArea field={mockField} label="Notes" />);
      expect(screen.getByText('Notes')).toBeInTheDocument();
    });

    it('should display field value', () => {
      mockField = createMockField('Some text content');
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);
      expect(screen.getByDisplayValue('Some text content')).toBeInTheDocument();
    });

    it('should handle null value', () => {
      mockField.state.value = null as unknown as string;
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);
      expect(screen.getByRole('textbox')).toHaveValue('');
    });

    it('should handle undefined value', () => {
      mockField.state.value = undefined as unknown as string;
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);
      expect(screen.getByRole('textbox')).toHaveValue('');
    });
  });

  describe('Interaction', () => {
    it('should call handleChange on input', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);

      await user.type(screen.getByRole('textbox'), 'Hello');
      expect(mockField.handleChange).toHaveBeenCalled();
    });

    it('should call handleBlur on blur', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);

      const textarea = screen.getByRole('textbox');
      await user.click(textarea);
      await user.tab();
      expect(mockField.handleBlur).toHaveBeenCalled();
    });

    it('should allow multiline input', async () => {
      const user = userEvent.setup();
      mockField = createMockField('Line 1\nLine 2');
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);

      expect(screen.getByRole('textbox')).toHaveValue('Line 1\nLine 2');
    });
  });

  describe('Disabled State', () => {
    it('should be disabled when disabled prop is true', () => {
      renderWithTheme(<BgtTextArea field={mockField} label="Description" disabled />);
      expect(screen.getByRole('textbox')).toBeDisabled();
    });

    it('should not be disabled by default', () => {
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);
      expect(screen.getByRole('textbox')).not.toBeDisabled();
    });
  });

  describe('Error State', () => {
    it('should display errors when present', () => {
      mockField.state.meta.errors = ['This field is required'];
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);
      expect(screen.getByText('This field is required')).toBeInTheDocument();
    });

    it('should not display errors when none present', () => {
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);
      expect(screen.queryByText('This field is required')).not.toBeInTheDocument();
    });
  });

  describe('Rows Attribute', () => {
    it('should have 4 rows by default', () => {
      renderWithTheme(<BgtTextArea field={mockField} label="Description" />);
      expect(screen.getByRole('textbox')).toHaveAttribute('rows', '4');
    });
  });
});
