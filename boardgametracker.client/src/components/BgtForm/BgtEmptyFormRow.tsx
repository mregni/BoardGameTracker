import { ReactNode } from 'react';
import { cx } from 'class-variance-authority';

interface Props {
  right: ReactNode;
  alignRight?: boolean;
}

export const BgtEmptyFormRow = (props: Props) => {
  const { right, alignRight = false } = props;
  return (
    <div className="grid grid-cols-1 gap-1 md:gap-0 md:grid-cols-2 w-full min-h-16">
      <div className="md:col-start-2 md:pl-3">
        <div className={cx('flex', alignRight && 'justify-end md:justify-start')}>{right}</div>
      </div>
    </div>
  );
};
