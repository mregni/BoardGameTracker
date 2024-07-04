import { ComponentPropsWithoutRef } from 'react';
import { t } from 'i18next';
import { AgChartsReact } from 'ag-charts-react';

import { BgtHeading } from '../BgtHeading/BgtHeading';

import { BgtCard } from './BgtCard';

interface Props extends ComponentPropsWithoutRef<'div'> {
  title: string;
}
export const BgtChartCard = (props: Props) => {
  const { title, className, children } = props;

  return (
    <BgtCard className={className}>
      <div className="flex flex-col gap-3 p-3">
        <BgtHeading size="5" className="uppercase">
          {title}
        </BgtHeading>
        {children}
      </div>
    </BgtCard>
  );
};
