import { useState } from 'react';

import { BgtCheckbox } from './BgtCheckbox';

interface ListItem {
  id: number;
  value: string;
}

interface CheckboxListProps<T extends ListItem> {
  items: T[];
  selectedIds?: number[];
  onSelectionChange?: (selectedIds: number[]) => void;
  disabled?: boolean;
  renderLabel?: (item: T) => string;
}

export const BgtCheckboxList = <T extends ListItem>(props: CheckboxListProps<T>) => {
  const { items, selectedIds = [], onSelectionChange, disabled = false, renderLabel = (item) => item.value } = props;
  const [checkedIds, setCheckedIds] = useState<Set<number>>(new Set(selectedIds));

  const handleCheckedChange = (id: number, checked: boolean) => {
    const updatedCheckedIds = new Set(checkedIds);

    if (checked) {
      updatedCheckedIds.add(id);
    } else {
      updatedCheckedIds.delete(id);
    }

    setCheckedIds(updatedCheckedIds);
    onSelectionChange?.(Array.from(updatedCheckedIds));
  };

  return (
    <div className="space-y-3">
      {items.map((item) => (
        <BgtCheckbox
          key={item.id}
          id={`item-${item.id}`}
          label={renderLabel(item)}
          checked={checkedIds.has(item.id)}
          onCheckedChange={(checked) => handleCheckedChange(item.id, checked === true)}
          disabled={disabled}
        />
      ))}
    </div>
  );
};
