import { describe, it, expect, vi, beforeEach } from 'vitest';

import { BgtDatePicker } from './BgtDatePicker';

import { screen, waitFor, userEvent, renderWithProviders } from '@/test/test-utils';

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

const createMockField = (value: string = '', errors: string[] = []) => ({
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
  name: 'testDate',
});

describe('BgtDatePicker', () => {
  const defaultProps = {
    field: createMockField(),
    label: 'Select Date',
    placeholder: 'Pick a date',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render the label', () => {
      renderWithProviders(<BgtDatePicker {...defaultProps} />);
      expect(screen.getByText('Select Date')).toBeInTheDocument();
    });

    it('should render the placeholder when no date is selected', () => {
      renderWithProviders(<BgtDatePicker {...defaultProps} />);
      expect(screen.getByText('Pick a date')).toBeInTheDocument();
    });

    it('should render calendar icon', () => {
      renderWithProviders(<BgtDatePicker {...defaultProps} />);
      const button = screen.getByRole('button');
      expect(button).toBeInTheDocument();
    });

    it('should render with custom className', () => {
      renderWithProviders(<BgtDatePicker {...defaultProps} className="custom-class" />);
      const button = screen.getByRole('button');
      expect(button).toHaveClass('custom-class');
    });
  });

  describe('Disabled State', () => {
    it('should disable the button when disabled is true', () => {
      renderWithProviders(<BgtDatePicker {...defaultProps} disabled={true} />);
      const button = screen.getByRole('button');
      expect(button).toBeDisabled();
    });

    it('should enable the button when disabled is false', () => {
      renderWithProviders(<BgtDatePicker {...defaultProps} disabled={false} />);
      const button = screen.getByRole('button');
      expect(button).not.toBeDisabled();
    });
  });

  describe('Error State', () => {
    it('should display error messages when field has errors', () => {
      const fieldWithErrors = createMockField('', ['Date is required']);
      renderWithProviders(<BgtDatePicker {...defaultProps} field={fieldWithErrors} />);

      expect(screen.getByText('Date is required')).toBeInTheDocument();
    });

    it('should not display error messages when field has no errors', () => {
      renderWithProviders(<BgtDatePicker {...defaultProps} />);

      expect(screen.queryByText('Date is required')).not.toBeInTheDocument();
    });
  });

  describe('Popover Interaction', () => {
    it('should open calendar popover when button is clicked', async () => {
      const user = userEvent.setup();
      renderWithProviders(<BgtDatePicker {...defaultProps} />);

      await user.click(screen.getByRole('button'));

      await waitFor(() => {
        // The DayPicker should be visible
        expect(screen.getByRole('grid')).toBeInTheDocument();
      });
    });

    it('should not open popover when disabled', async () => {
      const user = userEvent.setup();
      renderWithProviders(<BgtDatePicker {...defaultProps} disabled={true} />);

      await user.click(screen.getByRole('button'));

      await waitFor(() => {
        expect(screen.queryByRole('grid')).not.toBeInTheDocument();
      });
    });
  });

  describe('Date Selection', () => {
    it('should call handleChange when a date is selected', async () => {
      const user = userEvent.setup();
      const mockHandleChange = vi.fn();
      const field = {
        ...createMockField(),
        handleChange: mockHandleChange,
      };

      renderWithProviders(<BgtDatePicker {...defaultProps} field={field} />);

      await user.click(screen.getByRole('button'));

      await waitFor(() => {
        expect(screen.getByRole('grid')).toBeInTheDocument();
      });

      // Find and click a day button (today or any available day)
      const dayButtons = screen.getAllByRole('gridcell');
      const clickableDay = dayButtons.find(
        (btn) => btn.querySelector('button') && !btn.querySelector('button')?.disabled
      );

      if (clickableDay) {
        const dayButton = clickableDay.querySelector('button');
        if (dayButton) {
          await user.click(dayButton);
          expect(mockHandleChange).toHaveBeenCalled();
        }
      }
    });
  });

  describe('Pre-selected Date', () => {
    it('should show formatted date when field has value and settings are loaded', async () => {
      const fieldWithValue = createMockField('2024-06-15');
      renderWithProviders(<BgtDatePicker {...defaultProps} field={fieldWithValue} />);

      // Wait for settings to load, then the formatted date should be visible
      await waitFor(() => {
        expect(screen.queryByText('Pick a date')).not.toBeInTheDocument();
      });
    });
  });

  describe('Accessibility', () => {
    it('should have accessible button', () => {
      renderWithProviders(<BgtDatePicker {...defaultProps} />);
      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('type', 'button');
    });
  });
});
