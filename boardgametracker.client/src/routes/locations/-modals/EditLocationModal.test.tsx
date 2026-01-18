import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, userEvent, renderWithTheme } from '@/test/test-utils';

import { EditLocationModal } from './EditLocationModal';
import { Location } from '@/models';

// i18next is mocked globally in setup.ts

const mockUpdateLocation = vi.fn();

vi.mock('../-hooks/useLocationModal', () => ({
  useLocationModal: () => ({
    updateLocation: mockUpdateLocation,
    isLoading: false,
  }),
}));

vi.mock('@/routes/-hooks/useToasts', () => ({
  useToasts: () => ({
    successToast: vi.fn(),
    errorToast: vi.fn(),
  }),
}));

describe('EditLocationModal', () => {
  const mockLocation: Location = {
    id: 1,
    name: 'Living Room',
  };

  const defaultProps = {
    open: true,
    close: vi.fn(),
    location: mockLocation,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render the modal when open is true', () => {
      renderWithTheme(<EditLocationModal {...defaultProps} />);
      expect(screen.getByText('location.edit.title')).toBeInTheDocument();
    });

    it('should render name input field with existing value', () => {
      renderWithTheme(<EditLocationModal {...defaultProps} />);
      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('Living Room');
    });

    it('should render cancel button', () => {
      renderWithTheme(<EditLocationModal {...defaultProps} />);
      expect(screen.getByRole('button', { name: 'common.cancel' })).toBeInTheDocument();
    });

    it('should render save button', () => {
      renderWithTheme(<EditLocationModal {...defaultProps} />);
      expect(screen.getByRole('button', { name: 'common.save' })).toBeInTheDocument();
    });

    it('should not render when open is false', () => {
      renderWithTheme(<EditLocationModal {...defaultProps} open={false} />);
      expect(screen.queryByText('location.edit.title')).not.toBeInTheDocument();
    });
  });

  describe('User Interactions', () => {
    it('should call close when cancel button is clicked', async () => {
      const user = userEvent.setup();
      const closeMock = vi.fn();
      renderWithTheme(<EditLocationModal {...defaultProps} close={closeMock} />);

      await user.click(screen.getByRole('button', { name: 'common.cancel' }));

      expect(closeMock).toHaveBeenCalledTimes(1);
    });

    it('should allow editing the name', async () => {
      const user = userEvent.setup();
      renderWithTheme(<EditLocationModal {...defaultProps} />);

      const input = screen.getByRole('textbox');
      await user.clear(input);
      await user.type(input, 'Kitchen');

      expect(input).toHaveValue('Kitchen');
    });

    it('should call updateLocation when form is submitted with valid data', async () => {
      const user = userEvent.setup();
      mockUpdateLocation.mockResolvedValue(undefined);
      renderWithTheme(<EditLocationModal {...defaultProps} />);

      const input = screen.getByRole('textbox');
      await user.clear(input);
      await user.type(input, 'Kitchen');

      await user.click(screen.getByRole('button', { name: 'common.save' }));

      await waitFor(() => {
        expect(mockUpdateLocation).toHaveBeenCalledWith({
          id: 1,
          name: 'Kitchen',
        });
      });
    });
  });

  describe('Pre-populated Data', () => {
    it('should populate form with existing location data', () => {
      const location: Location = {
        id: 42,
        name: 'Basement',
      };
      renderWithTheme(<EditLocationModal {...defaultProps} location={location} />);

      const input = screen.getByRole('textbox');
      expect(input).toHaveValue('Basement');
    });
  });

  describe('Validation Errors', () => {
    it('should not call updateLocation when name is cleared to empty', async () => {
      const user = userEvent.setup();
      mockUpdateLocation.mockResolvedValue(undefined);
      renderWithTheme(<EditLocationModal {...defaultProps} />);

      const input = screen.getByRole('textbox');
      await user.clear(input);

      await user.click(screen.getByRole('button', { name: 'common.save' }));

      await waitFor(() => {
        expect(mockUpdateLocation).not.toHaveBeenCalled();
      });
    });
  });
});
