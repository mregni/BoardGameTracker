import { DeepMap, FieldError, FieldValues, Path, UseFormRegister } from 'react-hook-form';
import { ComponentPropsWithoutRef } from 'react';
import { cx } from 'class-variance-authority';
import { TextArea } from '@radix-ui/themes';

export interface Props<T extends FieldValues> extends ComponentPropsWithoutRef<'div'> {
  name: Path<T>;
  register?: UseFormRegister<T>;
  errors?: Partial<DeepMap<T, FieldError>>;
  disabled?: boolean;
  label: string;
}

export const BgtTextArea = <T extends FieldValues>(props: Props<T>) => {
  const { name, register, disabled = false, className, label } = props;
  return (
    <div className="flex flex-col">
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
      </div>
      <div className="bg-input rounded-lg p-2">
        <TextArea
          className={cx(className, 'bg-transparent shadow-none focus-within:outline-none')}
          rows={4}
          disabled={disabled}
          {...register?.(name)}
        />
      </div>
    </div>
  );
};
