import { ComponentPropsWithoutRef } from 'react';
import { cx } from 'class-variance-authority';

import CloseIcon from '@/assets/icons/x.svg?react';

interface Props extends ComponentPropsWithoutRef<'div'> {
  variant: 'soft';
  color: 'red' | 'green';
  onClick?: () => void;
  onClose?: () => void;
}

export const BgtBadge = (props: Props) => {
  const { className, children, variant, color, onClick, onClose } = props;

  return (
    <div
      className={cx(
        'group group-hover:bg-[#34ffaa4a] px-3 py-2 rounded-md uppercase text-xs flex flex-row gap-2',
        color === 'green' && variant === 'soft' && 'text-mint-green bg-[#34FFAA1F]',
        color === 'red' && variant === 'soft' && '',
        onClick && 'cursor-pointer hover:bg-[#34ffaa4a] transition-colors duration-200',
        className
      )}
      onClick={onClick}
    >
      {children}
      {onClose && <CloseIcon onClick={onClose} className="size-4 cursor-pointer" />}
    </div>
  );
};
