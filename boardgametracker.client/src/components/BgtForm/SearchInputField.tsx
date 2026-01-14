import { useTranslation } from 'react-i18next';
import { ChangeEventHandler } from 'react';
import { cx } from 'class-variance-authority';

import { BgtSimpleInputField } from './BgtSimpleInputField';

import SearchIcon from '@/assets/icons/magnifying-glass.svg?react';
interface Props {
  value: string | number | Date | undefined;
  onChange: ChangeEventHandler<HTMLInputElement> | undefined;
  placeholder?: string;
  className?: string;
  inputClassName?: string;
}

export const SearchInputField = (props: Props) => {
  const { value, onChange, placeholder, className, inputClassName } = props;
  const { t } = useTranslation();

  return (
    <BgtSimpleInputField
      value={value}
      onChange={onChange}
      className={cx('border-primary/30 border bg-primary/10 w-full md:w-64 xl:w-[300px]', className)}
      inputClassName={cx('placeholder:text-primary/80', inputClassName)}
      placeholder={placeholder || t('common.filter-name')}
      type="text"
      prefixLabel={<SearchIcon className="text-primary" />}
    />
  );
};
