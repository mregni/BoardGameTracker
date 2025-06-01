import { useTranslation } from 'react-i18next';
import { Control, Controller, FieldValues, Path, useController } from 'react-hook-form';
import { useEffect, useRef, useState } from 'react';
import { cx } from 'class-variance-authority';
import * as Select from '@radix-ui/react-select';

import { BgtAvatar } from '../BgtAvatar/BgtAvatar';
import { StringToHsl } from '../../utils/stringUtils';
import { BgtSelectImageItem, BgtSelectItem } from '../../models/Common/BgtSelectItem';

import { BgtFormErrors } from './BgtFormErrors';

import SearchIcon from '@/assets/icons/magnifying-glass.svg?react';
import CheckIcon from '@/assets/icons/check.svg?react';
import CaretUpIcon from '@/assets/icons/caret-up.svg?react';
import CaretDownIcon from '@/assets/icons/caret-down.svg?react';

interface Props<T extends FieldValues> {
  label: string;
  items: BgtSelectImageItem[] | BgtSelectItem[];
  name: Path<T>;
  control?: Control<T>;
  disabled?: boolean;
  placeholder?: string;
  defaultValue?: string;
  hasSearch?: boolean;
}

export const BgtSelect = <T extends FieldValues>(props: Props<T>) => {
  const { items, label, control, name, disabled = false, placeholder = null, hasSearch = false } = props;

  const { t } = useTranslation();
  const [searchTerm, setSearchTerm] = useState('');
  const [open, setOpen] = useState(false);
  const searchInputRef = useRef<HTMLInputElement>(null);

  const {
    fieldState: { error },
    field: controllerField,
  } = useController({ name, control });

  const currentValue = controllerField.value?.toString();
  const filteredItems = items.filter((item) => {
    if (item.value === currentValue) {
      return true;
    }
    return item.label.toLowerCase().includes(searchTerm.toLowerCase());
  });

  const isSelectImageItem = (item: BgtSelectImageItem | BgtSelectItem): item is BgtSelectImageItem => {
    return item && typeof item === 'object' && 'image' in item;
  };

  useEffect(() => {
    if (open && searchInputRef.current) {
      setTimeout(() => {
        searchInputRef.current?.focus();
      }, 0);
    }
  }, [open]);

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
            open={open}
            onOpenChange={(isOpen) => {
              setOpen(isOpen);
              if (!isOpen) {
                setSearchTerm('');
              }
            }}
          >
            <Select.Trigger
              className={cx(
                'px-4 py-2 h-[45px] shadow-none bg-input uppercase inline-flex justify-between items-center rounded-lg leading-none text-[12px]',
                error && 'border border-red-600 !bg-error-dark'
              )}
            >
              <Select.Value placeholder={placeholder} />
              <Select.Icon>
                {open ? <CaretUpIcon className="size-5" /> : <CaretDownIcon className="size-5" />}
              </Select.Icon>
            </Select.Trigger>
            <Select.Portal>
              <Select.Content
                className="overflow-hidden bg-input rounded-md w-[var(--radix-select-trigger-width)]"
                position="popper"
                sideOffset={5}
                style={{ bottom: 'auto' }}
                onCloseAutoFocus={(e) => {
                  if (open) {
                    e.preventDefault();
                  }
                }}
              >
                {hasSearch && (
                  <div className="p-2 border-b border-gray-700">
                    <div className="flex items-center px-2 bg-input rounded">
                      <SearchIcon className="size-4 text-gray-400 mr-2" />
                      <input
                        ref={searchInputRef}
                        type="text"
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        placeholder="Search..."
                        className="bg-transparent border-none outline-none py-2 text-sm w-full"
                        onClick={(e) => e.stopPropagation()}
                        onKeyDown={(e) => {
                          e.stopPropagation();
                        }}
                      />
                    </div>
                  </div>
                )}
                <Select.Viewport className="p-1 max-h-[300px]">
                  {filteredItems.length > 0 ? (
                    filteredItems.map((item) => (
                      <Select.Item
                        value={item.value}
                        key={item.value}
                        className="text-[13px] leading-none rounded-lg h-[45px] flex items-center pr-[35px] pl-[25px] relative select-none data-[disabled]:text-mauve8 data-[disabled]:pointer-events-none data-[highlighted]:outline-none data-[highlighted]:bg-primary-dark hover:cursor-pointer"
                      >
                        <Select.ItemText>
                          <div className="flex flex-row justify-start items-center gap-2">
                            {isSelectImageItem(item) && (
                              <BgtAvatar title={item.label} image={item.image} color={StringToHsl(item.label)} />
                            )}
                            {item.label}
                          </div>
                        </Select.ItemText>
                        <Select.ItemIndicator className="absolute left-0 w-[25px] inline-flex items-center justify-center">
                          <CheckIcon className="size-4" />
                        </Select.ItemIndicator>
                      </Select.Item>
                    ))
                  ) : (
                    <div className="text-[13px] py-2 px-4 text-gray-400">{t('common.no-results')}</div>
                  )}
                </Select.Viewport>
              </Select.Content>
            </Select.Portal>
          </Select.Root>
        )}
      />
    </div>
  );
};
