import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, userEvent, renderWithTheme } from '@/test/test-utils';

import { NewLocationModal } from './NewLocationModal';

// i18next is mocked globally in setup.ts

const mockSaveLocation = vi.fn();

vi.mock('../-hooks/useLocationModal', () => ({
  useLocationModal: () => ({
    saveLocation: mockSaveLocation,
    isLoading: false,
  }),
}));

vi.mock('@/routes/-hooks/useToasts', () => ({
  useToasts: () => ({
    successToast: vi.fn(),
    errorToast: vi.fn(),
  }),
}));

describe('NewLocationModal', () => {
  const defaultProps = {
    open: true,
    close: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render the modal when open is true', () => {
      renderWithTheme(<NewLocationModal {...defaultProps} />);
      expect(screen.getByText('location.new.title')).toBeInTheDocument();
    });

    it('should render name input field', () => {
      renderWithTheme(<NewLocationModal {...defaultProps} />);
      expect(screen.getByRole('textbox')).toBeInTheDocument();
    });

    it('should render cancel button', () => {
      renderWithTheme(<NewLocationModal {...defaultProps} />);
      expect(screen.getByRole('button', { name: 'common.cancel' })).toBeInTheDocument();
    });

    it('should render save button', () => {
      renderWithTheme(<NewLocationModal {...defaultProps} />);
      expect(screen.getByRole('button', { name: 'common.save' })).toBeInTheDocument();
    });

    it('should not render when open is false', () => {
      renderWithTheme(<NewLocationModal {...defaultProps} open={false} />);
      expect(screen.queryByText('location.new.title')).not.toBeInTheDocument();
    });
  });

  describe('User Interactions', () => {
    it('should call close when cancel button is clicked', async () => {
      const user = userEvent.setup();
      const closeMock = vi.fn();
      renderWithTheme(<NewLocationModal {...defaultProps} close={closeMock} />);

      await user.click(screen.getByRole('button', { name: 'common.cancel' }));

      expect(closeMock).toHaveBeenCalledTimes(1);
    });

    it('should allow typing in the name input', async () => {
      const user = userEvent.setup();
      renderWithTheme(<NewLocationModal {...defaultProps} />);

      const input = screen.getByRole('textbox');
      await user.type(input, 'Living Room');

      expect(input).toHaveValue('Living Room');
    });

    it('should call saveLocation when form is submitted with valid data', async () => {
      const user = userEvent.setup();
      mockSaveLocation.mockResolvedValue(undefined);
      renderWithTheme(<NewLocationModal {...defaultProps} />);

      const input = screen.getByRole('textbox');
      await user.type(input, 'Living Room');

      await user.click(screen.getByRole('button', { name: 'common.save' }));

      await waitFor(() => {
        expect(mockSaveLocation).toHaveBeenCalledWith({ name: 'Living Room' });
      });
    });
  });

  describe('Form State', () => {
    it('should start with empty name field', () => {
      renderWithTheme(<NewLocationModal {...defaultProps} />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('');
    });
  });

  describe('Validation Errors', () => {
    it('should not call saveLocation when form is submitted with empty name', async () => {
      const user = userEvent.setup();
      mockSaveLocation.mockResolvedValue(undefined);
      renderWithTheme(<NewLocationModal {...defaultProps} />);

      // Submit without entering any data
      await user.click(screen.getByRole('button', { name: 'common.save' }));

      // saveLocation should not be called when validation fails
      await waitFor(() => {
        expect(mockSaveLocation).not.toHaveBeenCalled();
      });
    });
  });
});
