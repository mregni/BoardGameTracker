import { ChangeEventHandler } from 'react';
import { format } from 'date-fns';
import { cx } from 'class-variance-authority';

import { BgtText } from '../BgtText/BgtText';

export const formatInput = (input: string | number | Date | undefined) => {
  if (input === undefined || input === null) {
    return '';
  }

  if (input instanceof Date) {
    return format(input, 'yyyy-MM-dd');
  }

  return input;
};

export interface Props {
  type: 'text' | 'number' | 'date';
  placeholder?: string;
  label?: string;
  value: string | number | Date | undefined;
  prefixLabel?: string;
  suffixLabel?: string;
  className?: string;
  disabled?: boolean;
  onChange: ChangeEventHandler<HTMLInputElement> | undefined;
}

export const BgtSimpleInputField = (props: Props) => {
  const {
    type,
    placeholder = '',
    value,
    label = undefined,
    prefixLabel = undefined,
    suffixLabel = undefined,
    className = '',
    disabled = false,
    onChange,
  } = props;

  return (
    <div className="flex flex-col justify-start w-full">
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
      </div>
      <div
        className={cx(
          'rounded-lg bg-input active:border-none px-4 flex flex-row gap-2 items-center text-[12px]',
          className,
          disabled && 'text-gray-500'
        )}
      >
        {prefixLabel && <BgtText>{prefixLabel}</BgtText>}
        <input
          className="h-[45px] bg-transparent shadow-none focus:outline-none hide-arrow w-full"
          value={formatInput(value)}
          disabled={disabled}
          type={type}
          onChange={onChange}
          placeholder={placeholder.toUpperCase()}
        />
        {suffixLabel && <BgtText>{suffixLabel}</BgtText>}
      </div>
    </div>
  );
};
