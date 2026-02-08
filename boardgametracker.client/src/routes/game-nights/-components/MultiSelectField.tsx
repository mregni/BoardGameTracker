import { useMemo, useState } from 'react';
import { cx } from 'class-variance-authority';

import { FormFieldWrapper } from '@/components/BgtForm';

interface Option {
  value: number;
  label: string;
  image?: string | null;
}

interface Props {
  label: string;
  options: Option[];
  selected: number[];
  disabled: boolean;
  onChange: (selected: number[]) => void;
  placeholder?: string;
}

export const MultiSelectField = (props: Props) => {
  const { label, options, selected, onChange, placeholder, disabled } = props;
  const [searchTerm, setSearchTerm] = useState('');
  const [isOpen, setIsOpen] = useState(false);

  const filteredOptions = useMemo(() => {
    return options.filter(
      (option) => option.label.toLowerCase().includes(searchTerm.toLowerCase()) && !selected.includes(option.value)
    );
  }, [options, searchTerm, selected]);

  const selectedItems = useMemo(() => {
    return options.filter((option) => selected.includes(option.value));
  }, [options, selected]);

  const handleSelect = (value: number) => {
    onChange([...selected, value]);
    setSearchTerm('');
  };

  const handleRemove = (value: number) => {
    onChange(selected.filter((v) => v !== value));
  };

  return (
    <FormFieldWrapper label={label} errors={[]} className="w-full">
      <div className="relative flex flex-col gap-2">
        <input
          type="text"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          onFocus={() => setIsOpen(true)}
          onBlur={() => setTimeout(() => setIsOpen(false), 200)}
          placeholder={placeholder}
          disabled={disabled}
          className={cx(
            'w-full bg-background font- text-white px-4 py-3 rounded-lg border border-primary/30 focus:border-primary focus:outline-none',
            'px-4 py-2 h-[45px]'
          )}
        />

        {isOpen && filteredOptions.length > 0 && (
          <div className="absolute top-full left-0 right-0 mt-1 bg-input border border-primary/30 rounded-lg max-h-48 overflow-y-auto z-50">
            {filteredOptions.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => handleSelect(option.value)}
                className="w-full px-4 py-2 text-left hover:bg-primary/20 transition-colors text-sm"
              >
                {option.label}
              </button>
            ))}
          </div>
        )}

        {selectedItems.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {selectedItems.map((item) => (
              <span
                key={item.value}
                className="px-3 py-1 bg-primary/20 text-primary border border-primary/30 rounded-lg text-sm flex items-center gap-2"
              >
                {item.label}
                <button
                  type="button"
                  onClick={() => handleRemove(item.value)}
                  className="hover:text-white transition-colors cursor-pointer"
                >
                  x
                </button>
              </span>
            ))}
          </div>
        )}
      </div>
    </FormFieldWrapper>
  );
};
