import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, userEvent, renderWithTheme } from '@/test/test-utils';

import { BgtDeleteModal } from './BgtDeleteModal';

// i18next is mocked globally in setup.ts

describe('BgtDeleteModal', () => {
  const defaultProps = {
    open: true,
    close: vi.fn(),
    onDelete: vi.fn(),
    title: 'Game',
    description: 'Are you sure you want to delete this game?',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render the modal when open is true', () => {
      renderWithTheme(<BgtDeleteModal {...defaultProps} />);
      expect(screen.getByText('Delete Game')).toBeInTheDocument();
    });

    it('should render the description', () => {
      renderWithTheme(<BgtDeleteModal {...defaultProps} />);
      expect(screen.getByText('Are you sure you want to delete this game?')).toBeInTheDocument();
    });

    it('should render cancel and delete buttons', () => {
      renderWithTheme(<BgtDeleteModal {...defaultProps} />);
      expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /delete/i })).toBeInTheDocument();
    });

    it('should not render when open is false', () => {
      renderWithTheme(<BgtDeleteModal {...defaultProps} open={false} />);
      expect(screen.queryByText('Delete Game')).not.toBeInTheDocument();
    });
  });

  describe('Title Interpolation', () => {
    it('should interpolate title correctly for different entities', () => {
      const { rerender } = renderWithTheme(<BgtDeleteModal {...defaultProps} title="Player" />);
      expect(screen.getByText('Delete Player')).toBeInTheDocument();

      rerender(<BgtDeleteModal {...defaultProps} title="Location" />);
      expect(screen.getByText('Delete Location')).toBeInTheDocument();
    });
  });

  describe('User Interactions', () => {
    it('should call close when cancel button is clicked', async () => {
      const user = userEvent.setup();
      const closeMock = vi.fn();
      renderWithTheme(<BgtDeleteModal {...defaultProps} close={closeMock} />);

      await user.click(screen.getByRole('button', { name: /cancel/i }));

      expect(closeMock).toHaveBeenCalledTimes(1);
    });

    it('should call onDelete when delete button is clicked', async () => {
      const user = userEvent.setup();
      const onDeleteMock = vi.fn();
      renderWithTheme(<BgtDeleteModal {...defaultProps} onDelete={onDeleteMock} />);

      await user.click(screen.getByRole('button', { name: /delete/i }));

      expect(onDeleteMock).toHaveBeenCalledTimes(1);
    });

    it('should handle async onDelete', async () => {
      const user = userEvent.setup();
      const onDeleteMock = vi.fn().mockResolvedValue(undefined);
      renderWithTheme(<BgtDeleteModal {...defaultProps} onDelete={onDeleteMock} />);

      await user.click(screen.getByRole('button', { name: /delete/i }));

      expect(onDeleteMock).toHaveBeenCalledTimes(1);
    });
  });

  describe('Custom Content', () => {
    it('should render custom description', () => {
      const customDescription = 'This action cannot be undone. All data will be lost.';
      renderWithTheme(<BgtDeleteModal {...defaultProps} description={customDescription} />);
      expect(screen.getByText(customDescription)).toBeInTheDocument();
    });
  });

  describe('Async Error Handling', () => {
    it('should handle async onDelete rejection gracefully', async () => {
      const user = userEvent.setup();
      const onDeleteMock = vi.fn().mockRejectedValue(new Error('Network error'));
      renderWithTheme(<BgtDeleteModal {...defaultProps} onDelete={onDeleteMock} />);

      await user.click(screen.getByRole('button', { name: /delete/i }));

      // onDelete should have been called
      expect(onDeleteMock).toHaveBeenCalledTimes(1);

      // Modal should still be in the document (error handling is parent's responsibility)
      expect(screen.getByText('Delete Game')).toBeInTheDocument();
    });

    it('should handle slow async onDelete', async () => {
      const user = userEvent.setup();
      const onDeleteMock = vi.fn().mockImplementation(() => new Promise((resolve) => setTimeout(resolve, 100)));
      renderWithTheme(<BgtDeleteModal {...defaultProps} onDelete={onDeleteMock} />);

      await user.click(screen.getByRole('button', { name: /delete/i }));

      expect(onDeleteMock).toHaveBeenCalledTimes(1);
    });
  });
});
