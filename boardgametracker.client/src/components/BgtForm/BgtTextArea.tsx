import { memo, useCallback } from 'react';
import { cx } from 'class-variance-authority';
import { AnyFieldApi } from '@tanstack/react-form';
import { TextArea } from '@radix-ui/themes';

import { FormFieldWrapper } from './FormFieldWrapper';

export interface Props {
  field: AnyFieldApi;
  disabled?: boolean;
  label: string;
  className?: string;
}

const BgtTextAreaComponent = (props: Props) => {
  const { field, disabled = false, className, label } = props;

  const handleChange = useCallback(
    (event: React.ChangeEvent<HTMLTextAreaElement>) => {
      field.handleChange(event.target.value);
    },
    [field]
  );

  return (
    <FormFieldWrapper label={label} errors={field.state.meta.errors}>
      <div className="'w-full bg-background font- text-whiterounded-lg border border-primary/30 focus:border-primary focus:outline-none'">
        <TextArea
          className={cx(className, 'bg-transparent shadow-none focus-within:outline-hidden')}
          rows={4}
          disabled={disabled}
          value={field.state.value ?? ''}
          onChange={handleChange}
          onBlur={field.handleBlur}
        />
      </div>
    </FormFieldWrapper>
  );
};

export const BgtTextArea = memo(BgtTextAreaComponent);
