import { memo, useCallback } from 'react';
import { AnyFieldApi } from '@tanstack/react-form';
import { Text } from '@radix-ui/themes';
import * as Switch from '@radix-ui/react-switch';

interface Props {
  field: AnyFieldApi;
  label: string;
  disabled?: boolean;
  className?: string;
}

const BgtSwitchComponent = (props: Props) => {
  const { label, field, disabled = false, className } = props;

  const handleCheckedChange = useCallback(
    (checked: boolean) => {
      field.handleChange(checked);
    },
    [field]
  );

  return (
    <div className={className}>
      <Text as="label" size="3">
        <div className="flex gap-2">
          <Switch.Root
            onCheckedChange={handleCheckedChange}
            disabled={disabled}
            checked={field.state.value}
            className="w-[42px] h-[21px] rounded-full relative data-disabled:bg-primary/80 data-[state=checked]:bg-primary outline-hidden cursor-defaul bg-(--gray-10)"
          >
            <Switch.Thumb className="block w-[21px] h-[21px] -left-0.5 top-0 absolute bg-white rounded-full transition-transform duration-100 translate-x-0.5 will-change-transform data-[state=checked]:translate-x-[23px]" />
          </Switch.Root>
          {label}
        </div>
      </Text>
    </div>
  );
};

BgtSwitchComponent.displayName = 'BgtSwitch';

export const BgtSwitch = memo(BgtSwitchComponent);
