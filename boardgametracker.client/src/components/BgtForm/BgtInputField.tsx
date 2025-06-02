import { Control, Controller, FieldValues, Path, PathValue, useController } from 'react-hook-form';
import { format } from 'date-fns';
import { cx } from 'class-variance-authority';

import { BgtText } from '../BgtText/BgtText';

import { BgtFormErrors } from './BgtFormErrors';

export interface Props<T extends FieldValues> {
  type: 'text' | 'number' | 'datetime-local' | 'date';
  name: Path<T>;
  placeholder?: string;
  control: Control<T>;
  valueAsNumber?: boolean;
  defaultValue?: PathValue<T, Path<T>>;
  label?: string;
  prefixLabel?: string;
  suffixLabel?: string;
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

const formatInput = (input: string | undefined, type: 'text' | 'number' | 'datetime-local' | 'date') => {
  if (input === undefined || input === null) {
    return '';
  }

  if (type === 'datetime-local') {
    return formatDateToLocalInput(new Date(input));
  }
  if (type === 'date') {
    return format(new Date(input), 'yyyy-MM-dd');
  }

  return input;
};

export const BgtInputField = <T extends FieldValues>(props: Props<T>) => {
  const {
    type,
    name,
    placeholder = '',
    valueAsNumber,
    label,
    defaultValue,
    control,
    prefixLabel = undefined,
    suffixLabel = undefined,
    className = '',
    disabled = false,
  } = props;

  const {
    fieldState: { error },
  } = useController({ name, control, defaultValue });

  return (
    <div className="flex flex-col justify-start w-full">
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
        {<BgtFormErrors error={error} />}
      </div>
      <Controller
        name={name}
        control={control}
        defaultValue={defaultValue}
        render={({ field }) => (
          <div
            className={cx(
              'rounded-lg bg-input active:border-none px-4 flex flex-row gap-2 items-center text-[12px]',
              className,
              error && 'border border-red-600 !bg-error-dark'
            )}
          >
            {prefixLabel && <BgtText>{prefixLabel}</BgtText>}
            <input
              className="h-[45px] bg-transparent shadow-none focus:outline-none hide-arrow w-full"
              value={formatInput(field.value, type)}
              disabled={disabled}
              type={type}
              onChange={(event) =>
                valueAsNumber ? field.onChange(+event.target.value) : field.onChange(event.target.value)
              }
              placeholder={placeholder.toUpperCase()}
            />
            {suffixLabel && <BgtText>{suffixLabel}</BgtText>}
          </div>
        )}
      />
    </div>
  );
};
