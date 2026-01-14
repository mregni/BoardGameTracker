import { memo, useCallback } from 'react';
import * as Checkbox from '@radix-ui/react-checkbox';

import CheckIcon from '@/assets/icons/check.svg?react';

interface Props {
  id: string;
  label: string;
  disabled?: boolean;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
}

const BgtSimpleCheckboxComponent = (props: Props) => {
  const { id, label, disabled = false, checked, onCheckedChange } = props;

  const handleCheckedChange = useCallback(
    (checkedState: boolean | 'indeterminate') => {
      if (checkedState !== 'indeterminate') {
        onCheckedChange(checkedState);
      }
    },
    [onCheckedChange]
  );

  return (
    <div className="flex items-center space-x-2">
      <Checkbox.Root
        className="flex h-4 w-4 appearance-none items-center justify-center rounded-xs border border-gray-300 bg-card-border data-[state=checked]:bg-primary data-[state=checked]:border-primary/60 focus:outline-hidden focus:ring-0 disabled:cursor-not-allowed disabled:opacity-50"
        id={id}
        checked={checked}
        onCheckedChange={handleCheckedChange}
        disabled={disabled}
      >
        <Checkbox.Indicator className="text-white">
          <CheckIcon className="size-3" />
        </Checkbox.Indicator>
      </Checkbox.Root>
      <label
        htmlFor={id}
        className={`text-sm font-medium leading-none cursor-pointer ${disabled ? 'cursor-not-allowed opacity-50' : ''}`}
      >
        {label}
      </label>
    </div>
  );
};

BgtSimpleCheckboxComponent.displayName = 'BgtSimpleCheckbox';

export const BgtSimpleCheckbox = memo(BgtSimpleCheckboxComponent);
