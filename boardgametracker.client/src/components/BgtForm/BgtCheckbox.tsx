import * as Checkbox from '@radix-ui/react-checkbox';

import CheckIcon from '@/assets/icons/check.svg?react';

interface Props {
  id: string;
  label: string;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled?: boolean;
}

export const BgtCheckbox = (props: Props) => {
  const { id, label, checked, onCheckedChange, disabled = false } = props;
  return (
    <div className="flex items-center space-x-2">
      <Checkbox.Root
        className="flex h-4 w-4 appearance-none items-center justify-center rounded-sm border border-gray-300 bg-card-border data-[state=checked]:bg-primary data-[state=checked]:border-primary-dark focus:outline-none focus:ring-0 disabled:cursor-not-allowed disabled:opacity-50"
        id={id}
        checked={checked}
        onCheckedChange={onCheckedChange}
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
