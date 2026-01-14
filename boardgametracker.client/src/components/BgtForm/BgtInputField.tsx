import { memo } from 'react';
import { format } from 'date-fns';
import { cx } from 'class-variance-authority';
import { AnyFieldApi } from '@tanstack/react-form';

import { BgtText } from '../BgtText/BgtText';

import { FormFieldWrapper } from './FormFieldWrapper';

export interface BgtInputFieldProps {
  field: AnyFieldApi;
  type: 'text' | 'number' | 'datetime-local' | 'date';
  placeholder?: string;
  label?: string;
  prefixLabel?: string;
  suffixLabel?: string;
  className?: string;
  disabled?: boolean;
}

const formatDateToLocalInput = (date: Date) => {
  try {
    return format(date, "yyyy-MM-dd'T'HH:mm");
  } catch {
    return '';
  }
};

const formatInput = (
  input: string | number | Date | undefined,
  type: 'text' | 'number' | 'datetime-local' | 'date'
) => {
  if (input === undefined || input === null || input === '') {
    return '';
  }

  if (type === 'datetime-local') {
    return formatDateToLocalInput(new Date(input));
  }
  if (type === 'date') {
    try {
      return format(new Date(input), 'yyyy-MM-dd');
    } catch {
      return '';
    }
  }

  return String(input);
};

const BgtInputFieldComponent = (props: BgtInputFieldProps) => {
  const {
    field,
    type,
    placeholder = '',
    label,
    prefixLabel = undefined,
    suffixLabel = undefined,
    className = '',
    disabled = false,
  } = props;

  const hasErrors = field.state.meta.errors.length > 0;

  return (
    <FormFieldWrapper label={label} errors={field.state.meta.errors} className="w-full">
      <div
        className={cx(
          'rounded-lg active:border-none flex flex-row gap-2 items-center text-[12px]',
          className,
          hasErrors && 'border border-error bg-error/5!'
        )}
      >
        {prefixLabel && <BgtText color="white">{prefixLabel}</BgtText>}
        <input
          className="w-full bg-background font- text-white px-4 py-3 rounded-lg border border-primary/30 focus:border-primary focus:outline-none"
          value={formatInput(field.state.value, type)}
          disabled={disabled}
          type={type}
          onChange={(event) => {
            const value = type === 'number' ? +event.target.value : event.target.value;
            field.handleChange(value);
          }}
          onBlur={field.handleBlur}
          placeholder={placeholder.toUpperCase()}
        />
        {suffixLabel && <BgtText color="white">{suffixLabel}</BgtText>}
      </div>
    </FormFieldWrapper>
  );
};

BgtInputFieldComponent.displayName = 'BgtInputField';

export const BgtInputField = memo(BgtInputFieldComponent);
