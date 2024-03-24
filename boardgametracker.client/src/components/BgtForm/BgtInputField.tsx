import {DeepMap, FieldError, FieldValues, Path, UseFormRegister} from 'react-hook-form';
import {useTranslation} from 'react-i18next';

import {TextField} from '@radix-ui/themes';

export interface Props<T extends FieldValues> {
  type: 'text' | 'number' | 'date';
  placeholder?: string;
  name: Path<T>;
  register?: UseFormRegister<T>;
  errors?: Partial<DeepMap<T, FieldError>>;
  valueAsNumber?: boolean;
  label: string;
  prefixLabel?: string;
  className?: string;
  disabled?: boolean;
}

export const BgtInputField = <TFormValues extends FieldValues>(props: Props<TFormValues>) => {
  const {
    type,
    placeholder = '',
    name,
    valueAsNumber,
    label,
    register,
    errors,
    prefixLabel = null,
    className = '',
    disabled = false
  } = props;
  const { t } = useTranslation();

  return (
    <div className="grid">
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px]">
          {label}
        </div>
        {
          errors?.[name] && (
            <div className="text-[13px] text-red-500 opacity-[0.8]">
              {t(errors?.[name].message as string)}
            </div>)
        }
      </div>
      <TextField.Root className={className}>
        {
          prefixLabel && (
            <TextField.Slot>
              {prefixLabel}
            </TextField.Slot>
          )
        }
        <TextField.Input
          disabled={disabled}
          type={type}
          radius="large"
          defaultValue=""
          placeholder={placeholder}
          {...register?.(name, { valueAsNumber })}
        />
      </TextField.Root>
    </div>
  )
}