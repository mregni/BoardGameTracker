import { Control, FieldValues, Path, useController } from 'react-hook-form';
import { useState } from 'react';
import { CommandList } from 'cmdk';
import { Button, Text } from '@radix-ui/themes';
import { ChevronDownIcon } from '@heroicons/react/24/outline';

import { BgtPopover, BgtPopoverContent, BgtPopoverTrigger } from '../BgtPopover/BgtPopover';
import { BgtCommand, BgtCommandCreate, BgtCommandGroup, BgtCommandInput, BgtCommandItem } from '../BgtCommand/BgtCommand';
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
    <div className="grid w-full">
      <BgtPopover open={open} onOpenChange={setOpen}>
        <div className="space-y-1">
          {label && <Text>{label}</Text>}
          <BgtPopoverTrigger asChild>
            <Button
              size="2"
              variant="outline"
              role="combobox"
              aria-expanded={open}
              className="w-full md:max-w-96 flex rt-variant-surface rt-reset rt-SelectTrigger rt-r-size-2 rt-variant-surface !font-normal"
            >
              {selected ? options.find((option) => option.value === selected)?.label ?? selected : placeholder}
              <ChevronDownIcon className="ml-auto size-3 shrink-0 opacity-50" />
            </Button>
          </BgtPopoverTrigger>
        </div>

        <BgtPopoverContent>
          <BgtCommand className="max-h-64 overflow-y-auto">
            <BgtCommandInput placeholder={placeholder} value={query} onValueChange={(value: string) => setQuery(value)} />
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
      <div className="flex items-baseline">
        <BgtFormErrors error={error} />
      </div>
    </div>
  );
};
