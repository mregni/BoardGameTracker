import { Control, FieldValues, Path, useController, UseFormRegister } from 'react-hook-form';
import clsx from 'clsx';
import { TextField } from '@radix-ui/themes';
 
import { BgtFormErrors } from './BgtFormErrors';

export interface Props<T extends FieldValues> {
  type: 'text' | 'number' | 'date';
  placeholder?: string;
  name: Path<T>;
  register?: UseFormRegister<T>;
  control: Control<T>;
  valueAsNumber?: boolean;
  label?: string;
  prefixLabel?: string;
  className?: string;
  disabled?: boolean;
}

export const BgtInputField = <T extends FieldValues>(props: Props<T>) => {
  const { type, placeholder = '', name, valueAsNumber, label, register, control, prefixLabel = null, className = '', disabled = false } = props;

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
      <TextField.Root className={clsx(className, (type === 'number' || type === 'date') && 'pr-2')}>
        {prefixLabel && <TextField.Slot>{prefixLabel}</TextField.Slot>}
        <TextField.Input
          disabled={disabled}
          type={type}
          radius="large"
          defaultValue=""
          placeholder={placeholder}
          {...register?.(name, { valueAsNumber })}
        />
      </TextField.Root>
      {!label && (
        <div className="flex items-baseline">
          <BgtFormErrors error={error} />
        </div>
      )}
    </div>
  );
};
