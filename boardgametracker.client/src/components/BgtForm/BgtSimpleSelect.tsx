import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { cx } from 'class-variance-authority';
import * as Select from '@radix-ui/react-select';

import { BgtSelectImageItem, BgtSelectItem } from '@/models';
import CheckIcon from '@/assets/icons/check.svg?react';
import CaretUpIcon from '@/assets/icons/caret-up.svg?react';
import CaretDownIcon from '@/assets/icons/caret-down.svg?react';

interface Props {
  label?: string;
  items: BgtSelectImageItem[] | BgtSelectItem[];
  disabled?: boolean;
  placeholder?: string;
  value: string | undefined;
  onChange: (value: string | null) => void;
}

export const BgtSimpleSelect = (props: Props) => {
  const { items, label, disabled = false, placeholder = null, value, onChange } = props;

  const { t } = useTranslation();
  const [open, setOpen] = useState(false);

  return (
    <div className={cx('flex flex-col justify-start', disabled && 'text-gray-500')}>
      {label && (
        <div className="flex items-baseline justify-between">
          <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
        </div>
      )}
      <Select.Root
        disabled={disabled}
        onValueChange={onChange}
        value={value}
        open={open}
        onOpenChange={(isOpen) => setOpen(isOpen)}
      >
        <Select.Trigger className="px-4 py-2 h-[45px] shadow-none bg-input uppercase inline-flex justify-between items-center rounded-lg leading-none text-[12px]">
          <Select.Value placeholder={placeholder} />
          <Select.Icon>{open ? <CaretUpIcon className="size-5" /> : <CaretDownIcon className="size-5" />}</Select.Icon>
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
            <Select.Viewport className="p-1 max-h-[300px]">
              {items.length > 0 ? (
                items.map((item) => (
                  <Select.Item
                    value={item.value}
                    key={item.value}
                    className="text-[13px] leading-none rounded-lg h-[45px] flex items-center pr-[35px] pl-[25px] relative select-none data-[disabled]:text-mauve8 data-[disabled]:pointer-events-none data-[highlighted]:outline-none data-[highlighted]:bg-primary-dark hover:cursor-pointer"
                  >
                    <Select.ItemText>
                      <div className="flex flex-row justify-start items-center gap-2">{item.label}</div>
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
    </div>
  );
};
