import { ComponentPropsWithoutRef } from 'react';
import { DeepMap, FieldError, FieldValues, Path, UseFormRegister } from 'react-hook-form';

import { TextArea } from '@radix-ui/themes';

export interface Props<T extends FieldValues> extends ComponentPropsWithoutRef<'div'> {
  name: Path<T>;
  register?: UseFormRegister<T>;
  errors?: Partial<DeepMap<T, FieldError>>;
  disabled: boolean;
}

export const BgtTextArea = <T extends FieldValues>(props: Props<T>) => {
  const { name, register, disabled, className } = props;
  return <TextArea className={className} disabled={disabled} {...register?.(name)} />;
};
