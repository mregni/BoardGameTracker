import { ComponentPropsWithoutRef } from 'react';

import { BgtHeading } from '../BgtHeading/BgtHeading';

import { BgtCard } from './BgtCard';

interface Props extends ComponentPropsWithoutRef<'div'> {
  title: string;
  hide?: boolean;
}

export const BgtChartCard = (props: Props) => {
  const { title, className, children, hide, ...rest } = props;

  return (
    <BgtCard className={className} hide={hide} {...rest}>
      <div className="flex flex-col gap-3 p-3 h-full w-full">
        <BgtHeading size="5">{title}</BgtHeading>
        {children}
      </div>
    </BgtCard>
  );
};
