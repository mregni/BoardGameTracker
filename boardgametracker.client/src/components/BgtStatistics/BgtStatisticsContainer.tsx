import {ReactNode} from 'react';

interface Props {
  children: ReactNode[];
}
export const BgtStatisticsContainer = (props: Props) => {
  const { children } = props;

  return (
    <div className='grid grid-cols-2 md:grid-cols-3 xl:grid-cols-5 gap-3'>
      {children}
    </div>
  )
}