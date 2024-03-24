import {Control, Controller, DeepMap, FieldError, FieldValues, Path} from 'react-hook-form';

import {Select} from '@radix-ui/themes';

export interface BgtItem {
  value: string;
  label: string;
}

interface Props<T extends FieldValues> {
  defaultValue: string;
  label: string;
  items: BgtItem[];
  name: Path<T>;
  control?: Control<T>;
  errors?: Partial<DeepMap<T, FieldError>>;
  disabled?: boolean;
}

export const BgtSelect = <TFormValues extends FieldValues>(props: Props<TFormValues>) => {
  const { defaultValue, items, label, control, errors, name, disabled = false } = props;

  return (
    <div className="grid">
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px]">
          {label}
        </div>
        {
          errors?.[name] && (
            <div className="text-[13px] text-red-500 opacity-[0.8]">
              {errors?.[name].message}
            </div>)
        }
      </div>
      <Controller
        name={name}
        control={control}
        render={({ field }) => (
          <Select.Root
            disabled={disabled}
            onValueChange={field.onChange}
            defaultValue={defaultValue}
          >
            <Select.Trigger />
            <Select.Content>
              {items.map(item => <Select.Item value={item.value} key={item.value}>{item.label}</Select.Item>)}
            </Select.Content>
          </Select.Root>
        )}
      />
    </div>
  )
}