import { ComponentPropsWithoutRef } from 'react';
import clsx from 'clsx';

interface Props extends ComponentPropsWithoutRef<'div'> {
  hide?: boolean;
}

export const BgtCard = (props: Props) => {
  const { children, className, hide = false } = props;

  return (
    <div className={clsx('border-card-border border rounded-lg bg-card-black p-2', className, hide && 'hidden')}>
      {children}
    </div>
  );
};


