import { ComponentPropsWithoutRef } from 'react';
import { cx } from 'class-variance-authority';

interface Props extends ComponentPropsWithoutRef<'div'> {
  variant: 'soft';
  color: 'red' | 'green';
}

export const BgtBadge = (props: Props) => {
  const { className, children, variant, color } = props;

  return (
    <div
      className={cx(
        'px-4 py-2 rounded-md uppercase text-xs',
        color === 'green' && variant === 'soft' && 'text-mint-green bg-[#34FFAA1F]',
        color === 'red' && variant === 'soft' && '',
        className
      )}
    >
      {children}
    </div>
  );
};
