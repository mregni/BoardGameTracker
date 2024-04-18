import clsx from 'clsx';

import {Text} from '@radix-ui/themes';

interface Props {
  title?: string | undefined;
  image: string | undefined;
  color?: string | undefined;
  onClick?: () => void;
  noTooltip?: boolean;
}

export const BgtAvatar = (props: Props) => {
  const { title, image, color, onClick, noTooltip = false } = props;

  if (!image && title === undefined) return null;

  return (
    <div className='group flex relative min-w-7'>
      {
        image && (
          <img
            className={
              clsx(
                'rounded-sm shadow-gray-800 shadow-md h-7 w-7',
                onClick && 'hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer'
              )
            }
            onClick={onClick}
            src={image}
          />
        )
      }
      {
        !image && (
          <div style={{ backgroundColor: color }}
            onClick={onClick}
            className={
              clsx(
                'rounded-sm shadow-gray-800 shadow-md h-7 w-7 flex justify-center items-center',
                onClick && 'hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer'
              )
            }>
            <Text size="2">{title![0]}</Text>
          </div>
        )
      }
      {
        !noTooltip && title && (
          <span className="group-hover:opacity-100 group-hover:block bg-black py-1 px-1.5 text-sm text-white absolute hidden left-1/2 z-50
    -translate-x-1/2 -translate-y-full -top-2 opacity-0 mx-auto font-sans font-normal focus:outline-none shadow-black shadow-md">{title}</span>
        )
      }
    </div>
  )
}