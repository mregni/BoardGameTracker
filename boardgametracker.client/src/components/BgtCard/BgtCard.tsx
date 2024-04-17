import clsx from 'clsx';
import {ComponentPropsWithoutRef} from 'react';

interface Props extends ComponentPropsWithoutRef<'div'> {
  contentStyle?: string;
  transparant?: boolean;
  title?: string;
  hide?: boolean;
  noTitleSpacing?: boolean;
}

export const BgtCard = (props: Props) => {
  const {
    children,
    className,
    contentStyle = '',
    transparant = false,
    title, 
    hide = false,
    noTitleSpacing = false
  } = props;
  
  return (
    <div className={clsx('flex flex-col gap-1', className, hide && 'hidden')}>
      { title && <div className='pl-2 text-gray-400 font-bold text-sm'>{title}</div> }
      { !title && <div className={clsx(noTitleSpacing && title === undefined && 'md:h-5')}></div>}
      <div className={
        clsx(
          'rounded-md',
          contentStyle,
          !transparant && 'border-blue-500 border bg-gray-900 p-3'
        )}>
        {children}
      </div>
    </div>
  )
}
