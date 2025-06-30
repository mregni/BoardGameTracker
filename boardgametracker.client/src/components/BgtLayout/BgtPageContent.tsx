import { ReactNode } from 'react';
import { cx } from 'class-variance-authority';

interface Props {
  children: ReactNode;
  className?: string;
}

export const BgtPageContent = (props: Props) => {
  const { children, className = '' } = props;

  return <div className={cx('flex flex-col gap-3 h-full', className)}>{children}</div>;
};
