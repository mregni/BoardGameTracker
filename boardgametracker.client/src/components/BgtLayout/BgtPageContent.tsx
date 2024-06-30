import { ReactNode } from 'react';
import clsx from 'clsx';

interface Props {
  children: ReactNode;
  className?: string;
}

export const BgtPageContent = (props: Props) => {
  const { children, className = '' } = props;

  return <div className={clsx('flex flex-col gap-8 p-8', className)}>{children}</div>;
};
