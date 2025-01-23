import { Control, Controller, FieldValues, Path, useController } from 'react-hook-form';
import { cx } from 'class-variance-authority';
import * as Select from '@radix-ui/react-select';

import { BgtAvatar } from '../BgtAvatar/BgtAvatar';
import { StringToHsl } from '../../utils/stringUtils';
import { BgtSelectImageItem } from '../../models/Common/BgtSelectItem';

import { BgtFormErrors } from './BgtFormErrors';

import CheckIcon from '@/assets/icons/check.svg?react';
import CaretDownIcon from '@/assets/icons/caret-down.svg?react';

interface Props<T extends FieldValues> {
  label: string;
  items: BgtSelectImageItem[];
  name: Path<T>;
  control?: Control<T>;
  disabled?: boolean;
  placeholder?: string;
  hasAvatars?: boolean;
}

export const BgtSelect = <T extends FieldValues>(props: Props<T>) => {
  const { items, label, control, name, disabled = false, placeholder = null, hasAvatars = false } = props;

  const {
    fieldState: { error },
  } = useController({ name, control });

  return (
    <div className="flex flex-col justify-start">
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
        {<BgtFormErrors error={error} />}
      </div>
      <Controller
        name={name}
        control={control}
        render={({ field }) => (
          <Select.Root
            disabled={disabled}
            onValueChange={field.onChange}
            value={field.value?.toString()}
            defaultValue={control?._defaultValues[name]}
          >
            <Select.Trigger
              className={cx(
                'px-4 py-2 h-[45px] shadow-none bg-input uppercase inline-flex justify-between items-center rounded-lg leading-none text-[12px]',
                error && 'border border-red-600 !bg-error-dark'
              )}
            >
              <Select.Value placeholder={placeholder} />
              <Select.Icon>
                <CaretDownIcon className="size-5" />
              </Select.Icon>
            </Select.Trigger>
            <Select.Portal>
              <Select.Content className="overflow-hidden bg-input rounded-md">
                <Select.Viewport className="p-1">
                  {items.map((item) => (
                    <Select.Item
                      value={item.value}
                      key={item.value}
                      className="text-[13px] leading-none rounded-lg h-[45px] flex items-center pr-[35px] pl-[25px] relative select-none data-[disabled]:text-mauve8 data-[disabled]:pointer-events-none data-[highlighted]:outline-none data-[highlighted]:bg-primary-dark hover:cursor-pointer"
                    >
                      <Select.ItemText>
                        <div className="flex flex-row justify-start items-center gap-2">
                          {hasAvatars && (
                            <BgtAvatar title={item.label} image={item.image} color={StringToHsl(item.label)} />
                          )}
                          {item.label}
                        </div>
                      </Select.ItemText>
                      <Select.ItemIndicator className="absolute left-0 w-[25px] inline-flex items-center justify-center">
                        <CheckIcon className="size-4" />
                      </Select.ItemIndicator>
                    </Select.Item>
                  ))}
                </Select.Viewport>
              </Select.Content>
            </Select.Portal>
          </Select.Root>
        )}
      />
    </div>
  );
};
