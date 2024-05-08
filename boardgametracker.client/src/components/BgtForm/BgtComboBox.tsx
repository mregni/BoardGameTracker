import { CommandList } from 'cmdk';
import { useState } from 'react';

import { ChevronDownIcon } from '@heroicons/react/24/outline';
import { Button, Text } from '@radix-ui/themes';

import { BgtSelectItem } from '../../models/Common/BgtSelectItem';
import { Command, CommandCreate, CommandGroup, CommandInput, CommandItem } from './Command';
import { Popover, PopoverContent, PopoverTrigger } from './Popover';

interface ComboboxProps<T> {
  options: BgtSelectItem[];
  placeholder: string;
  addOptionText: (query: string) => string;
  onChange: (value: BgtSelectItem | null) => void;
  label?: string;
  onCreate: (name: string) => Promise<T>;
  isSaving: boolean;
}

export const BgtComboBox = <T,>(props: ComboboxProps<T>) => {
  const { options, placeholder, addOptionText, onChange, label, onCreate, isSaving } = props;
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState<string>('');
  const [selected, setSelected] = useState<string>('');

  console.log(isSaving);

  return (
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
                    onChange(options.find((option) => option.value === currentValue)!);
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
                  const newValue = {
                    label: query,
                    value: query,
                  };

                  onCreate(query)
                    .then(() => {
                      onChange(newValue);
                      setQuery('');
                      setSelected(query);
                    })
                    .catch((error) => console.log(error))
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
  );
};
