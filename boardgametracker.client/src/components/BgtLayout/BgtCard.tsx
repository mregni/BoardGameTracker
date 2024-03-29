import clsx from 'clsx';
import {ReactNode} from 'react';

interface Props {
  children: ReactNode;
  cardStyle?: string;
  contentStyle?: string;
  transparant?: boolean;
  title?: string;
  hide?: boolean;
}

export const BgtCard = (props: Props) => {
  const { children, cardStyle = '', contentStyle = '', transparant = false, title, hide = false } = props;
  return (
    <div className={clsx('flex flex-col gap-1', cardStyle, hide && 'hidden')}>
      <div className='pl-2 text-gray-400 font-bold text-sm'>{title}</div>
      <div className={
        clsx(
          'rounded-md',
          contentStyle,
          !transparant && 'bg-sky-900 p-3'
        )}>
        {children}
      </div>
    </div>
  )
}
