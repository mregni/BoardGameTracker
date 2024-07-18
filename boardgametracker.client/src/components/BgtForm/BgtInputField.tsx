import { Control, Controller, FieldValues, Path, useController } from 'react-hook-form';
import { format } from 'date-fns';
import clsx from 'clsx';

import { BgtText } from '../BgtText/BgtText';

import { BgtFormErrors } from './BgtFormErrors';

export interface Props<T extends FieldValues> {
  type: 'text' | 'number' | 'datetime-local' | 'date';
  name: Path<T>;
  placeholder?: string;
  control: Control<T>;
  valueAsNumber?: boolean;
  label?: string;
  prefixLabel?: string;
  className?: string;
  disabled?: boolean;
}

const formatDateToLocalInput = (date: Date) => {
  try {
    return format(date, "yyyy-MM-dd'T'HH:mm");
  } catch (error) {
    return '';
  }
};

export const BgtInputField = <T extends FieldValues>(props: Props<T>) => {
  const {
    type,
    name,
    placeholder = '',
    valueAsNumber,
    label,
    control,
    prefixLabel = undefined,
    className = '',
    disabled = false,
  } = props;

  const {
    fieldState: { error },
  } = useController({ name, control });

  return (
    <div className="flex flex-col justify-start w-full">
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
        {<BgtFormErrors error={error} />}
      </div>
      <Controller
        name={name}
        control={control}
        render={({ field }) => (
          <div
            className={clsx(
              'rounded-lg bg-input active:border-none px-4 flex flex-row gap-2 items-center text-[12px]',
              className,
              error && 'border border-red-600 !bg-error-dark'
            )}
          >
            {prefixLabel && <BgtText>{prefixLabel}</BgtText>}
            <input
              className="h-[45px] bg-transparent shadow-none focus:outline-none hide-arrow w-full"
              disabled={disabled}
              type={type}
              onChange={(event) =>
                valueAsNumber ? field.onChange(+event.target.value) : field.onChange(event.target.value)
              }
              defaultValue={type === 'datetime-local' ? formatDateToLocalInput(field.value) : field.value}
              placeholder={placeholder.toUpperCase()}
            />
          </div>
        )}
      />
    </div>
  );
};
