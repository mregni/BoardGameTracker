import { Control, FieldValues, Path, useController } from 'react-hook-form';
import { useState } from 'react';
import { CommandList } from 'cmdk';
import { cx } from 'class-variance-authority';
import { Button } from '@radix-ui/themes';
import { ChevronDownIcon } from '@heroicons/react/24/outline';

import { BgtPopover, BgtPopoverContent, BgtPopoverTrigger } from '../BgtPopover/BgtPopover';
import { BgtIcon } from '../BgtIcon/BgtIcon';
import {
  BgtCommand,
  BgtCommandCreate,
  BgtCommandGroup,
  BgtCommandInput,
  BgtCommandItem,
} from '../BgtCommand/BgtCommand';
import { BgtSelectItem } from '../../models/Common/BgtSelectItem';

import { BgtFormErrors } from './BgtFormErrors';

interface ComboboxProps<T1 extends FieldValues, T2> {
  options: BgtSelectItem[];
  placeholder: string;
  addOptionText: (query: string) => string;
  onChange: (value: BgtSelectItem | undefined) => void;
  label?: string;
  onCreate: (name: string) => Promise<T2>;
  isSaving: boolean;
  getSelectedItem: (value: T2) => BgtSelectItem;
  name: Path<T1>;
  control?: Control<T1>;
  disabled: boolean;
}

export const BgtComboBox = <T1 extends FieldValues, T2>(props: ComboboxProps<T1, T2>) => {
  const {
    options,
    placeholder,
    addOptionText,
    onChange,
    label,
    onCreate,
    isSaving,
    getSelectedItem,
    name,
    control,
    disabled,
  } = props;
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState<string>('');
  const [selected, setSelected] = useState<string>('');

  const {
    fieldState: { error },
  } = useController({ name, control });

  return (
    <div className="flex flex-col justify-start">
      <BgtPopover open={open && !disabled} onOpenChange={setOpen}>
        <div className="flex items-baseline justify-between">
          <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
          {<BgtFormErrors error={error} />}
        </div>
        <BgtPopoverTrigger asChild>
          <Button
            size="2"
            role="combobox"
            aria-expanded={open}
            disabled={disabled}
            className={cx(
              'px-4 py-2 h-[45px] !font-normal uppercase shadow-none bg-input inline-flex justify-between items-center rounded-lg leading-none text-[12px]',
              error && 'outline outline-1 outline-red-600 !bg-error-dark'
            )}
          >
            {selected ? options.find((option) => option.value === selected)?.label ?? selected : placeholder}
            <BgtIcon icon={<ChevronDownIcon />} />
          </Button>
        </BgtPopoverTrigger>

        <BgtPopoverContent>
          <BgtCommand className="max-h-64 overflow-y-auto">
            <BgtCommandInput
              placeholder={placeholder}
              value={query}
              onValueChange={(value: string) => setQuery(value)}
            />
            <CommandList>
              <BgtCommandGroup>
                {options.map((option) => (
                  <BgtCommandItem
                    key={option.value}
                    value={option.label}
                    onSelect={(currentValue: string) => {
                      setSelected(currentValue === selected ? '' : currentValue);
                      setOpen(false);
                      onChange(options.find((option) => option.label === currentValue));
                    }}
                  >
                    {option.label}
                  </BgtCommandItem>
                ))}
              </BgtCommandGroup>
              {query && (
                <BgtCommandCreate
                  isLoading={isSaving}
                  onSelect={() => {
                    onCreate(query)
                      .then((newLocation) => {
                        onChange(getSelectedItem(newLocation));
                        setQuery('');
                        setSelected(query);
                      })
                      .catch(() => setQuery(''))
                      .finally(() => {
                        setOpen(false);
                      });
                  }}
                >
                  {addOptionText(query)}
                </BgtCommandCreate>
              )}
            </CommandList>
          </BgtCommand>
        </BgtPopoverContent>
      </BgtPopover>
    </div>
  );
};
