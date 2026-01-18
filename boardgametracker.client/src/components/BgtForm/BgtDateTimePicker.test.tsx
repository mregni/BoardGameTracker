import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, userEvent, renderWithProviders } from '@/test/test-utils';

import { BgtDateTimePicker } from './BgtDateTimePicker';

// i18next is mocked globally in setup.ts

vi.mock('@/services/queries/settings', () => ({
  getSettings: () => ({
    queryKey: ['settings'],
    queryFn: () =>
      Promise.resolve({
        dateFormat: 'yyyy-MM-dd',
        timeFormat: 'HH:mm',
        uiLanguage: 'en-us',
      }),
  }),
}));

vi.mock('@/utils/localeUtils', () => ({
  getDateFnsLocale: () => undefined,
}));

const createMockField = (value: Date | string = '', errors: string[] = []) => ({
  state: {
    value,
    meta: {
      errors,
      isTouched: false,
      isValidating: false,
    },
  },
  handleChange: vi.fn(),
  handleBlur: vi.fn(),
  name: 'testDateTime',
});

describe('BgtDateTimePicker', () => {
  const defaultProps = {
    field: createMockField(),
    label: 'Select Date and Time',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render label and both date and time inputs', () => {
      renderWithProviders(<BgtDateTimePicker {...defaultProps} />);

      expect(screen.getByText('Select Date and Time')).toBeInTheDocument();
      expect(screen.getByRole('button')).toBeInTheDocument(); // Date picker button
      // Time input has no implicit ARIA role, so we query by placeholder
      expect(screen.getByPlaceholderText('xx:xx')).toBeInTheDocument();
    });
  });

  describe('Disabled State', () => {
    it('should disable both date and time inputs when disabled', () => {
      renderWithProviders(<BgtDateTimePicker {...defaultProps} disabled={true} />);

      expect(screen.getByRole('button')).toBeDisabled();
      expect(screen.getByPlaceholderText('xx:xx')).toBeDisabled();
    });
  });

  describe('Time Input Behavior', () => {
    it('should call handleChange when time is modified', async () => {
      const user = userEvent.setup();
      const mockHandleChange = vi.fn();
      const field = {
        ...createMockField(new Date('2024-06-15T10:30:00')),
        handleChange: mockHandleChange,
      };

      renderWithProviders(<BgtDateTimePicker {...defaultProps} field={field} />);

      // When there's a value, query by the displayed value
      const timeInput = screen.getByDisplayValue('10:30');
      await user.clear(timeInput);
      await user.type(timeInput, '14:30');

      expect(mockHandleChange).toHaveBeenCalled();
    });

    it('should display formatted time when date is set', () => {
      const dateWithTime = new Date('2024-06-15T10:30:00');
      const fieldWithValue = createMockField(dateWithTime);

      renderWithProviders(<BgtDateTimePicker {...defaultProps} field={fieldWithValue} />);

      // Assert the value is displayed correctly
      expect(screen.getByDisplayValue('10:30')).toBeInTheDocument();
    });

    it('should display empty time when no date is set', () => {
      renderWithProviders(<BgtDateTimePicker {...defaultProps} />);

      const timeInput = screen.getByPlaceholderText('xx:xx') as HTMLInputElement;
      expect(timeInput.value).toBe('');
    });

    it('should preserve existing time value', () => {
      const dateWithTime = new Date('2024-06-15T14:30:00');
      const field = createMockField(dateWithTime);

      renderWithProviders(<BgtDateTimePicker {...defaultProps} field={field} />);

      expect(screen.getByDisplayValue('14:30')).toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('should handle clearing time input without crashing', async () => {
      const user = userEvent.setup();
      const field = createMockField(new Date('2024-06-15T10:30:00'));

      renderWithProviders(<BgtDateTimePicker {...defaultProps} field={field} />);

      const timeInput = screen.getByDisplayValue('10:30');
      expect(timeInput).toBeInTheDocument();

      await user.clear(timeInput);
      expect(timeInput).toBeInTheDocument();
    });

    it('should handle ISO string date value', () => {
      const fieldWithIsoString = createMockField('2024-06-15T10:30:00.000Z');

      renderWithProviders(<BgtDateTimePicker {...defaultProps} field={fieldWithIsoString} />);

      // The time input should be rendered (value depends on timezone)
      expect(screen.getByPlaceholderText('xx:xx')).toBeInTheDocument();
    });
  });
});
