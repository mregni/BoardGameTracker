import { describe, it, expect, vi } from 'vitest';

import { BgtEditDeleteButtons } from './BgtEditDeleteButtons';

import { screen, userEvent, renderWithTheme } from '@/test/test-utils';

vi.mock('@/assets/icons/trash.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="trash-icon" {...props} />,
}));

vi.mock('@/assets/icons/pencil.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="pencil-icon" {...props} />,
}));

describe('BgtEditDeleteButtons', () => {
  const defaultProps = {
    onDelete: vi.fn(),
    onEdit: vi.fn(),
  };

  describe('Rendering', () => {
    it('should render two buttons', () => {
      renderWithTheme(<BgtEditDeleteButtons {...defaultProps} />);
      const buttons = screen.getAllByRole('button');
      expect(buttons).toHaveLength(2);
    });

    it('should render edit icon', () => {
      renderWithTheme(<BgtEditDeleteButtons {...defaultProps} />);
      expect(screen.getByTestId('pencil-icon')).toBeInTheDocument();
    });

    it('should render delete icon', () => {
      renderWithTheme(<BgtEditDeleteButtons {...defaultProps} />);
      expect(screen.getByTestId('trash-icon')).toBeInTheDocument();
    });
  });

  describe('Click Handlers', () => {
    it('should call onEdit when edit button is clicked', async () => {
      const user = userEvent.setup();
      const handleEdit = vi.fn();
      renderWithTheme(<BgtEditDeleteButtons {...defaultProps} onEdit={handleEdit} />);

      const editButton = screen.getAllByRole('button')[0];
      await user.click(editButton);

      expect(handleEdit).toHaveBeenCalledTimes(1);
    });

    it('should call onDelete when delete button is clicked', async () => {
      const user = userEvent.setup();
      const handleDelete = vi.fn();
      renderWithTheme(<BgtEditDeleteButtons {...defaultProps} onDelete={handleDelete} />);

      const deleteButton = screen.getAllByRole('button')[1];
      await user.click(deleteButton);

      expect(handleDelete).toHaveBeenCalledTimes(1);
    });

    it('should not call wrong handler when clicking', async () => {
      const user = userEvent.setup();
      const handleEdit = vi.fn();
      const handleDelete = vi.fn();
      renderWithTheme(<BgtEditDeleteButtons onEdit={handleEdit} onDelete={handleDelete} />);

      await user.click(screen.getAllByRole('button')[0]);
      expect(handleEdit).toHaveBeenCalled();
      expect(handleDelete).not.toHaveBeenCalled();

      handleEdit.mockClear();
      handleDelete.mockClear();

      await user.click(screen.getAllByRole('button')[1]);
      expect(handleDelete).toHaveBeenCalled();
      expect(handleEdit).not.toHaveBeenCalled();
    });
  });
});
