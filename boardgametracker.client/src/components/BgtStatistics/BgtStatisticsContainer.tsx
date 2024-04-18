import {ReactNode} from 'react';

import {BgtCard} from '../BgtCard/BgtCard';

interface Props {
  children: ReactNode[];
}
export const BgtStatisticsContainer = (props: Props) => {
  const { children } = props;

  return (
    <div className='flex justify-center'>
      <BgtCard noPadding contentStyle='flex flex-row justify-center flex-wrap [&>*:not(:last-child)]:border-r [&>*:not(:last-child)]:border-blue-500'>
        {children}
      </BgtCard>
    </div>
  )
}