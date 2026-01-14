import { useMemo } from 'react';
import { format } from 'date-fns';
import { cx } from 'class-variance-authority';
import { AnyFieldApi } from '@tanstack/react-form';

import { BgtFormErrors } from './BgtFormErrors';
import { BgtDatePicker } from './BgtDatePicker';

interface Props {
  field: AnyFieldApi;
  label: string;
  disabled?: boolean;
}

export const BgtDateTimePicker = (props: Props) => {
  const { field, label, disabled = false } = props;

  const hasErrors = field.state.meta.errors.length > 0;

  const selectedDate = useMemo(
    () => (field.state.value ? new Date(field.state.value) : undefined),
    [field.state.value]
  );

  const dateField = useMemo(
    () => ({
      state: {
        value: selectedDate ? format(selectedDate, 'yyyy-MM-dd') : '',
        meta: field.state.meta,
      },
      handleChange: (dateString: string) => {
        const newDate = new Date(dateString);

        if (selectedDate) {
          newDate.setHours(selectedDate.getHours());
          newDate.setMinutes(selectedDate.getMinutes());
        }
        field.handleChange(newDate);
      },
      handleBlur: field.handleBlur,
      name: field.name,
    }),
    [selectedDate, field]
  );

  const handleTimeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const timeValue = e.target.value;
    if (!timeValue) return;

    const [hours, minutes] = timeValue.split(':').map(Number);
    const newDate = selectedDate ? new Date(selectedDate) : new Date();
    newDate.setHours(hours);
    newDate.setMinutes(minutes);
    field.handleChange(newDate);
  };

  const timeValue = selectedDate ? format(selectedDate, 'HH:mm') : '';

  return (
    <div className="flex flex-col justify-start w-full">
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
        <BgtFormErrors errors={field.state.meta.errors} />
      </div>
      <div className="flex gap-2">
        <div className="flex-1">
          <BgtDatePicker field={dateField as AnyFieldApi} label="" disabled={disabled} />
        </div>
        <input
          type="time"
          value={timeValue}
          onChange={handleTimeChange}
          disabled={disabled}
          placeholder="xx:xx"
          className={cx(
            'w-32 bg-background text-white px-4 py-3 rounded-lg border border-primary/30 focus:border-primary focus:outline-none',
            hasErrors && 'border border-error bg-error/5!',
            disabled && 'opacity-50 cursor-not-allowed'
          )}
        />
      </div>
    </div>
  );
};
