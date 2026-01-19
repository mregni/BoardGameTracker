import { describe, it, expect, vi, beforeEach } from 'vitest';

import { BgtCheckboxList } from './BgtCheckboxList';

import { screen, userEvent, renderWithTheme } from '@/test/test-utils';

vi.mock('@/assets/icons/check.svg?react', () => ({
  default: (props: React.SVGProps<SVGSVGElement>) => <svg data-testid="check-icon" {...props} />,
}));

describe('BgtCheckboxList', () => {
  const defaultItems = [
    { id: 1, value: 'Option 1' },
    { id: 2, value: 'Option 2' },
    { id: 3, value: 'Option 3' },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render all items', () => {
      renderWithTheme(<BgtCheckboxList items={defaultItems} />);

      expect(screen.getByText('Option 1')).toBeInTheDocument();
      expect(screen.getByText('Option 2')).toBeInTheDocument();
      expect(screen.getByText('Option 3')).toBeInTheDocument();
    });

    it('should render correct number of checkboxes', () => {
      renderWithTheme(<BgtCheckboxList items={defaultItems} />);

      const checkboxes = screen.getAllByRole('checkbox');
      expect(checkboxes).toHaveLength(3);
    });

    it('should render empty list when no items provided', () => {
      renderWithTheme(<BgtCheckboxList items={[]} />);

      const checkboxes = screen.queryAllByRole('checkbox');
      expect(checkboxes).toHaveLength(0);
    });

    it('should render with pre-selected items', () => {
      renderWithTheme(<BgtCheckboxList items={defaultItems} selectedIds={[1, 3]} />);

      const checkboxes = screen.getAllByRole('checkbox');
      expect(checkboxes[0]).toBeChecked();
      expect(checkboxes[1]).not.toBeChecked();
      expect(checkboxes[2]).toBeChecked();
    });
  });

  describe('User Interactions', () => {
    it('should check an item when clicked', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtCheckboxList items={defaultItems} />);

      const checkboxes = screen.getAllByRole('checkbox');
      await user.click(checkboxes[0]);

      expect(checkboxes[0]).toBeChecked();
    });

    it('should uncheck an item when clicked twice', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtCheckboxList items={defaultItems} />);

      const checkboxes = screen.getAllByRole('checkbox');
      await user.click(checkboxes[0]);
      await user.click(checkboxes[0]);

      expect(checkboxes[0]).not.toBeChecked();
    });

    it('should call onSelectionChange with correct ids when item is checked', async () => {
      const user = userEvent.setup();
      const onSelectionChange = vi.fn();
      renderWithTheme(<BgtCheckboxList items={defaultItems} onSelectionChange={onSelectionChange} />);

      const checkboxes = screen.getAllByRole('checkbox');
      await user.click(checkboxes[0]);

      expect(onSelectionChange).toHaveBeenCalledWith([1]);
    });

    it('should call onSelectionChange with correct ids when item is unchecked', async () => {
      const user = userEvent.setup();
      const onSelectionChange = vi.fn();
      renderWithTheme(
        <BgtCheckboxList items={defaultItems} selectedIds={[1, 2]} onSelectionChange={onSelectionChange} />
      );

      const checkboxes = screen.getAllByRole('checkbox');
      await user.click(checkboxes[0]);

      expect(onSelectionChange).toHaveBeenCalledWith([2]);
    });

    it('should allow multiple selections', async () => {
      const user = userEvent.setup();
      const onSelectionChange = vi.fn();
      renderWithTheme(<BgtCheckboxList items={defaultItems} onSelectionChange={onSelectionChange} />);

      const checkboxes = screen.getAllByRole('checkbox');
      await user.click(checkboxes[0]);
      await user.click(checkboxes[2]);

      expect(onSelectionChange).toHaveBeenLastCalledWith([1, 3]);
    });
  });

  describe('Disabled State', () => {
    it('should disable all checkboxes when disabled is true', () => {
      renderWithTheme(<BgtCheckboxList items={defaultItems} disabled={true} />);

      const checkboxes = screen.getAllByRole('checkbox');
      checkboxes.forEach((checkbox) => {
        expect(checkbox).toBeDisabled();
      });
    });

    it('should enable all checkboxes when disabled is false', () => {
      renderWithTheme(<BgtCheckboxList items={defaultItems} disabled={false} />);

      const checkboxes = screen.getAllByRole('checkbox');
      checkboxes.forEach((checkbox) => {
        expect(checkbox).not.toBeDisabled();
      });
    });
  });

  describe('Custom Label Rendering', () => {
    it('should use custom renderLabel function', () => {
      const items = [
        { id: 1, value: 'test', customField: 'Custom Label 1' },
        { id: 2, value: 'test', customField: 'Custom Label 2' },
      ];

      renderWithTheme(
        <BgtCheckboxList items={items} renderLabel={(item) => (item as (typeof items)[0]).customField} />
      );

      expect(screen.getByText('Custom Label 1')).toBeInTheDocument();
      expect(screen.getByText('Custom Label 2')).toBeInTheDocument();
    });

    it('should use default value property when no renderLabel provided', () => {
      const items = [
        { id: 1, value: 'Default Value 1' },
        { id: 2, value: 'Default Value 2' },
      ];

      renderWithTheme(<BgtCheckboxList items={items} />);

      expect(screen.getByText('Default Value 1')).toBeInTheDocument();
      expect(screen.getByText('Default Value 2')).toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('should handle empty selectedIds array', () => {
      renderWithTheme(<BgtCheckboxList items={defaultItems} selectedIds={[]} />);

      const checkboxes = screen.getAllByRole('checkbox');
      checkboxes.forEach((checkbox) => {
        expect(checkbox).not.toBeChecked();
      });
    });

    it('should handle selectedIds with non-existent ids', () => {
      renderWithTheme(<BgtCheckboxList items={defaultItems} selectedIds={[999]} />);

      const checkboxes = screen.getAllByRole('checkbox');
      checkboxes.forEach((checkbox) => {
        expect(checkbox).not.toBeChecked();
      });
    });

    it('should work without onSelectionChange callback', async () => {
      const user = userEvent.setup();
      renderWithTheme(<BgtCheckboxList items={defaultItems} />);

      const checkboxes = screen.getAllByRole('checkbox');
      await user.click(checkboxes[0]);

      expect(checkboxes[0]).toBeChecked();
    });
  });
});
