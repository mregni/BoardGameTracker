import { useTranslation } from 'react-i18next';
import { FieldError } from 'react-hook-form';
import { ComponentPropsWithoutRef } from 'react';

interface ErrorProps extends ComponentPropsWithoutRef<'div'> {
  error: FieldError | undefined;
}
export const BgtFormErrors = (props: ErrorProps) => {
  const { error } = props;
  const { t } = useTranslation();

  return error && <div className="text-[13px] text-red-500 opacity-[0.8]">{t(error.message ?? '')}</div>;
};
