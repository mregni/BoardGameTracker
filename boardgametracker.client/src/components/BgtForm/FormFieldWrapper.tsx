import { memo, ReactNode } from 'react';

import { BgtFormErrors } from './BgtFormErrors';

interface FormFieldWrapperProps {
  label?: string;
  errors?: string[];
  children: ReactNode;
  className?: string;
}

const FormFieldWrapperComponent = ({ label, errors = [], children, className = '' }: FormFieldWrapperProps) => (
  <div className={`flex flex-col justify-start ${className}`}>
    {label && (
      <div className="flex items-baseline justify-between">
        <div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
        <BgtFormErrors errors={errors} />
      </div>
    )}
    {children}
  </div>
);

FormFieldWrapperComponent.displayName = 'FormFieldWrapper';

export const FormFieldWrapper = memo(FormFieldWrapperComponent);
