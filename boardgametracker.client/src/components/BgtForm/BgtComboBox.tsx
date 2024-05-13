import { Control, FieldValues, Path, useController } from 'react-hook-form';
import { useState } from 'react';
import { CommandList } from 'cmdk';
import { Button, Text } from '@radix-ui/themes';
import { ChevronDownIcon } from '@heroicons/react/24/outline';

import { BgtSelectItem } from '../../models/Common/BgtSelectItem';

import { Popover, PopoverContent, PopoverTrigger } from './Popover';
import { Command, CommandCreate, CommandGroup, CommandInput, CommandItem } from './Command';
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
}

export const BgtComboBox = <T1 extends FieldValues, T2>(props: ComboboxProps<T1, T2>) => {
  const { options, placeholder, addOptionText, onChange, label, onCreate, isSaving, getSelectedItem, name, control } = props;
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState<string>('');
  const [selected, setSelected] = useState<string>('');

  const {
    fieldState: { error },
  } = useController({ name, control });

  return (
    <div className="grid">
      <Popover open={open} onOpenChange={setOpen}>
        <div className="space-y-1">
          {label && <Text>{label}</Text>}
          <PopoverTrigger asChild>
            <Button
              size="2"
              variant="outline"
              role="combobox"
              aria-expanded={open}
              className="w-full min-w-96 flex rt-variant-surface rt-reset rt-SelectTrigger rt-r-size-2 rt-variant-surface !font-normal"
            >
              {selected ? options.find((option) => option.value === selected)?.label ?? selected : placeholder}
              <ChevronDownIcon className="ml-auto size-3 shrink-0 opacity-50" />
            </Button>
          </PopoverTrigger>
        </div>

        <PopoverContent>
          <Command className="max-h-64 overflow-y-auto">
            <CommandInput placeholder={placeholder} value={query} onValueChange={(value: string) => setQuery(value)} />
            <CommandList>
              <CommandGroup>
                {options.map((option) => (
                  <CommandItem
                    key={option.value}
                    value={option.label}
                    onSelect={(currentValue: string) => {
                      setSelected(currentValue === selected ? '' : currentValue);
                      setOpen(false);
                      onChange(options.find((option) => option.label === currentValue));
                    }}
                  >
                    {option.label}
                  </CommandItem>
                ))}
              </CommandGroup>
              {query && (
                <CommandCreate
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
                </CommandCreate>
              )}
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>
      <div className="flex items-baseline">
        <BgtFormErrors error={error} />
      </div>
    </div>
  );
};
