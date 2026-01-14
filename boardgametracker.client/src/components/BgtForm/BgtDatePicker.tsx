import { DayPicker } from 'react-day-picker';
import { useState, useMemo } from 'react';
import { cx } from 'class-variance-authority';
import { useQuery } from '@tanstack/react-query';
import { AnyFieldApi } from '@tanstack/react-form';
import * as Popover from '@radix-ui/react-popover';

import { BgtFormErrors } from './BgtFormErrors';

import { getDateFnsLocale } from '@/utils/localeUtils';
import { toDisplay, toInputDate, safeParseDate } from '@/utils/dateUtils';
import { getSettings } from '@/services/queries/settings';
import CalendarIcon from '@/assets/icons/calendar.svg?react';

import 'react-day-picker/dist/style.css';

interface Props {
  field: AnyFieldApi;
  label: string;
  disabled?: boolean;
  className?: string;
  placeholder: string;
}

export const BgtDatePicker = (props: Props) => {
  const { field, label, disabled = false, className = '', placeholder } = props;
  const [open, setOpen] = useState(false);

  const { data: settings } = useQuery(getSettings());

  const locale = useMemo(() => {
    const languageCode = settings?.uiLanguage ?? 'en-us';
    return getDateFnsLocale(languageCode);
  }, [settings?.uiLanguage]);

  const hasErrors = field.state.meta.errors.length > 0;
  const selectedDate = field.state.value ? safeParseDate(field.state.value) : undefined;

  const handleSelect = (date: Date | undefined) => {
    if (date) {
      field.handleChange(toInputDate(date, false));
      setOpen(false);
    }
  };

  return (
    <div className="flex flex-col justify-start w-full">
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
        <BgtFormErrors errors={field.state.meta.errors} />
      </div>
      <Popover.Root open={open} onOpenChange={setOpen}>
        <Popover.Trigger asChild>
          <button
            type="button"
            disabled={disabled}
            className={cx(
              'w-full bg-background text-white px-4 py-3 rounded-lg border border-primary/30 focus:border-primary focus:outline-none text-left flex items-center justify-between',
              className,
              hasErrors && 'border border-error bg-error/5!',
              disabled && 'opacity-50 cursor-not-allowed'
            )}
          >
            <span className={cx(!selectedDate && 'text-gray-400')}>
              {selectedDate && settings?.dateFormat && settings?.uiLanguage
                ? toDisplay(selectedDate, settings.dateFormat, settings.uiLanguage)
                : placeholder}
            </span>
            <CalendarIcon className="size-5 text-gray-400" />
          </button>
        </Popover.Trigger>
        <Popover.Portal>
          <Popover.Content
            className="bg-background border border-primary/30 rounded-lg p-4 shadow-lg z-50"
            side="bottom"
            align="end"
            sideOffset={5}
          >
            <DayPicker
              mode="single"
              selected={selectedDate}
              onSelect={handleSelect}
              locale={locale}
              classNames={{
                root: 'text-white w-60',
                month_caption: 'flex justify-center items-center px-0 mb-4 relative',
                caption_label: 'uppercase text-sm tracking-wider font-medium text-white',
                nav: 'absolute top-0 left-0 right-0 flex justify-between items-center',
                button_previous:
                  'bg-primary/20 hover:bg-primary/30 text-white rounded-md w-8 h-8 inline-flex items-center justify-center transition-colors border-none cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed',
                button_next:
                  'bg-primary/20 hover:bg-primary/30 text-white rounded-md w-8 h-8 inline-flex items-center justify-center transition-colors border-none cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed',
                month_grid: 'w-full',
                weekdays: 'grid grid-cols-7 mb-2',
                weekday: 'text-white/50 uppercase text-xs text-center py-2 font-normal',
                week: 'grid grid-cols-7 gap-1 mt-1',
                day: 'text-white flex items-center justify-center',
                day_button:
                  'w-9 h-9 rounded-md transition-all border-none bg-transparent text-white cursor-pointer text-sm flex items-center justify-center hover:bg-primary/20',
                selected: 'bg-primary text-white hover:bg-primary rounded-md',
                today: 'bg-[#22d3ee]/20 text-[#22d3ee] rounded-md',
                outside: 'text-white/20 opacity-50',
              }}
            />
          </Popover.Content>
        </Popover.Portal>
      </Popover.Root>
    </div>
  );
};
