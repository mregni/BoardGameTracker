import { Control, Controller, FieldValues, Path, useController } from 'react-hook-form';

import { Select } from '@radix-ui/themes';

import { BgtSelectItem } from '../../models/Common/BgtSelectItem';
import { BgtFormErrors } from './BgtFormErrors';

interface Props<T extends FieldValues> {
  defaultValue?: string;
  label: string;
  items: BgtSelectItem[];
  name: Path<T>;
  control?: Control<T>;
  disabled?: boolean;
}

export const BgtSelect = <T extends FieldValues>(props: Props<T>) => {
  const { defaultValue, items, label, control, name, disabled = false } = props;

  const {
    fieldState: { error },
  } = useController({ name, control });

  return (
    <div className="grid">
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px]">{label}</div>
        {<BgtFormErrors error={error} />}
      </div>
      <Controller
        name={name}
        control={control}
        render={({ field }) => (
          <Select.Root disabled={disabled} onValueChange={field.onChange} defaultValue={defaultValue}>
            <Select.Trigger />
            <Select.Content>
              {items.map((item) => (
                <Select.Item value={item.value} key={item.value}>
                  {item.label}
                </Select.Item>
              ))}
            </Select.Content>
          </Select.Root>
        )}
      />
    </div>
  );
};
