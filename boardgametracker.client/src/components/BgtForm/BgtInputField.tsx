import { Control, Controller, FieldValues, Form, Path, useController, UseFormRegister } from 'react-hook-form';
import { formatISO } from 'date-fns';
import clsx from 'clsx';
import { TextField } from '@radix-ui/themes';

import { BgtFormErrors } from './BgtFormErrors';

export interface Props<T extends FieldValues> {
  type: 'text' | 'number' | 'datetime-local';
  name: Path<T>;
  placeholder?: string;
  register?: UseFormRegister<T>;
  control: Control<T>;
  valueAsNumber?: boolean;
  label?: string;
  prefixLabel?: string;
  className?: string;
  disabled?: boolean;
}

export const BgtInputField = <T extends FieldValues>(props: Props<T>) => {
  const { type, name, placeholder = '', valueAsNumber, label, register, control, prefixLabel = null, className = '', disabled = false } = props;

  const {
    fieldState: { error },
  } = useController({ name, control });

  return (
    <div className="grid gap-1">
      {label && (
        <div className="flex items-baseline justify-between">
          <div className="text-[15px] font-medium leading-[35px]">{label}</div>
          <BgtFormErrors error={error} />
        </div>
      )}
      <Controller
        name={name}
        control={control}
        render={({ field }) => (
          <TextField.Root
            className={clsx(className, (type === 'number' || type === 'datetime-local') && 'pr-2')}
            disabled={disabled}
            type={type}
            onChange={(event) => (valueAsNumber ? field.onChange(+event.target.value) : field.onChange(event.target.value))}
            defaultValue={type === 'datetime-local' ? formatISO(field.value).substring(0, 16) : field.value}
            placeholder={placeholder}
          >
            {prefixLabel && <TextField.Slot>{prefixLabel}</TextField.Slot>}
          </TextField.Root>
        )}
      />
      {!label && (
        <div className="flex items-baseline">
          <BgtFormErrors error={error} />
        </div>
      )}
    </div>
  );
};
