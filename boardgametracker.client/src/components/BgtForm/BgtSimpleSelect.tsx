import { useTranslation } from 'react-i18next';
import { useEffect, useRef, useState } from 'react';
import { cx } from 'class-variance-authority';
import * as Select from '@radix-ui/react-select';

import { BgtAvatar } from '../BgtAvatar/BgtAvatar';

import { StringToHsl } from '@/utils/stringUtils';
import { BgtSelectImageItem, BgtSelectItem } from '@/models';
import SearchIcon from '@/assets/icons/magnifying-glass.svg?react';
import CheckIcon from '@/assets/icons/check.svg?react';
import CaretUpIcon from '@/assets/icons/caret-up.svg?react';
import CaretDownIcon from '@/assets/icons/caret-down.svg?react';

interface Props {
  label?: string;
  items: BgtSelectImageItem[] | BgtSelectItem[];
  disabled?: boolean;
  placeholder?: string;
  hasSearch?: boolean;
  value?: string | number | null;
  onValueChange?: (value: string | number) => void;
  className?: string;
}

export const BgtSimpleSelect = (props: Props) => {
  const {
    items,
    label,
    disabled = false,
    placeholder = null,
    hasSearch = false,
    value,
    onValueChange,
    className = '',
  } = props;

  const { t } = useTranslation();
  const [searchTerm, setSearchTerm] = useState('');
  const [open, setOpen] = useState(false);
  const searchInputRef = useRef<HTMLInputElement>(null);

  const currentValue = value?.toString();
  const filteredItems = items.filter((item) => {
    if (item.value === value) {
      return true;
    }
    return item.label.toLowerCase().includes(searchTerm.toLowerCase());
  });

  const isSelectImageItem = (item: BgtSelectImageItem | BgtSelectItem): item is BgtSelectImageItem => {
    return item && typeof item === 'object' && 'image' in item && item.image !== null;
  };

  useEffect(() => {
    if (open && searchInputRef.current) {
      setTimeout(() => {
        searchInputRef.current?.focus();
      }, 0);
    }
  }, [open]);

  useEffect(() => {
    const onResize = (event: Event) => {
      event.stopImmediatePropagation();
    };
    window.addEventListener('resize', onResize);
    return () => {
      window.removeEventListener('resize', onResize);
    };
  }, []);

  return (
    <div className={cx('flex flex-col justify-start', className)}>
      {label && <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>}
      <Select.Root
        disabled={disabled}
        onValueChange={onValueChange}
        value={currentValue}
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
            'w-full bg-background font- text-white rounded-lg border border-primary/30 focus:border-primary focus:outline-none',
            'px-4 py-2 h-[45px] shadow-none uppercase inline-flex justify-between items-center rounded-lg leading-none text-[12px] gap-1'
          )}
        >
          <Select.Value placeholder={placeholder} />
          <Select.Icon>{open ? <CaretUpIcon className="size-5" /> : <CaretDownIcon className="size-5" />}</Select.Icon>
        </Select.Trigger>
        <Select.Portal>
          <Select.Content
            className="overflow-hidden bg-input rounded-md w-radix-select-trigger-width"
            position="popper"
            sideOffset={5}
            style={{ bottom: 'auto' }}
            onCloseAutoFocus={(e) => {
              e.preventDefault();
            }}
          >
            {hasSearch && (
              <div className="p-2 border-b border-gray-700">
                <div className="flex items-center px-2 bg-input rounded-sm">
                  <SearchIcon className="size-4 text-gray-400 mr-2" />
                  <input
                    ref={searchInputRef}
                    type="text"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    placeholder="Search..."
                    className="bg-transparent border-none outline-hidden py-2 text-sm w-full"
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
                    value={item.value.toString()}
                    key={item.value}
                    className="text-[13px] leading-none rounded-lg h-[45px] flex items-center pr-[35px] pl-[25px] relative select-none data-[disabled]:text-mauve8 data-disabled:pointer-events-none data-highlighted:outline-hidden data-highlighted:bg-primary/60 hover:cursor-pointer"
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
    </div>
  );
};
